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
using System.Text;

namespace CountlySDK.Helpers
{
    internal class Storage
    {
        /// <summary>
        /// Countly folder
        /// </summary>
        private const string folder = "countly";
        private static object locker = new Object();

        internal static bool UseIsolatedStorage { get; set; }

        private static string Path
        {
            get
            {
                return System.IO.Directory.GetCurrentDirectory() + @"\" + folder;
            }
        }

        /// <summary>
        /// Saves object into file
        /// </summary>
        /// <param name="filename">File to save to</param>
        /// <param name="objForSave">Object to save</param>
        public static void SaveToFile(string filename, object objForSave)
        {
            if (UseIsolatedStorage)
            {
                IsoStorage.SaveToFile(filename, objForSave);
            }
            else
            {
                lock (locker)
                {
                    try
                    {
                        bool exists = System.IO.Directory.Exists(Path);

                        if (!exists)
                            System.IO.Directory.CreateDirectory(Path);

                        using (FileStream file = new FileStream(Path + @"\" + filename, FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            Serialize(file, objForSave);
                            file.Close();
                        }
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

        }

        /// <summary>
        /// Load object from file
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="filename">Filename to load from</param>
        /// <returns>Object from file</returns>
        public static T LoadFromFile<T>(string filename)
        {
            if (UseIsolatedStorage)
            {
                return IsoStorage.LoadFromFile<T>(filename);
            }
            else
            {
                T obj = default(T);

                lock (locker)
                {
                    try
                    {
                        if (!System.IO.Directory.Exists(Path))
                        {
                            System.IO.Directory.CreateDirectory(Path);
                        }

                        if (!File.Exists(Path + @"\" + filename)) return obj;

                        using (FileStream file = new FileStream(Path + @"\" + filename, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            obj = (T)Deserialize(file, typeof(T));

                            file.Close();
                        }
                    }
                    catch
                    {
                        if (Countly.IsLoggingEnabled)
                        {
                            Debug.WriteLine("countly queue lost");
                        }

                        DeleteFile(Path + @"\" + filename);
                    }
                }

                return obj;
            }
        }

        /// <summary>
        /// Delete file
        /// </summary>
        /// <param name="filename">Filename to delete</param>
        public static void DeleteFile(string filename)
        {
            if (UseIsolatedStorage)
            {
                IsoStorage.DeleteFile(filename);
            }
            else
            {
                try
                {
                    if (File.Exists(Path + @"\" + filename))
                    {
                        File.Delete(Path + @"\" + filename);
                    }
                }
                catch
                { }
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
