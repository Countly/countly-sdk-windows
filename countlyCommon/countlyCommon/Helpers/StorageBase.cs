using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CountlySDK.CountlyCommon.Helpers
{
    abstract internal class StorageBase
    {       
        protected object locker = new Object();

        /// <summary>
        /// Saves object into file
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="filename">File to save to</param>
        /// <param name="objForSave">Object to save</param>
        /// <returns>True if success, otherwise - False</returns>
        public abstract Task<bool> SaveToFile<T>(string filename, object objForSave) where T : class;

        /// <summary>
        /// Load object from file
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="filename">Filename to load from</param>
        /// <returns>Object from file</returns>
        public abstract Task<T> LoadFromFile<T>(string filename) where T : class;

    }
}
