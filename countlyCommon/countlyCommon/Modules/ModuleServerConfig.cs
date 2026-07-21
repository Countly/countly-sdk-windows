using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.CountlyCommon.Server.Responses;
using CountlySDK.Helpers;
using Newtonsoft.Json.Linq;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace CountlySDK.CountlyCommon
{
    internal interface IServerConfigProvider
    {
        bool GetTrackingEnabled();
        bool GetNetworkingEnabled();
        bool GetSessionTrackingEnabled();
        bool GetViewTrackingEnabled();
        bool GetCustomEventTrackingEnabled();
        bool GetCrashReportingEnabled();
        bool GetLocationTrackingEnabled();
        bool GetEnterContentZoneEnabled();
        bool GetRefreshContentZoneEnabled();
    }

    internal class ModuleServerConfig : IServerConfigProvider
    {
        internal const string serverConfigFilename = "server_config.xml";

        private static readonly string[] BoolKeys = { "tracking", "networking", "cr", "log", "st", "vt", "cet", "crt", "lt", "ecz", "rcz" };
        private static readonly string[] PositiveIntKeys = { "rqs", "eqs", "sui", "scui", "lkl", "lvs", "lsv", "lbc", "ltlpt", "ltl", "upcl" };
        // Listing filters (string arrays) + journey trigger events.
        private static readonly string[] StringArrayKeys = { "eb", "ew", "upb", "upw", "sb", "sw", "jte" };
        // Per-event segmentation filters (objects mapping event name -> array of segmentation keys).
        private static readonly string[] ObjectKeys = { "esb", "esw" };
        // If a fetch carries one filter type, the stored opposite type is evicted per category-pairing rules.
        private static readonly string[] WhitelistKeys = { "ew", "upw", "sw", "esw" };
        private static readonly string[] BlacklistKeys = { "eb", "upb", "sb", "esb" };
        // Content zone poll interval; values below 16 seconds are rejected (matches the config setter).
        private const string ContentZoneIntervalKey = "czi";
        private const int ContentZoneIntervalMin = 16;
        // Drop-old-request time in hours; 0 disables the feature.
        private const string DropOldRequestTimeKey = "dort";

        private readonly RequestHelper requestHelper;
        private readonly string ServerUrl;
        private readonly object configLock = new object();

        // Effective gate state (defaults: allowed; only enter-content-zone defaults to off).
        private bool currentTracking = true;
        private bool currentNetworking = true;
        private bool currentSessionTracking = true;
        private bool currentViewTracking = true;
        private bool currentCustomEventTracking = true;
        private bool currentCrashReporting = true;
        private bool currentLocationTracking = true;
        private bool currentEnterContentZone = false;
        private bool currentRefreshContentZone = true;

        // Listing filters: empty set = no filtering. Each is a blacklist unless its whitelist flag is set.
        private HashSet<string> eventFilter = new HashSet<string>();
        private bool eventFilterIsWhitelist = false;
        private HashSet<string> userPropertyFilter = new HashSet<string>();
        private bool userPropertyFilterIsWhitelist = false;
        private HashSet<string> segmentationFilter = new HashSet<string>();
        private bool segmentationFilterIsWhitelist = false;
        private Dictionary<string, HashSet<string>> eventSegmentationFilter = new Dictionary<string, HashSet<string>>();
        private bool eventSegmentationFilterIsWhitelist = false;
        private HashSet<string> journeyTriggerEvents = new HashSet<string>();

        // Requests older than this many hours are dropped from the queue (0 = disabled).
        private int currentDropOldRequestTime = 0;
        private int currentUserPropertyCacheLimit = 100;

        // SBS self-refresh interval in HOURS (server-tunable via 'scui'). Default 4h.
        private int currentServerConfigUpdateInterval = 4;

        private bool updatesDisabled = false;
        private JObject providedConfig = null;   // sanitized inner config from developer-provided settings
        private JObject storedConfig = null;     // sanitized inner config from the persisted envelope
        private JObject storedFullConfig = null; // full {v,t,c} envelope (for merge + persist)

        private System.Threading.Timer refreshTimer = null;

        public ModuleServerConfig(RequestHelper requestHelper, string serverUrl)
        {
            this.requestHelper = requestHelper;
            ServerUrl = serverUrl;
        }

        public bool GetTrackingEnabled() { lock (configLock) { return currentTracking; } }

        public bool GetNetworkingEnabled() { lock (configLock) { return currentNetworking; } }

        public bool GetSessionTrackingEnabled() { lock (configLock) { return currentSessionTracking; } }

        public bool GetViewTrackingEnabled() { lock (configLock) { return currentViewTracking; } }

        public bool GetCustomEventTrackingEnabled() { lock (configLock) { return currentCustomEventTracking; } }

        public bool GetCrashReportingEnabled() { lock (configLock) { return currentCrashReporting; } }

        public bool GetLocationTrackingEnabled() { lock (configLock) { return currentLocationTracking; } }

        public bool GetEnterContentZoneEnabled() { lock (configLock) { return currentEnterContentZone; } }

        public bool GetRefreshContentZoneEnabled() { lock (configLock) { return currentRefreshContentZone; } }

        internal int GetDropOldRequestTimeHours() { lock (configLock) { return currentDropOldRequestTime; } }

        internal int GetUserPropertyCacheLimit() { lock (configLock) { return currentUserPropertyCacheLimit; } }

        internal bool IsEventKeyAllowed(string key) { lock (configLock) { return IsAllowedByFilter(key, eventFilter, eventFilterIsWhitelist); } }

        internal bool IsUserPropertyAllowed(string key) { lock (configLock) { return IsAllowedByFilter(key, userPropertyFilter, userPropertyFilterIsWhitelist); } }

        internal bool IsJourneyTriggerEvent(string key) { lock (configLock) { return journeyTriggerEvents.Contains(key); } }

        /// <summary>Empty filter = everything allowed; whitelist keeps listed keys, blacklist drops them.</summary>
        private static bool IsAllowedByFilter(string key, HashSet<string> filter, bool isWhitelist)
        {
            if (filter.Count == 0) { return true; }
            bool contains = filter.Contains(key);
            return isWhitelist ? contains : !contains;
        }

        /// <summary>Applies the global (sb/sw) then the per-event (esb/esw) segmentation filters in place.</summary>
        internal void FilterEventSegmentation(string eventKey, Segmentation segmentation)
        {
            if (segmentation == null || segmentation.segmentation == null || segmentation.segmentation.Count == 0) { return; }
            lock (configLock) {
                if (segmentationFilter.Count > 0) {
                    RemoveFilteredItems(segmentation, segmentationFilter, segmentationFilterIsWhitelist);
                }
                HashSet<string> eventRules;
                if (eventSegmentationFilter.TryGetValue(eventKey, out eventRules) && eventRules.Count > 0) {
                    RemoveFilteredItems(segmentation, eventRules, eventSegmentationFilterIsWhitelist);
                }
            }
        }

        private static void RemoveFilteredItems(Segmentation segmentation, HashSet<string> filter, bool isWhitelist)
        {
            List<SegmentationItem> items = segmentation.segmentation;
            for (int i = items.Count - 1; i >= 0; i--) {
                if (!IsAllowedByFilter(items[i].Key, filter, isWhitelist)) {
                    UtilityHelper.CountlyLogging("[ModuleServerConfig] FilterEventSegmentation, segmentation key filtered out by server config: " + items[i].Key);
                    items.RemoveAt(i);
                }
            }
        }

        /// <summary>Applies the user property filter (upb/upw) and the cache limit (upcl) to custom user properties.</summary>
        internal Dictionary<string, string> FilterUserProperties(Dictionary<string, string> properties)
        {
            if (properties == null) { return null; }
            lock (configLock) {
                Dictionary<string, string> result = new Dictionary<string, string>();
                foreach (KeyValuePair<string, string> kv in properties) {
                    if (!IsAllowedByFilter(kv.Key, userPropertyFilter, userPropertyFilterIsWhitelist)) {
                        UtilityHelper.CountlyLogging("[ModuleServerConfig] FilterUserProperties, user property filtered out by server config: " + kv.Key);
                        continue;
                    }
                    if (result.Count >= currentUserPropertyCacheLimit) {
                        UtilityHelper.CountlyLogging("[ModuleServerConfig] FilterUserProperties, user property cache limit [" + currentUserPropertyCacheLimit + "] reached, dropping: " + kv.Key, LogLevel.WARNING);
                        continue;
                    }
                    result[kv.Key] = kv.Value;
                }
                return result;
            }
        }

        /// <summary>
        /// Synchronously loads stored settings, parses developer-provided settings, and applies
        /// both (provided first, stored second) to the effective configuration. No network I/O.
        /// </summary>
        internal void InitializeServerConfig(CountlyConfigBase config)
        {
            updatesDisabled = config.sdkBehaviorSettingsUpdatesDisabled;
            providedConfig = Sanitize(ExtractConfigObject(config.providedSdkBehaviorSettings));
            LoadStoredConfig();
            ApplyEffectiveConfig();
        }

        private void LoadStoredConfig()
        {
            ServerConfigEntity entity = Storage.Instance.LoadFromFile<ServerConfigEntity>(serverConfigFilename).Result;
            if (entity == null || string.IsNullOrEmpty(entity.Json)) { return; }
            try {
                JObject env = JObject.Parse(entity.Json);
                if (!ValidateEnvelope(env)) { return; }
                storedFullConfig = env;
                storedConfig = Sanitize((JObject)env["c"]);
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("[ModuleServerConfig] LoadStoredConfig, failed to parse stored config: " + ex.Message, LogLevel.WARNING);
            }
        }

        private void ApplyEffectiveConfig()
        {
            ApplyConfigMap(providedConfig);
            ApplyConfigMap(storedConfig);
        }

        private void ApplyConfigMap(JObject c)
        {
            if (c == null) { return; }
            CountlyConfigBase config = Countly.Instance.Configuration;
            lock (configLock) {
                if (c["tracking"] != null) { currentTracking = c["tracking"].Value<bool>(); }
                if (c["networking"] != null) { currentNetworking = c["networking"].Value<bool>(); }
                if (c["st"] != null) { currentSessionTracking = c["st"].Value<bool>(); }
                if (c["vt"] != null) { currentViewTracking = c["vt"].Value<bool>(); }
                if (c["cet"] != null) { currentCustomEventTracking = c["cet"].Value<bool>(); }
                if (c["crt"] != null) { currentCrashReporting = c["crt"].Value<bool>(); }
                if (c["lt"] != null) { currentLocationTracking = c["lt"].Value<bool>(); }
                if (c["ecz"] != null) { currentEnterContentZone = c["ecz"].Value<bool>(); }
                if (c["rcz"] != null) { currentRefreshContentZone = c["rcz"].Value<bool>(); }
                if (c[DropOldRequestTimeKey] != null) { currentDropOldRequestTime = c[DropOldRequestTimeKey].Value<int>(); }
                if (c["upcl"] != null) { currentUserPropertyCacheLimit = c["upcl"].Value<int>(); }
                if (c["eb"] != null) { eventFilter = ExtractStringSet(c["eb"]); eventFilterIsWhitelist = false; } else if (c["ew"] != null) { eventFilter = ExtractStringSet(c["ew"]); eventFilterIsWhitelist = true; }
                if (c["upb"] != null) { userPropertyFilter = ExtractStringSet(c["upb"]); userPropertyFilterIsWhitelist = false; } else if (c["upw"] != null) { userPropertyFilter = ExtractStringSet(c["upw"]); userPropertyFilterIsWhitelist = true; }
                if (c["sb"] != null) { segmentationFilter = ExtractStringSet(c["sb"]); segmentationFilterIsWhitelist = false; } else if (c["sw"] != null) { segmentationFilter = ExtractStringSet(c["sw"]); segmentationFilterIsWhitelist = true; }
                if (c["esb"] != null) { eventSegmentationFilter = ExtractStringSetMap(c["esb"]); eventSegmentationFilterIsWhitelist = false; } else if (c["esw"] != null) { eventSegmentationFilter = ExtractStringSetMap(c["esw"]); eventSegmentationFilterIsWhitelist = true; }
                if (c["jte"] != null) { journeyTriggerEvents = ExtractStringSet(c["jte"]); }
                if (c["log"] != null) { Countly.IsLoggingEnabled = c["log"].Value<bool>(); }
                if (c["scui"] != null) { currentServerConfigUpdateInterval = c["scui"].Value<int>(); }
                if (config != null) {
                    if (c["cr"] != null) { config.consentRequired = config.consentRequired || c["cr"].Value<bool>(); }
                    if (c["lkl"] != null) { config.MaxKeyLength = c["lkl"].Value<int>(); }
                    if (c["lvs"] != null) { config.MaxValueSize = c["lvs"].Value<int>(); }
                    if (c["lsv"] != null) { config.MaxSegmentationValues = c["lsv"].Value<int>(); }
                    if (c["lbc"] != null) { config.MaxBreadcrumbCount = c["lbc"].Value<int>(); }
                    if (c["ltlpt"] != null) { config.MaxStackTraceLinesPerThread = c["ltlpt"].Value<int>(); }
                    if (c["ltl"] != null) { config.MaxStackTraceLineLength = c["ltl"].Value<int>(); }
                    if (c["rqs"] != null) { config.RequestQueueMaxSize = c["rqs"].Value<int>(); }
                    if (c["eqs"] != null) { config.EventQueueThreshold = c["eqs"].Value<int>(); }
                    if (c["sui"] != null) { config.sessionUpdateInterval = c["sui"].Value<int>(); }
                    if (c[ContentZoneIntervalKey] != null) { config.ContentZoneTimerInterval = c[ContentZoneIntervalKey].Value<int>(); }
                }
            }
        }

        /// <summary>Collects the string entries of a JSON array (non-strings are skipped).</summary>
        private HashSet<string> ExtractStringSet(JToken arr)
        {
            HashSet<string> result = new HashSet<string>();
            foreach (JToken t in (JArray)arr) {
                if (t.Type == JTokenType.String) { result.Add(t.Value<string>()); }
            }
            return result;
        }

        /// <summary>Collects an event-name -> segmentation-key-set map from a JSON object of arrays.</summary>
        private Dictionary<string, HashSet<string>> ExtractStringSetMap(JToken obj)
        {
            Dictionary<string, HashSet<string>> result = new Dictionary<string, HashSet<string>>();
            foreach (KeyValuePair<string, JToken> kv in (JObject)obj) {
                if (kv.Value.Type == JTokenType.Array) { result[kv.Key] = ExtractStringSet(kv.Value); }
            }
            return result;
        }

        /// <summary>Returns the inner config object: the "c" child if present, else the whole object.</summary>
        private JObject ExtractConfigObject(string json)
        {
            if (string.IsNullOrEmpty(json)) { return null; }
            try {
                JObject parsed = JObject.Parse(json);
                JToken inner = parsed["c"];
                if (inner != null && inner.Type == JTokenType.Object) { return (JObject)inner; }
                return parsed;
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("[ModuleServerConfig] ExtractConfigObject, failed to parse: " + ex.Message, LogLevel.WARNING);
                return null;
            }
        }

        /// <summary>Envelope is valid iff it has v, t, and a non-empty c object.</summary>
        private bool ValidateEnvelope(JObject env)
        {
            if (env == null) { return false; }
            if (env["v"] == null || env["t"] == null || env["c"] == null) { return false; }
            JObject c = env["c"] as JObject;
            return c != null && c.HasValues;
        }

        /// <summary>Keeps only known keys with valid types/ranges; drops (and logs) the rest.</summary>
        private JObject Sanitize(JObject c)
        {
            JObject result = new JObject();
            if (c == null) { return result; }
            foreach (KeyValuePair<string, JToken> kv in c) {
                string key = kv.Key;
                JToken val = kv.Value;
                if (Array.IndexOf(BoolKeys, key) >= 0) {
                    if (val.Type == JTokenType.Boolean) { result[key] = val; } else {
                        UtilityHelper.CountlyLogging("[ModuleServerConfig] Sanitize, dropping non-boolean key: " + key, LogLevel.WARNING);
                    }
                } else if (Array.IndexOf(PositiveIntKeys, key) >= 0) {
                    if (val.Type == JTokenType.Integer && val.Value<long>() > 0) { result[key] = val; } else {
                        UtilityHelper.CountlyLogging("[ModuleServerConfig] Sanitize, dropping invalid positive-int key: " + key, LogLevel.WARNING);
                    }
                } else if (key == ContentZoneIntervalKey) {
                    if (val.Type == JTokenType.Integer && val.Value<long>() >= ContentZoneIntervalMin) { result[key] = val; } else {
                        UtilityHelper.CountlyLogging("[ModuleServerConfig] Sanitize, dropping invalid content zone interval (must be an integer >= " + ContentZoneIntervalMin + "): " + key, LogLevel.WARNING);
                    }
                } else if (key == DropOldRequestTimeKey) {
                    if (val.Type == JTokenType.Integer && val.Value<long>() >= 0) { result[key] = val; } else {
                        UtilityHelper.CountlyLogging("[ModuleServerConfig] Sanitize, dropping invalid non-negative-int key: " + key, LogLevel.WARNING);
                    }
                } else if (Array.IndexOf(StringArrayKeys, key) >= 0) {
                    if (val.Type == JTokenType.Array) { result[key] = val; } else {
                        UtilityHelper.CountlyLogging("[ModuleServerConfig] Sanitize, dropping non-array key: " + key, LogLevel.WARNING);
                    }
                } else if (Array.IndexOf(ObjectKeys, key) >= 0) {
                    if (val.Type == JTokenType.Object) { result[key] = val; } else {
                        UtilityHelper.CountlyLogging("[ModuleServerConfig] Sanitize, dropping non-object key: " + key, LogLevel.WARNING);
                    }
                } else {
                    UtilityHelper.CountlyLogging("[ModuleServerConfig] Sanitize, dropping unsupported key: " + key, LogLevel.WARNING);
                }
            }
            return result;
        }

        /// <summary>
        /// Network fetch of SDK Behavior Settings (direct, off-queue, /o/sdk?method=sc).
        /// No-op when updates are disabled. Always (re)starts the refresh timer when enabled,
        /// so periodic refresh survives a failed fetch.
        /// </summary>
        internal async Task FetchServerConfig()
        {
            if (updatesDisabled) {
                UtilityHelper.CountlyLogging("[ModuleServerConfig] FetchServerConfig, updates disabled, skipping network fetch");
                return;
            }
            await FetchAndApply();
            StartTimer();
        }

        private async Task FetchAndApply()
        {
            IDictionary<string, object> scParams = new Dictionary<string, object> { { "method", "sc" } };
            RequestResult requestResult = await Api.Instance.SendDirectRequest(ServerUrl, await requestHelper.BuildRequest(scParams));
            UtilityHelper.CountlyLogging("[ModuleServerConfig] FetchServerConfig, response code: [" + requestResult.responseCode + "], text: [" + requestResult.responseText + "]");

            if (requestResult.responseCode != 200 || requestResult.responseText == null) {
                UtilityHelper.CountlyLogging("[ModuleServerConfig] FetchServerConfig, request failed, keeping current settings", LogLevel.WARNING);
                return;
            }

            JObject env;
            try {
                env = JObject.Parse(requestResult.responseText);
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("[ModuleServerConfig] FetchServerConfig, failed to parse response: " + ex.Message, LogLevel.WARNING);
                return;
            }

            if (!ValidateEnvelope(env)) {
                UtilityHelper.CountlyLogging("[ModuleServerConfig] FetchServerConfig, invalid envelope, keeping current settings", LogLevel.WARNING);
                return;
            }

            MergeAndPersist(env);
            ApplyEffectiveConfig();
        }

        private void MergeAndPersist(JObject env)
        {
            JObject incoming = Sanitize((JObject)env["c"]);
            lock (configLock) {
                if (storedFullConfig == null) { storedFullConfig = new JObject(); }
                storedFullConfig["v"] = env["v"];
                storedFullConfig["t"] = env["t"];
                if (storedConfig == null) { storedConfig = new JObject(); }
                EvictOppositeFilterKeys(incoming, storedConfig);
                foreach (KeyValuePair<string, JToken> kv in incoming) {
                    storedConfig[kv.Key] = kv.Value;
                }
                storedFullConfig["c"] = storedConfig;
            }
            PersistStoredConfig();
        }

        /// <summary>
        /// When a fetch carries filters of one type, the stored filters of the opposite type are
        /// removed so a server-side switch between blacklists and whitelists fully replaces the old set.
        /// </summary>
        private void EvictOppositeFilterKeys(JObject incoming, JObject stored)
        {
            bool hasWhitelist = false, hasBlacklist = false;
            foreach (string key in WhitelistKeys) { if (incoming[key] != null) { hasWhitelist = true; } }
            foreach (string key in BlacklistKeys) { if (incoming[key] != null) { hasBlacklist = true; } }
            if (hasWhitelist) { foreach (string key in BlacklistKeys) { stored.Remove(key); } }
            if (hasBlacklist) { foreach (string key in WhitelistKeys) { stored.Remove(key); } }
        }

        private void PersistStoredConfig()
        {
            string json;
            lock (configLock) {
                if (storedFullConfig == null) { return; }
                json = storedFullConfig.ToString(Newtonsoft.Json.Formatting.None);
            }
            Storage.Instance.SaveToFile<ServerConfigEntity>(serverConfigFilename, new ServerConfigEntity { Json = json }).Wait();
        }

        private void StartTimer()
        {
            StopTimer();
            long intervalMs = (long)currentServerConfigUpdateInterval * 60L * 60L * 1000L;
            refreshTimer = new System.Threading.Timer(OnTimerTick, null, intervalMs, intervalMs);
        }

        private void OnTimerTick(object state)
        {
            FetchServerConfig().Wait();
        }

        internal void StopTimer()
        {
            if (refreshTimer != null) {
                refreshTimer.Dispose();
                refreshTimer = null;
            }
        }
    }

    [DataContract]
    internal class ServerConfigEntity
    {
        [DataMember]
        public string Json;
    }
}
