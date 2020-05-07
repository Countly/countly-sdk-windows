using CountlySDK.CountlyCommon.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static CountlySDK.Countly;

namespace CountlySDK.Entities
{
    public class CountlyConfig : CountlyConfigBase
    {
        /// <summary>
        /// Which method for deviceId generation is used
        /// </summary>
        public DeviceIdMethod deviceIdMethod = DeviceIdMethod.multipleFields;        

        /// <summary>
        /// Sets the custom data path for temporary caching files
        /// If it is set to null, it will use the default location
        /// THIS WILL ONLY WORK WHEN TARGETING .NET3.5
        /// If you downloaded this package from nuget and are targeting .net4.0,
        /// this will do nothing.
        /// </summary>
        public String customDataPath = null;
    }
}
