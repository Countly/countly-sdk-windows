using CountlySDK.CountlyCommon.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using static CountlySDK.Countly;

namespace CountlySDK.Entities
{
    public class CountlyConfig : CountlyConfigBase
    {
        /// <summary>
        /// Which method for deviceId generation is used
        /// </summary>
        public DeviceIdMethod deviceIdMethod = DeviceIdMethod.windowsGUID;

        /// <summary>
        /// Reference to you Application
        /// </summary>
        public Application application = null;
    }
}
