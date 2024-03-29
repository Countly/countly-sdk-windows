﻿/*
Copyright (c) 2012, 2013, 2014 Countly

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon.Helpers;

namespace CountlySDK.Helpers
{
    internal class StorageNetStd : StorageBase
    {
        internal virtual IsolatedStorageFile isolatedStorage { get { throw new NotImplementedException(); } }

        internal virtual void closeIsolatedStorageStream(IsolatedStorageFileStream file) { throw new NotImplementedException(); }

        internal virtual void closeStreamWriter(StreamWriter stream) { throw new NotImplementedException(); }

        private bool IsFileExists(IsolatedStorageFile store, string fileName)
        {
            UtilityHelper.CountlyLogging("[StorageNetStd] Calling 'IsFileExists', with store");
            if (!store.DirectoryExists(sdkFolder)) {
                return false;
            }

            return store.FileExists(Path.Combine(sdkFolder, fileName));
        }

        public bool IsFileExists(string fileName)
        {
            UtilityHelper.CountlyLogging("[StorageNetStd] Calling 'IsFileExists', without store");
            var store = isolatedStorage;
            return IsFileExists(store, fileName);
        }

        public override async Task<bool> SaveToFile<T>(string filename, object objForSave)
        {
            UtilityHelper.CountlyLogging("[StorageNetStd] Calling 'SaveToFile'");
            Debug.Assert(filename != null, "Provided filename can't be null");
            Debug.Assert(objForSave != null, "Provided object can't be null");

            lock (locker) {
                bool success = true;
                try {
                    var store = isolatedStorage;

                    if (!store.DirectoryExists(sdkFolder)) {
                        store.CreateDirectory(sdkFolder);
                    }

                    using (IsolatedStorageFileStream file = store.OpenFile(Path.Combine(sdkFolder, filename), FileMode.Create, FileAccess.Write, FileShare.Read)) {
                        if (file != null && objForSave != null) {
                            DataContractSerializer ser = new DataContractSerializer(objForSave.GetType());
                            ser.WriteObject(file, objForSave);
                        }

                        closeIsolatedStorageStream(file);
                    }
                } catch (Exception ex) {
                    success = false;
                    UtilityHelper.CountlyLogging("[StorageNetStd] SaveToFile, save countly data failed." + ex.ToString());
                }
                return success;
            }

        }

        public override async Task<T> LoadFromFile<T>(string filename)
        {
            UtilityHelper.CountlyLogging("[StorageNetStd] Calling 'LoadFromFile'");
            Debug.Assert(filename != null, "Provided filename can't be null");

            T obj = default(T);

            lock (locker) {
                try {
                    var store = isolatedStorage;

                    if (!IsFileExists(store, filename)) {
                        //if file does not exist, return null
                        return obj;
                    }

                    using (var file = store.OpenFile(Path.Combine(sdkFolder, filename), FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                        if (file != null) {
                            DataContractSerializer ser = new DataContractSerializer(typeof(T));
                            obj = (T)ser.ReadObject(file);
                        } else {
                            obj = default(T);
                        }

                        closeIsolatedStorageStream(file);
                    }
                } catch (Exception ex) {
                    UtilityHelper.CountlyLogging("[StorageNetStd] LoadFromFile, Problem while loading from file. " + ex.ToString());
                }
            }

            return obj;
        }

        /// <summary>
        /// Delete file
        /// </summary>
        /// <param name="filename">Filename to delete</param>
        public override async Task DeleteFile(string filename)
        {
            UtilityHelper.CountlyLogging("[StorageNetStd] Calling 'DeleteFile'");
            try {
                var store = isolatedStorage;
                if (IsFileExists(store, filename)) {
                    store.DeleteFile(Path.Combine(sdkFolder, filename));
                }
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("[StorageNetStd] DeleteFile, Problem while loading from file. " + ex.ToString());
            }
        }

        internal override async Task<string> GetFolderPath(string folderName)
        {
            // Create a file in isolated storage.
            IsolatedStorageFile store = isolatedStorage;
            IsolatedStorageFileStream stream = new IsolatedStorageFileStream("test.txt", FileMode.Create, store);
            StreamWriter writer = new StreamWriter(stream);
            writer.WriteLine("Hello");
            closeStreamWriter(writer);
            closeIsolatedStorageStream(stream);



            // Retrieve the actual path of the file using reflection.
            string path = stream.GetType().GetField("m_FullPath", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(stream).ToString();
            return path.Replace("test.txt", folderName);
        }
    }
}
