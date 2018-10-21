using CountlySDK.CountlyCommon.Entities;
using PCLStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CountlySDK.Entities
{
    public class CountlyConfig : CountlyConfigBase
    {
        /// <summary>
        /// Needed for PCL storage
        /// [Mandatory field]
        /// </summary>
        public IFileSystem fileSystem = FileSystem.Current;
    }
}
