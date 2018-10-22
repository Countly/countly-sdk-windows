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
        internal const string folder = "countly";        
        private string customDataPath = null;

        private string Path
        {
            get
            {
                if (customDataPath == null)
                {
                    return System.IO.Directory.GetCurrentDirectory() + @"\" + folder;
                } else
                {
                    return customDataPath + @"\" + folder;
                }
            }
        }

        /// <summary>
        /// Set custom data path for countly data cache
        /// If path is set to null, it will clear the custom path
        /// </summary>
        /// <param name="customPath">Given custom path</param>
        public void SetCustomDataPath(string customPath)
        {
            customDataPath = customPath;
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
                    bool exists = System.IO.Directory.Exists(Path);

                    if (!exists)
                        System.IO.Directory.CreateDirectory(Path);
                
                    using (FileStream file = new FileStream(Path + @"\" + filename, FileMode.Create, FileAccess.Write, FileShare.Read))
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
                    if (!System.IO.Directory.Exists(Path))
                    {
                        System.IO.Directory.CreateDirectory(Path);
                    }

                    if (!File.Exists(Path + @"\" + filename)) return obj;
            
                    using (FileStream file = new FileStream(Path + @"\" + filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        if (typeof(T) == null || file != null)
                        {
                            DataContractSerializer ser = new DataContractSerializer(typeof(T));
                            obj = (T)ser.ReadObject(file);
                        }
                        else
                        {
                            obj = null;
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
                if (File.Exists(Path + @"\" + filename))
                {
                    File.Delete(Path + @"\" + filename);
                }
            }
            catch
            { }
        }

        internal override async Task<string> GetFolderPath(string folderName)
        {
            return System.IO.Directory.GetCurrentDirectory() + @"\" + folderName;
        }
    }
}
