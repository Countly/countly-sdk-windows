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


using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;

namespace CountlySDK.Helpers
{
    internal class Storage
    {
        /// <summary>
        /// Countly folder
        /// </summary>
        private const string folder = "countly";

        private static IsolatedStorageFile Store
        {
            get
            {
                return IsolatedStorageFile.GetUserStoreForApplication();
            }
        }

        /// <summary>
        /// Saves object into file
        /// </summary>
        /// <param name="filename">File to save to</param>
        /// <param name="objForSave">Object to save</param>
        public static void SaveToFile(string filename, object objForSave)
        {
            lock (Store)
            {
                if (!Store.DirectoryExists(folder))
                {
                    Store.CreateDirectory(folder);
                }

                try
                {
                    using (IsolatedStorageFileStream file = new IsolatedStorageFileStream(folder + "/" + filename, FileMode.Create, FileAccess.Write, Store))
                    {
                        Serialize(file, objForSave);

                        file.Close();
                    };
                }
                catch
                {
                    if (Countly.IsLoggingEnabled)
                    {
                        Debug.WriteLine("save countly data failed");
                    }
                }
            }
        }

        /// <summary>
        /// Load object from file
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="filename">Filename to load from</param>
        /// <returns>Object from file</returns>
        public static T LoadFromFile<T>(string filename)
        {
            T obj = default(T);

            if (!Store.DirectoryExists(folder))
            {
                Store.CreateDirectory(folder);
            }

            if (!Store.FileExists(folder + "/" + filename)) return obj;

            lock (Store)
            {
                try
                {
                    using (IsolatedStorageFileStream file = new IsolatedStorageFileStream(folder + "/" + filename, FileMode.Open, FileAccess.Read, Store))
                    {
                        obj = (T)Deserialize(file, typeof(T));

                        file.Close();
                    };
                }
                catch
                {
                    if (Countly.IsLoggingEnabled)
                    {
                        Debug.WriteLine("countly queue lost");
                    }

                    DeleteFile(folder + "/" + filename);
                }
            }

            return obj;
        }

        /// <summary>
        /// Delete file
        /// </summary>
        /// <param name="path">Filename to delete</param>
        public static void DeleteFile(string path)
        {
            if (Store.FileExists(path))
            {
                Store.DeleteFile(path);
            }
        }

        private static void Serialize(Stream streamObject, object objForSerialization)
        {
            if (objForSerialization == null || streamObject == null)
                return;

            DataContractSerializer ser = new DataContractSerializer(objForSerialization.GetType());
            ser.WriteObject(streamObject, objForSerialization);
        }

        private static object Deserialize(Stream streamObject, Type serializedObjectType)
        {
            if (serializedObjectType == null || streamObject == null)
                return null;

            DataContractSerializer ser = new DataContractSerializer(serializedObjectType);
            return ser.ReadObject(streamObject);
        }
    }
}
