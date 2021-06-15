/*
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

using CountlySDK.CountlyCommon.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace CountlySDK.Helpers
{
    internal class StorageNetStd : StorageBase
    {
        internal override string folder => throw new NotImplementedException();

        internal virtual IsolatedStorageFile isolatedStorage { get { throw new NotImplementedException(); } }

        internal virtual void closeIsolatedStorageStream(IsolatedStorageFileStream file) { throw new NotImplementedException(); }

        internal virtual void closeStreamWriter(StreamWriter stream) { throw new NotImplementedException(); }

        private bool IsFileExists(IsolatedStorageFile store, string fileName)
        {
            if (!store.DirectoryExists(folder))
            {
                return false;
            }

            return store.FileExists(Path.Combine(folder, fileName));
        }

        public bool IsFileExists(string fileName)
        {
            var store = isolatedStorage;
            return IsFileExists(store, fileName);
        }

        public override async Task<bool> SaveToFile<T>(string filename, object objForSave)
        {
            Debug.Assert(filename != null, "Provided filename can't be null");
            Debug.Assert(objForSave != null, "Provided object can't be null");

            lock (locker)
            {
                bool success = true;
                try
                {
                    var store = isolatedStorage;

                    if (!store.DirectoryExists(folder))
                    {
                        store.CreateDirectory(folder);
                    }

                    using (IsolatedStorageFileStream file = store.OpenFile(Path.Combine(folder, filename), FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        if (file != null && objForSave != null)
                        {
                            DataContractSerializer ser = new DataContractSerializer(objForSave.GetType());
                            ser.WriteObject(file, objForSave);
                        }

                        closeIsolatedStorageStream(file);
                    }
                }
                catch
                {
                    success = false;
                    UtilityHelper.CountlyLogging("save countly data failed");
                }
                return success;
            }

        }

        public override async Task<T> LoadFromFile<T>(string filename)
        {
            Debug.Assert(filename != null, "Provided filename can't be null");

            T obj = default(T);

            lock (locker)
            {
                try
                {
                    var store = isolatedStorage;

                    if (!IsFileExists(store, filename))
                    {
                        //if file does not exist, return null
                        return obj;
                    }

                    using (var file = store.OpenFile(Path.Combine(folder, filename), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        if (file != null)
                        {
                            DataContractSerializer ser = new DataContractSerializer(typeof(T));
                            obj = (T)ser.ReadObject(file);
                        }
                        else
                        {
                            obj = default(T);
                        }

                        closeIsolatedStorageStream(file);
                    }
                }
                catch
                {
                    UtilityHelper.CountlyLogging("Problem while loading from file");
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
            try
            {
                var store = isolatedStorage;
                if (IsFileExists(store, filename))
                {
                    store.DeleteFile(Path.Combine(folder, filename));
                }
            }
            catch
            { }
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
