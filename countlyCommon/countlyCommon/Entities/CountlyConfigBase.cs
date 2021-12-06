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
    }
}
