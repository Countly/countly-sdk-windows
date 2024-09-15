using System;
using System.Collections.Generic;
using System.Reflection;
using CountlySDK.Helpers;
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
        /// Folder name that the SDK will use in isolated storage
        /// </summary>
        public string sdkFolderName = "countly";

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
        public int MaxSegmentationValues = 100;

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

        // <summary>
        /// Enable/Disable backend mode
        /// </summary>
        internal bool backendMode = false;

        // <summary>
        /// Maximum event queue threshold
        /// </summary>
        internal int EventQueueThreshold = 10;

        internal int BackendModeAppEQSize = 1000;

        internal int BackendModeServerEQSize = 10000;

        // <summary>
        /// Maximum request queue size
        /// </summary>
        internal int RequestQueueMaxSize = 1000;


        internal string City = null;
        internal string Location = null;
        internal string IPAddress = null;
        internal string CountryCode = null;
        internal bool IsLocationDisabled = false;
        internal IDictionary<string, string> MetricOverride = null;

        internal string salt;
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

        /// <summary>
        /// Enabled backend mode. When backend mode enabled other feature calls are disabled
        /// </summary>
        /// <returns>Config for the call chaining</returns>
        public CountlyConfigBase EnableBackendMode()
        {
            backendMode = true;
            return this;
        }

        /// <summary>
        /// Sets maximum size of the request queue, default value is 1000
        /// This will currently works for only backend mode
        /// </summary>
        /// <param name="requestQueueMaxSize">new request queue size</param> 
        /// <returns>Config for the call chaining</returns>
        public CountlyConfigBase SetMaxRequestQueueSize(int requestQueueMaxSize)
        {
            RequestQueueMaxSize = requestQueueMaxSize;
            return this;
        }

        /// <summary>
        /// Changes the maximum size of the event queue, default value is 10
        /// This will currently works for only backend mode
        /// </summary>
        /// <param name="eventsQueueSize">new event queue size</param>
        /// <returns>Config for the call chaining</returns>
        public CountlyConfigBase SetEventQueueSizeToSend(int eventsQueueSize)
        {
            EventQueueThreshold = eventsQueueSize;
            return this;
        }

        /// <summary>
        /// Changes the maximum size of the event queue size for an app, default value is 1000
        /// This will only work for backend mode
        /// </summary>
        /// <param name="appEQSize"></param>
        /// <returns></returns>
        public CountlyConfigBase SetBackendModeAppEQSizeToSend(int appEQSize)
        {
            BackendModeAppEQSize = appEQSize;
            return this;
        }

        /// <summary>
        /// Changes the maximum size of the event queue size for all of the event queues, default value is 10000
        /// This will only work for backend mode 
        /// </summary>
        /// <param name="serverEQSize"></param>
        /// <returns></returns>
        public CountlyConfigBase SetBackendModeServerEQSizeToSend(int serverEQSize)
        {
            BackendModeServerEQSize = serverEQSize;
            return this;
        }

        public CountlyConfigBase SetMetricOverride(IDictionary<string, string> metricOverride)
        {
            if (metricOverride != null && metricOverride.Count > 0) {
                MetricOverride = metricOverride;
            }

            return this;
        }

        /// <summary>
        /// Enable parameter tampering protection
        /// </summary>
        /// <param name="salt">to add to each request before calculating checksum</param>
        /// <returns>instance for method chaining</returns>
        public CountlyConfigBase EnableParameterTamperingProtection(string salt)
        {
            if (UtilityHelper.IsNullOrEmptyOrWhiteSpace(salt)) {
                UtilityHelper.CountlyLogging("[Config] Salt cannot be empty in enableParameterTamperingProtection");
            } else {
                this.salt = salt;
            }
            return this;
        }
    }
}
