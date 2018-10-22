using CountlySDK.CountlyCommon.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml;
using static CountlySDK.Countly;

namespace CountlySDK.Entities
{
    public class CountlyConfig : CountlyConfigBase
    {
        public Application application = null;

        /// <summary>
        /// Which method for deviceId generation is used
        /// </summary>
        public DeviceIdMethod deviceIdMethod = DeviceIdMethod.winHardwareToken;
    }
}
