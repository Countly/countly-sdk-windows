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

        private int _contentZoneTimerInterval = 30;

        /// <summary>
        /// Content zone poll interval in seconds. Values &lt;= 15 are ignored. Default 30. (Experimental.)
        /// </summary>
        public int ContentZoneTimerInterval {
            get { return _contentZoneTimerInterval; }
            set { if (value > 15) { _contentZoneTimerInterval = value; } }
        }

        /// <summary>Optional callback invoked when a shown content item is closed. (Experimental.)</summary>
        public System.Action GlobalContentCallback { get; set; }

        /// <summary>
        /// Optional listener invoked for every SDK log message, independent of the console
        /// logging flag (Countly.IsLoggingEnabled). Receives the log message and its level.
        /// </summary>
        public System.Action<string, LogLevel> LogListener { get; set; }

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
        internal string TamperingProtectionSalt = null;


        internal string City = null;
        internal string Location = null;
        internal string IPAddress = null;
        internal string CountryCode = null;
        internal bool IsLocationDisabled = false;
        internal IDictionary<string, string> MetricOverride = null;
        internal IDictionary<string, string> CustomNetworkRequestHeaders = null;

        internal bool manualUserDetailsSave = true;
        internal bool autoSendUserDetails = true;
        internal bool remoteConfigAutomaticDownloadTriggers = false;

        // <summary>
        /// Developer-provided SDK Behavior Settings (Server Config) JSON, applied as a
        /// precedence layer below server-fetched settings. May be a full {v,t,c} envelope
        /// or a bare config object.
        /// </summary>
        internal string providedSdkBehaviorSettings = null;

        // <summary>
        /// When true, the SDK does NOT perform the network fetch of SDK Behavior Settings
        /// (nor its refresh timer). Provided and on-disk settings still load and apply.
        /// </summary>
        internal bool sdkBehaviorSettingsUpdatesDisabled = false;

        // <summary>
        /// When true, the SDK still accumulates health counters but never sends the
        /// health check request.
        /// </summary>
        internal bool healthCheckDisabled = false;

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

        /// <summary>
        /// Allows you to add custom metric key/value pairs
        /// </summary>
        /// <param name="metricOverride"></param>
        /// <returns></returns>
        public CountlyConfigBase SetMetricOverride(IDictionary<string, string> metricOverride)
        {
            if (metricOverride != null && metricOverride.Count > 0) {
                MetricOverride = metricOverride;
            }

            return this;
        }

        /// <summary>
        /// Allows you to add custom header key/value pairs to each request
        /// </summary>
        /// <param name="customNetworkRequestHeaders"></param>
        /// <returns></returns>
        public CountlyConfigBase AddCustomNetworkRequestHeaders(IDictionary<string, string> customNetworkRequestHeaders)
        {
            if (customNetworkRequestHeaders != null && customNetworkRequestHeaders.Count > 0) {
                CustomNetworkRequestHeaders = customNetworkRequestHeaders;
            }

            return this;
        }

        /// <summary>
        /// Salt to hash all requests
        /// </summary>
        /// <param name="paramaterTamperingProtectionSalt"></param>
        /// <returns></returns>
        public CountlyConfigBase SetParamaterTamperingProtectionSalt(string paramaterTamperingProtectionSalt)
        {
            if (!string.IsNullOrEmpty(paramaterTamperingProtectionSalt)) {
                TamperingProtectionSalt = paramaterTamperingProtectionSalt;
            }
            return this;
        }

        /// <summary>
        /// Disables manual user details save. By default manual user details save is enabled.
        /// This reverts the fix that all edit user details was not saved. And this is only for testing purposes
        /// </summary>
        /// <returns></returns>
        internal CountlyConfigBase DisableManualUserDetailsSave()
        {
            manualUserDetailsSave = false;
            return this;
        }

        /// <summary>
        /// Disable automatic sending of user properties on
        /// - When an event is recorded
        /// - During an internal timer tick
        /// - Upon flushing the event queue
        /// - When a session call made
        /// </summary>
        /// <returns></returns>
        public CountlyConfigBase DisableAutoSendUserDetails()
        {
            autoSendUserDetails = false;
            return this;
        }

        /// <summary>
        /// Enables automatic Remote Config download triggers.
        /// When enabled, the SDK will automatically initiate Remote Config downloads
        /// at specific lifecycle points such as SDK initialization completion,
        /// device ID changes, and consent being granted.
        /// </summary>
        /// <returns>
        /// </returns>
        public CountlyConfigBase EnableRemoteConfigAutomaticTriggers()
        {
            remoteConfigAutomaticDownloadTriggers = true;
            return this;
        }

        /// <summary>
        /// Seed developer-provided SDK Behavior Settings (Server Config). Used as a fallback
        /// source before/instead of stored settings (e.g. first run / offline). Lower precedence
        /// than settings fetched and stored from the server.
        /// </summary>
        /// <param name="sdkBehaviorSettings">A {v,t,c} envelope or a bare config object as JSON.</param>
        /// <returns>Config for call chaining</returns>
        public CountlyConfigBase SetSDKBehaviorSettings(string sdkBehaviorSettings)
        {
            providedSdkBehaviorSettings = sdkBehaviorSettings;
            return this;
        }

        /// <summary>
        /// Disables ONLY the network fetch (and refresh timer) of SDK Behavior Settings.
        /// Developer-provided and on-disk (last-known-good) settings still load and apply.
        /// </summary>
        /// <returns>Config for call chaining</returns>
        public CountlyConfigBase DisableSDKBehaviorSettingsUpdates()
        {
            sdkBehaviorSettingsUpdatesDisabled = true;
            return this;
        }

        /// <summary>
        /// Disables the SDK health check feature entirely. Counters still accumulate in
        /// memory, but no health check request is sent during initialization.
        /// </summary>
        /// <returns>Config for call chaining</returns>
        public CountlyConfigBase DisableHealthCheck()
        {
            healthCheckDisabled = true;
            return this;
        }

        /// <summary>
        /// Sets a listener that receives every SDK log message and its level. Fires regardless
        /// of whether console logging is enabled. A throwing listener cannot break the SDK.
        /// </summary>
        /// <param name="logListener">Callback receiving (message, level).</param>
        /// <returns>Config for call chaining</returns>
        public CountlyConfigBase SetLogListener(System.Action<string, LogLevel> logListener)
        {
            LogListener = logListener;
            return this;
        }
    }
}
