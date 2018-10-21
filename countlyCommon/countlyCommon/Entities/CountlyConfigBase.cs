using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CountlySDK.CountlyCommon.Entities
{
    public class CountlyConfigBase
    {
        /// <summary>
        /// Server url
        /// [Mandatory field]
        /// </summary>
        public String serverUrl;

        /// <summary>
        /// Application key
        /// [Mandatory field]
        /// </summary>
        public String appKey;

        /// <summary>
        /// Application version
        /// [Mandatory field]
        /// </summary>
        public String appVersion;        
    }
}
