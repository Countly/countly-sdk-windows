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
    }

    internal class ModuleServerConfig : IServerConfigProvider
    {
        internal const string serverConfigFilename = "server_config.xml";

        private static readonly string[] BoolKeys = { "tracking", "networking", "cr", "log" };
        private static readonly string[] PositiveIntKeys = { "rqs", "eqs", "sui", "scui", "lkl", "lvs", "lsv", "lbc", "ltlpt", "ltl" };

        private readonly RequestHelper requestHelper;
        private readonly string ServerUrl;
        private readonly object configLock = new object();

        // Effective gate state (defaults: allowed).
        private bool currentTracking = true;
        private bool currentNetworking = true;

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
                }
            }
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
                } else {
                    UtilityHelper.CountlyLogging("[ModuleServerConfig] Sanitize, dropping unsupported key: " + key, LogLevel.WARNING);
                }
            }
            return result;
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
