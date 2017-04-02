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

using PCLStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace CountlySDK.Helpers
{
    internal class Storage
    {
        public static IFileSystem fileSystem;
        /// <summary>
        /// Countly folder
        /// </summary>
        private const string folder = "countly";

        private static Dictionary<string, bool> filesInUse = new Dictionary<string, bool>();

        /// <summary>
        /// Saves object into file
        /// </summary>
        /// <param name="filename">File to save to</param>
        /// <param name="objForSave">Object to save</param>
        /// <returns>True if success, otherwise - False</returns>
        public static async Task<bool> SaveToFile<T>(string path, object objForSave)
        {
            if (filesInUse.ContainsKey(path))
            {
                return false;
            }

            filesInUse[path] = true;

            bool success = true;

            try
            {
                var sessionSerializer = new DataContractSerializer(typeof(T));
                MemoryStream sessionData = new MemoryStream();
                sessionSerializer.WriteObject(sessionData, objForSave);
                sessionData.Seek(0, SeekOrigin.Begin);

                await SaveStream(sessionData, path);
            }
            catch
            {
                success = false;
            }
            finally
            {
                filesInUse.Remove(path);
            }

            return success;
        }

        /// <summary>
        /// Saves stream into file
        /// </summary>
        /// <param name="stream">stream to save</param>
        /// <param name="file">filename</param>
        private static async Task SaveStream(Stream stream, string file)
        {
            try
            {
                IFolder storageFolder = await GetFolder(folder);

                IFile storageFile = await fileSystem.LocalStorage.CreateFileAsync(file, CreationCollisionOption.ReplaceExisting);

                using (Stream fileStream = await storageFile.OpenAsync(FileAccess.ReadAndWrite))
                {
                    await stream.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();
                    fileStream.Dispose();
                }
            }
            catch
            { }
        }

        /// <summary>
        /// Load object from file
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="filename">Filename to load from</param>
        /// <returns>Object from file</returns>
        public static async Task<T> LoadFromFile<T>(string path) where T : class
        {
            T t = null;

            try
            {
                Stream stream = await LoadStream(path);

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
        private static async Task<Stream> LoadStream(string path)
        {
            try
            {
                bool isFileExists = await FileExists(path);

                if (!isFileExists) return null;

                IFolder storageFolder = await GetFolder(folder);

                IFile storageFile = await storageFolder.CreateFileAsync(path, CreationCollisionOption.OpenIfExists);

                if (storageFile == null)
                {
                    throw new Exception();
                }

                IFile file = await fileSystem.GetFileFromPathAsync(path);

                using (StreamReader reader = new StreamReader(await file.OpenAsync(FileAccess.Read)))
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
        private static async Task<bool> FileExists(string path)
        {
            IFolder storageFolder = await GetFolder(folder);
            
            IList<IFile> files = await storageFolder.GetFilesAsync();

            foreach (IFile file in files)
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
        private static async Task<IFile> GetFile(string path)
        {
            IFolder storageFolder = await GetFolder(folder);

            IFile storageFile = await storageFolder.CreateFileAsync(path, CreationCollisionOption.OpenIfExists);

            return storageFile;
        }

        /// <summary>
        /// Delete file
        /// </summary>
        /// <param name="path">Filename to delete</param>
        public static async Task DeleteFile(string path)
        {
            IFolder storageFolder = await GetFolder(folder);

            IFile sessionFile = await storageFolder.CreateFileAsync(path, CreationCollisionOption.OpenIfExists);

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
        private static async Task<bool> FolderExists(string folder)
        {
            IList<IFolder> folders = await fileSystem.LocalStorage.GetFoldersAsync();

            foreach (IFolder folder_ in folders)
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
        private static async Task<IFolder> GetFolder(string folder)
        {            
            IFolder storageFolder;
                
            if (!await FolderExists(folder))
            {
                await CreateFolder(folder);
            }

            storageFolder = await fileSystem.LocalStorage.GetFolderAsync(folder);

            return storageFolder;
        }

        /// <summary>
        /// Creates folder with specified name
        /// </summary>
        /// <param name="folder">folder name</param>
        private static async Task CreateFolder(string folder)
        {
            try
            {
                bool isFolderExists = await FolderExists(folder);

                if (!isFolderExists)
                {
                    await fileSystem.LocalStorage.CreateFolderAsync(folder, CreationCollisionOption.OpenIfExists);
                }
            }
            catch
            { }
        }
    }
}
