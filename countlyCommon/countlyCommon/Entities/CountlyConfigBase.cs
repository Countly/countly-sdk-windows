using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static CountlySDK.CountlyCommon.CountlyBase;

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

        /// <summary>
        /// Device Id provided by the developer. If value is not null,
        /// changes the DeviceIdMethod to 'developerSupplied' and overwrites
        /// the previously used/saved deviceId
        /// </summary>
        public String developerProvidedDeviceId = null;

        /// <summary>
        /// If user consent for features is required. If enabled, then features
        /// will not work without explicitly granting permission.
        /// </summary>
        public bool consentRequired = false;

        /// <summary>
        /// Features for which consent is given or denied. These set values are not persistent
        /// </summary>
        public Dictionary<ConsentFeatures, bool> givenConsent = null;

        /// <summary>
        /// After how many seconds a session update is sent
        /// </summary>
        public int sessionUpdateInterval = 60;
    }
}
