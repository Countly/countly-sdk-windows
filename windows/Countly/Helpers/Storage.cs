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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Storage;

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

        internal override string folder { get { return "countly"; } }

        private Dictionary<string, bool> filesInUse = new Dictionary<string, bool>();

        public ApplicationDataContainer Settings
        {
            get
            {
                return Windows.Storage.ApplicationData.Current.LocalSettings;
            }
        }

        public TValue GetValue<TValue>(string key, TValue defaultvalue)
        {
            TValue value;

            // If the key exists, retrieve the value.
            if (Settings.Values.ContainsKey(key))
            {
                value = (TValue)Settings.Values[key];
            }
            // Otherwise, use the default value.
            else
            {
                value = defaultvalue;
            }

            return value;
        }

        public bool SetValue(string key, object value)
        {
            bool valueChanged = false;

            if (Settings.Values.ContainsKey(key))
            {
                if (Settings.Values[key] != value)
                {
                    Settings.Values[key] = value;
                    valueChanged = true;
                }
            }
            else
            {
                Settings.Values.Add(key, value);
                valueChanged = true;
            }

            return valueChanged;
        }

        public void RemoveValue(string key)
        {
            if (Settings.Values.ContainsKey(key))
            {
                Settings.Values.Remove(key);
            }
        }

        public override async Task<bool> SaveToFile<T>(string filename, object objForSave)
        {
            Debug.Assert(filename != null, "Provided filename can't be null");
            Debug.Assert(objForSave != null, "Provided object can't be null");

            if (filesInUse.ContainsKey(filename))
            {
                return false;
            }

            filesInUse[filename] = true;

            bool success = true;

            try
            {
                var sessionSerializer = new DataContractSerializer(typeof(T));
                MemoryStream sessionData = new MemoryStream();
                sessionSerializer.WriteObject(sessionData, objForSave);
                sessionData.Seek(0, SeekOrigin.Begin);

                await SaveStream(sessionData, filename);
            }
            catch
            {
                success = false;
            }
            finally
            {
                filesInUse.Remove(filename);
            }

            return success;
        }

        /// <summary>
        /// Saves stream into file
        /// </summary>
        /// <param name="stream">stream to save</param>
        /// <param name="file">filename</param>
        private async Task SaveStream(Stream stream, string file)
        {
            try
            {
                string temp = Guid.NewGuid().ToString();

                StorageFolder storageFolder = await GetFolder(folder);

                StorageFile tempFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(temp, CreationCollisionOption.ReplaceExisting);

                using (Stream fileStream = await tempFile.OpenStreamForWriteAsync())
                {
                    await stream.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();
                    fileStream.Dispose();
                }

                string backup = file + ".backup";

                StorageFile backupFile = await GetFile(backup);

                await tempFile.CopyAndReplaceAsync(backupFile);

                StorageFile storageFile = await GetFile(file);

                await tempFile.CopyAndReplaceAsync(storageFile);

                await DeleteFile(temp);                
            }
            catch
            { }
        }
        
        public override async Task<T> LoadFromFile<T>(string filename)
        {
            Debug.Assert(filename != null, "Provided filename can't be null");

            T t = null;

            try
            {
                Stream stream = await LoadStream(filename);

                if (stream != null)
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        var sessionSerializer = new DataContractSerializer(typeof(T));
                        T obj = (T)sessionSerializer.ReadObject(reader.BaseStream);

                        t = obj;
                    }
                }
            }
            catch
            { }

            if (t != null)
            {
                return t;
            }
            else if (!filename.EndsWith(".backup"))
            {
                return await LoadFromFile<T>(filename + ".backup");
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Loads stream from file
        /// </summary>
        /// <param name="path">filename</param>
        /// <returns>stream</returns>
        private async Task<Stream> LoadStream(string path)
        {
            try
            {
                bool isFileExists = await FileExists(path);

                if (!isFileExists) return null;

                StorageFolder storageFolder = await GetFolder(folder);

                StorageFile storageFile = await storageFolder.CreateFileAsync(path, CreationCollisionOption.OpenIfExists);

                if (storageFile == null)
                {
                    throw new Exception();
                }

                using (StreamReader reader = new StreamReader(await storageFolder.OpenStreamForReadAsync(path)))
                {
                    MemoryStream memoryStream = new MemoryStream();

                    reader.BaseStream.CopyTo(memoryStream);

                    memoryStream.Position = 0;

                    return memoryStream;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks whether there is a file in storage
        /// </summary>
        /// <param name="path">filename</param>
        /// <returns>true if file exists, false otherwise</returns>
        private async Task<bool> FileExists(string path)
        {
            StorageFolder storageFolder = await GetFolder(folder);
            
            IReadOnlyList<StorageFile> files = await storageFolder.GetFilesAsync();

            foreach (StorageFile file in files)
            {
                if (file.Name == path)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get StorageFile object from specified path
        /// </summary>
        /// <param name="path">file path</param>
        /// <returns>StorageFile object</returns>
        private async Task<StorageFile> GetFile(string path)
        {
            StorageFolder storageFolder = await GetFolder(folder);

            StorageFile storageFile = await storageFolder.CreateFileAsync(path, CreationCollisionOption.OpenIfExists);

            return storageFile;
        }

        /// <summary>
        /// Delete file
        /// </summary>
        /// <param name="path">Filename to delete</param>
        public override async Task DeleteFile(string path)
        {
            StorageFolder storageFolder = await GetFolder(folder);

            StorageFile sessionFile = await storageFolder.CreateFileAsync(path, CreationCollisionOption.OpenIfExists);

            if (sessionFile != null)
            {
                await sessionFile.DeleteAsync();
            }
        }

        /// <summary>
        /// Checks whether there is a folder in storage
        /// </summary>
        /// <param name="folder">folder name</param>
        /// <returns>true if folder exists, false otherwise</returns>
        private async Task<bool> FolderExists(string folder)
        {
            IReadOnlyList<StorageFolder> folders = await ApplicationData.Current.LocalFolder.GetFoldersAsync();

            foreach (StorageFolder folder_ in folders)
            {
                if (folder_.Name == folder)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get StorageFolder object from specified path
        /// </summary>
        /// <param name="folder">folder path</param>
        /// <returns>StorageFolder object</returns>
        private async Task<StorageFolder> GetFolder(string folder)
        {
            StorageFolder storageFolder;
                
            if (!await FolderExists(folder))
            {
                await CreateFolder(folder);
            }

            storageFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync(folder);

            return storageFolder;
        }

        /// <summary>
        /// Creates folder with specified name
        /// </summary>
        /// <param name="folder">folder name</param>
        private async Task CreateFolder(string folder)
        {
            try
            {
                bool isFolderExists = await FolderExists(folder);

                if (!isFolderExists)
                {
                    await ApplicationData.Current.LocalFolder.CreateFolderAsync(folder);
                }
            }
            catch
            { }
        }

        internal override Task<string> GetFolderPath(string folderName)
        {
            throw new NotImplementedException();
        }
    }
}
