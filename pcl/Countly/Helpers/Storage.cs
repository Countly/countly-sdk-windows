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
using PCLStorage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
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

        internal IFileSystem fileSystem;
        /// <summary>
        /// Countly folder
        /// </summary>
        internal override string folder { get { return "countly"; } }

        private Dictionary<string, bool> filesInUse = new Dictionary<string, bool>();

        public override async Task<bool> SaveToFile<T>(string filename, object objForSave)
        {
            UtilityHelper.CountlyLogging("[Storage] Calling 'SaveToFile'");
            Debug.Assert(filename != null, "Provided filename can't be null");
            Debug.Assert(objForSave != null, "Provided object can't be null");

            if (filesInUse.ContainsKey(filename)) {
                return false;
            }

            filesInUse[filename] = true;

            bool success = true;

            try {
                var sessionSerializer = new DataContractSerializer(typeof(T));
                MemoryStream sessionData = new MemoryStream();
                sessionSerializer.WriteObject(sessionData, objForSave);
                sessionData.Seek(0, SeekOrigin.Begin);

                await SaveStream(sessionData, filename);
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("[Storage] SaveToFile. " + ex.ToString());
                success = false;
            } finally {
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
            UtilityHelper.CountlyLogging("[Storage] Calling 'SaveStream'");
            try {
                IFolder storageFolder = await GetFolder(folder);

                IFile storageFile = await storageFolder.CreateFileAsync(file, CreationCollisionOption.ReplaceExisting);

                using (Stream fileStream = await storageFile.OpenAsync(PCLStorage.FileAccess.ReadAndWrite)) {
                    await stream.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();
                    fileStream.Dispose();
                }
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("[Storage] SaveStream. " + ex.ToString());
            }
        }

        public override async Task<T> LoadFromFile<T>(string filename)
        {
            UtilityHelper.CountlyLogging("[Storage] Calling 'LoadFromFile'");
            Debug.Assert(filename != null, "Provided filename can't be null");

            T t = null;

            try {
                Stream stream = await LoadStream(filename);

                if (stream != null) {
                    using (StreamReader reader = new StreamReader(stream)) {
                        var sessionSerializer = new DataContractSerializer(typeof(T));
                        T obj = (T)sessionSerializer.ReadObject(reader.BaseStream);

                        t = obj;
                    }
                }
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("[Storage] LoadFromFile, Problem while deserializing [" + filename + "] ex:[" + ex.ToString() + "]");
            }

            if (t != null) {
                return t;
            } else {
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
            UtilityHelper.CountlyLogging("[Storage] Calling 'LoadStream'");
            try {
                bool isFileExists = await FileExists(path);

                if (!isFileExists) {
                    return null;
                }

                IFolder storageFolder = await GetFolder(folder);

                IFile storageFile = await storageFolder.CreateFileAsync(path, CreationCollisionOption.OpenIfExists);

                if (storageFile == null) {
                    throw new Exception();
                }

                using (StreamReader reader = new StreamReader(await storageFile.OpenAsync(PCLStorage.FileAccess.Read))) {
                    MemoryStream memoryStream = new MemoryStream();

                    reader.BaseStream.CopyTo(memoryStream);

                    memoryStream.Position = 0;

                    return memoryStream;
                }
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("[Storage] LoadStream. " + ex.ToString());
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
            UtilityHelper.CountlyLogging("[Storage] Calling 'FileExists'");
            IFolder storageFolder = await GetFolder(folder);

            IList<IFile> files = await storageFolder.GetFilesAsync();

            foreach (IFile file in files) {
                if (file.Name == path) {
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
        private async Task<IFile> GetFile(string path)
        {
            UtilityHelper.CountlyLogging("[Storage] Calling 'GetFile'");
            IFolder storageFolder = await GetFolder(folder);

            IFile storageFile = await storageFolder.CreateFileAsync(path, CreationCollisionOption.OpenIfExists);

            return storageFile;
        }

        /// <summary>
        /// Delete file
        /// </summary>
        /// <param name="filename">Filename to delete</param>
        public override async Task DeleteFile(string filename)
        {
            UtilityHelper.CountlyLogging("[Storage] Calling 'DeleteFile'");
            IFolder storageFolder = await GetFolder(folder);

            IFile sessionFile = await storageFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

            if (sessionFile != null) {
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
            UtilityHelper.CountlyLogging("[Storage] Calling 'FolderExists'");
            IList<IFolder> folders = await fileSystem.LocalStorage.GetFoldersAsync();

            foreach (IFolder folder_ in folders) {
                if (folder_.Name == folder) {
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
        internal async Task<IFolder> GetFolder(string folder)
        {
            UtilityHelper.CountlyLogging("[Storage] Calling 'GetFolder'");
            IFolder storageFolder;

            if (!await FolderExists(folder)) {
                await CreateFolder(folder);
            }

            storageFolder = await fileSystem.LocalStorage.GetFolderAsync(folder);

            return storageFolder;
        }

        /// <summary>
        /// Creates folder with specified name
        /// </summary>
        /// <param name="folder">folder name</param>
        private async Task CreateFolder(string folder)
        {
            UtilityHelper.CountlyLogging("[Storage] Calling 'CreateFolder'");
            try {
                bool isFolderExists = await FolderExists(folder);

                if (!isFolderExists) {
                    await fileSystem.LocalStorage.CreateFolderAsync(folder, CreationCollisionOption.OpenIfExists);
                }
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("[Storage] CreateFolder. " + ex.ToString());
            }
        }

        internal async override Task<string> GetFolderPath(string folderName)
        {
            UtilityHelper.CountlyLogging("[Storage] Calling 'GetFolderPath'");
            IFolder folder = await Storage.Instance.GetFolder(Storage.Instance.folder);
            return folder.Path;
        }
    }
}
