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
using System.Text;
using System.Threading.Tasks;

namespace CountlySDK.Helpers
{
    internal class Storage : StorageBase
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly Storage instance = new Storage();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static Storage() { }
        internal Storage() { }
        public static Storage Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        /// <summary>
        /// Countly folder
        /// </summary>
        internal const string folder = "countly_data";

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
            var store = IsolatedStorageFile.GetUserStoreForAssembly();
            return IsFileExists(store, fileName);
        }

        /// <summary>
        /// This empty stub is needed because of sharing the "countly"
        /// class between .net3.5 and .net4.0
        /// </summary>
        /// <param name="path"></param>
        public void SetCustomDataPath(String path)
        {
            //do nothing
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
                    var store = IsolatedStorageFile.GetUserStoreForAssembly();

                    if (!store.DirectoryExists(folder))
                    {
                        store.CreateDirectory(folder);
                    }

                    using (var file = store.OpenFile(Path.Combine(folder, filename), FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        if (file != null && objForSave != null)
                        {
                            DataContractSerializer ser = new DataContractSerializer(objForSave.GetType());
                            ser.WriteObject(file, objForSave);
                        }

                        file.Close();
                    }
                }
                catch
                {
                    success = false;
                    if (Countly.IsLoggingEnabled)
                    {
                        Debug.WriteLine("save countly data failed");
                    }
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
                    var store = IsolatedStorageFile.GetUserStoreForAssembly();
                    
                    if (! IsFileExists(store, filename))
                    {
                        return obj;
                    }

                    using (var file = store.OpenFile(Path.Combine(folder, filename), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        if (typeof(T) == null || file != null)
                        {
                            DataContractSerializer ser = new DataContractSerializer(typeof(T));
                            obj = (T)ser.ReadObject(file);
                        }
                        else
                        {
                            obj = default(T);
                        }

                        file.Close();
                    }
                }
                catch
                {
                    if (Countly.IsLoggingEnabled)
                    {
                        Debug.WriteLine("countly queue lost");
                    }
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
                var store = IsolatedStorageFile.GetUserStoreForAssembly();
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
            IsolatedStorageFile store = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
            IsolatedStorageFileStream stream = new IsolatedStorageFileStream("test.txt", FileMode.Create, store);
            StreamWriter writer = new StreamWriter(stream);
            writer.WriteLine("Hello");
            writer.Close();
            stream.Close();

            // Retrieve the actual path of the file using reflection.
            string path = stream.GetType().GetField("m_FullPath", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(stream).ToString();
            return path.Replace("test.txt", folderName);
        }
    }
}
