using System.Collections.Generic;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace CountlySDK.CountlyCommon.Entities
{
    public class CountlyConfigBase
    {
        /// <summary>
        /// Server url
        /// [Mandatory field]
        /// </summary>
        public string serverUrl;

        /// <summary>
        /// Application key
        /// [Mandatory field]
        /// </summary>
        public string appKey;

        /// <summary>
        /// Application version
        /// [Mandatory field]
        /// </summary>
        public string appVersion;

        /// <summary>
        /// Device Id provided by the developer. If value is not null,
        /// changes the DeviceIdMethod to 'developerSupplied' and overwrites
        /// the previously used/saved deviceId
        /// </summary>
        public string developerProvidedDeviceId = null;

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

        // <summary>
        /// Maximum size of all string keys
        /// </summary>
        public int MaxKeyLength = 128;

        /// <summary>
        /// Maximum size of all values in our key-value pairs
        /// </summary>
        public int MaxValueSize = 256;

        /// <summary>
        /// Max amount of custom (dev provided) segmentation in one event
        /// </summary>
        public int MaxSegmentationValues = 30;

        /// <summary>
        /// Limits how many stack trace lines would be recorded per thread
        /// </summary>
        public int MaxStackTraceLinesPerThread = 30;

        /// <summary>
        /// Limits how many characters are allowed per stack trace line
        /// </summary>
        public int MaxStackTraceLineLength = 200;

        // <summary>
        /// Set the maximum amount of breadcrumbs.
        /// </summary>
        public int MaxBreadcrumbCount = 100;

        internal string City = null;
        internal string Location = null;
        internal string IPAddress = null;
        internal string CountryCode = null;
        internal bool IsLocationDisabled = false;

        /// <summary>
        /// Disabled the location tracking on the Countly server
        /// </summary>
        public void DisableLocation()
        {
            IsLocationDisabled = true;
        }

        /// <summary>
        /// Set location parameters that will be used during init.
        /// </summary>
        /// <param name="countryCode">ISO Country code for the user's country</param>
        /// <param name="city">Name of the user's city</param>
        /// <param name="gpsCoordinates">comma separate lat and long values.<example>"56.42345,123.45325"</example> </param>
        /// <param name="ipAddress">user's IP Address</param>
        /// <returns></returns>
        public void SetLocation(string countryCode, string city, string gpsCoordinates, string ipAddress)
        {
            City = city;
            IPAddress = ipAddress;
            CountryCode = countryCode;
            Location = gpsCoordinates;
        }
    }
}
