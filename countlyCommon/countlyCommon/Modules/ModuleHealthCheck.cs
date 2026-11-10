using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.CountlyCommon.Server.Responses;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace CountlySDK.CountlyCommon
{
    internal class ModuleHealthCheck
    {
        internal const string healthCheckFilename = "health_check.xml";

        // a method usage or log code count stops growing here until it is reported
        internal const int countCap = 65535;

        // feature areas of the method usage; each follows the consent of the same name
        internal static readonly string[] areas = { "sessions", "events", "views", "crashes" };

        // what was counted while no module was registered: before init, or after halt
        private static readonly object pendingLock = new object();
        private static int pendingWarnings = 0;
        private static int pendingErrors = 0;
        private static readonly Dictionary<string, int> pendingLogCodes = new Dictionary<string, int>();
        private static ModuleHealthCheck registered = null;

        private readonly RequestHelper requestHelper;
        private readonly Func<string> ServerUrl;
        private readonly string appVersion;
        private readonly bool disabled;
        private readonly Func<string, bool> areaAllowed;
        private readonly object hcLock = new object();

        // Counters since the last successful health check submission.
        private int countWarn = 0;
        private int countError = 0;
        private int statusCode = -1;
        private string errorMessage = "";
        // Backoff counters: no backoff mechanism exists yet; serialized as 0 for wire compatibility.
        private int backoff = 0;
        private int consecutiveBackoff = 0;
        private Dictionary<string, Dictionary<string, int>> methodUsage = new Dictionary<string, Dictionary<string, int>>();
        private Dictionary<string, int> logCodes = new Dictionary<string, int>();

        private bool healthCheckSent = false;

        /// <summary>
        /// Creates the health check module and reads the counters an earlier run stored
        /// </summary>
        /// <param name="requestHelper">builds the base request parameters</param>
        /// <param name="serverUrl">gives the server the health check is sent to</param>
        /// <param name="disabled">true when the health check request is turned off</param>
        /// <param name="appVersion">app version the health check reports, the overridden one when set</param>
        /// <param name="areaAllowed">tells whether a feature area may be counted now, by its consent; null counts every area</param>
        internal ModuleHealthCheck(RequestHelper requestHelper, Func<string> serverUrl, bool disabled, string appVersion, Func<string, bool> areaAllowed = null)
        {
            this.requestHelper = requestHelper;
            ServerUrl = serverUrl;
            this.disabled = disabled;
            this.appVersion = appVersion;
            this.areaAllowed = areaAllowed;
            LoadState();
        }

        // ---- static hook wiring ----
        /// <summary>
        /// Makes this module count the warning and error logs and the log codes of the SDK, and adds what was
        /// counted while no module was registered
        /// </summary>
        internal void RegisterHooks()
        {
            UtilityHelper.InternalLogHook = OnInternalLog;
            int warnings;
            int errors;
            Dictionary<string, int> codes;
            lock (pendingLock) {
                registered = this;
                warnings = pendingWarnings;
                errors = pendingErrors;
                codes = new Dictionary<string, int>(pendingLogCodes);
                pendingWarnings = 0;
                pendingErrors = 0;
                pendingLogCodes.Clear();
            }

            lock (hcLock) {
                countWarn += warnings;
                countError += errors;
                foreach (KeyValuePair<string, int> code in codes) {
                    AddCount(logCodes, code.Key, code.Value);
                }
            }
        }

        /// <summary>
        /// Stops this module from counting the logs and log codes of the SDK
        /// </summary>
        internal void UnregisterHooks()
        {
            if (UtilityHelper.InternalLogHook == (Action<LogLevel>)OnInternalLog) {
                UtilityHelper.InternalLogHook = null;
            }
            lock (pendingLock) {
                if (registered == this) {
                    registered = null;
                }
            }
        }

        private void OnInternalLog(LogLevel level)
        {
            if (level == LogLevel.WARNING) { LogWarning(); } else if (level == LogLevel.ERROR) { LogError(); }
        }

        /// <summary>
        /// Counts a warning or error log line written while no module is registered, so the next registered module adds it
        /// </summary>
        /// <param name="level">level of the log line</param>
        internal static void CountBeforeModule(LogLevel level)
        {
            lock (pendingLock) {
                if (level == LogLevel.WARNING) { pendingWarnings++; } else if (level == LogLevel.ERROR) { pendingErrors++; }
            }
        }

        /// <summary>
        /// Counts a known problem for the health check, next to the log line that reports it. While no module is
        /// registered, before init or after halt, it is kept for the next registered module.
        /// </summary>
        /// <param name="code">log code, a level letter and three digits, for example "w202"</param>
        /// <param name="area">feature area of the public method that records it; the code is then counted only while that feature has consent</param>
        internal static void Track(string code, string area = null)
        {
            ModuleHealthCheck module;
            lock (pendingLock) {
                module = registered;
                if (module == null) {
                    if (!string.IsNullOrEmpty(code)) {
                        AddCount(pendingLogCodes, code, 1);
                    }
                    return;
                }
            }
            module.RecordLogCode(code, area);
        }

        /// <summary>
        /// Forgets what was counted while no module was registered, when the SDK is halted
        /// </summary>
        internal static void ClearPending()
        {
            lock (pendingLock) {
                pendingWarnings = 0;
                pendingErrors = 0;
                pendingLogCodes.Clear();
            }
        }

        // ---- test/read accessors ----
        internal int WarningCount { get { lock (hcLock) { return countWarn; } } }
        internal int ErrorCount { get { lock (hcLock) { return countError; } } }
        internal int LastStatusCode { get { lock (hcLock) { return statusCode; } } }
        internal string LastErrorMessage { get { lock (hcLock) { return errorMessage; } } }

        /// <summary>
        /// How often a method of a feature area was counted
        /// </summary>
        /// <param name="area">feature area</param>
        /// <param name="method">method name with its modifiers</param>
        /// <returns>the count, 0 when not counted</returns>
        internal int UsageCount(string area, string method)
        {
            lock (hcLock) {
                Dictionary<string, int> methods;
                int count;
                return methodUsage.TryGetValue(area, out methods) && methods.TryGetValue(method, out count) ? count : 0;
            }
        }

        /// <summary>
        /// How often a log code was counted
        /// </summary>
        /// <param name="code">log code</param>
        /// <returns>the count, 0 when not counted</returns>
        internal int LogCodeCount(string code)
        {
            lock (hcLock) {
                int count;
                return logCodes.TryGetValue(code, out count) ? count : 0;
            }
        }

        // ---- counter accumulation ----
        internal void LogWarning() { lock (hcLock) { countWarn++; } }

        internal void LogError() { lock (hcLock) { countError++; } }

        /// <summary>
        /// Keeps the status code and the answer of the last queued request the server answered with a failure.
        /// A request that got no answer has no valid status code and changes nothing.
        /// </summary>
        /// <param name="statusCodeIn">HTTP status code</param>
        /// <param name="errorResponse">answer body</param>
        internal void LogFailedNetworkRequest(int statusCodeIn, string errorResponse)
        {
            if (statusCodeIn <= 0 || statusCodeIn >= 1000) {
                return;
            }
            if (errorResponse == null) { errorResponse = ""; }
            if (errorResponse.Length > 1000) { errorResponse = errorResponse.Substring(0, 1000); }
            lock (hcLock) {
                statusCode = statusCodeIn;
                errorMessage = errorResponse;
            }
        }

        /// <summary>
        /// Counts a call of a method of the session, event, view or crash feature, only while the feature has consent
        /// </summary>
        /// <param name="area">"sessions", "events", "views" or "crashes"</param>
        /// <param name="method">method name, with ":a" for a call the SDK made by itself and ":d" for a deprecated method</param>
        internal void RecordUsage(string area, string method)
        {
            if (string.IsNullOrEmpty(area) || string.IsNullOrEmpty(method) || !areas.Contains(area) || !IsAreaAllowed(area)) {
                return;
            }
            lock (hcLock) {
                Dictionary<string, int> methods;
                if (!methodUsage.TryGetValue(area, out methods)) {
                    methods = new Dictionary<string, int>();
                    methodUsage[area] = methods;
                }
                AddCount(methods, method, 1);
            }
        }

        /// <summary>
        /// Counts a known problem. A code that a method of a feature area records is counted only while that feature has consent.
        /// </summary>
        /// <param name="code">log code, a level letter and three digits</param>
        /// <param name="area">feature area of the method that records it, null when it has none</param>
        internal void RecordLogCode(string code, string area = null)
        {
            if (string.IsNullOrEmpty(code) || (area != null && areas.Contains(area) && !IsAreaAllowed(area))) {
                return;
            }
            lock (hcLock) {
                AddCount(logCodes, code, 1);
            }
        }

        private bool IsAreaAllowed(string area)
        {
            return areaAllowed == null || areaAllowed(area);
        }

        private static void AddCount(Dictionary<string, int> counts, string name, int amount)
        {
            int current;
            counts.TryGetValue(name, out current);
            counts[name] = (int)Math.Min((long)current + amount, countCap);
        }

        // ---- persistence ----
        private void LoadState()
        {
            HealthCheckEntity e = Storage.Instance.LoadFromFile<HealthCheckEntity>(healthCheckFilename).Result;
            if (e == null || string.IsNullOrEmpty(e.Json)) { return; }

            JObject o;
            try {
                o = JObject.Parse(e.Json);
            } catch (Exception ex) {
                // this module may not log warnings or errors, since it counts them itself
                UtilityHelper.CountlyLogging("[ModuleHealthCheck] malformed stored state, resetting: " + ex.Message);
                ClearAndSave();
                lock (hcLock) {
                    AddCount(logCodes, "e503", 1);
                }
                return;
            }

            lock (hcLock) {
                countError = ReadCount(o, "LErr", 0);
                countWarn = ReadCount(o, "LWar", 0);
                statusCode = o["RStatC"]?.Type == JTokenType.Integer ? o.Value<int>("RStatC") : -1;
                errorMessage = o["REMsg"]?.Type == JTokenType.String ? o.Value<string>("REMsg") : "";
                backoff = ReadCount(o, "BReq", 0);
                consecutiveBackoff = ReadCount(o, "CBReq", 0);

                JObject storedUsage = o["MU"] as JObject;
                if (storedUsage != null) {
                    foreach (JProperty area in storedUsage.Properties()) {
                        JObject storedMethods = area.Value as JObject;
                        if (!areas.Contains(area.Name) || storedMethods == null) {
                            continue;
                        }
                        foreach (JProperty method in storedMethods.Properties()) {
                            if (method.Value.Type == JTokenType.Integer && method.Value.Value<long>() > 0) {
                                Dictionary<string, int> methods;
                                if (!methodUsage.TryGetValue(area.Name, out methods)) {
                                    methods = new Dictionary<string, int>();
                                    methodUsage[area.Name] = methods;
                                }
                                AddCount(methods, method.Name, (int)Math.Min(method.Value.Value<long>(), countCap));
                            }
                        }
                    }
                }

                JObject storedCodes = o["LC"] as JObject;
                if (storedCodes != null) {
                    foreach (JProperty code in storedCodes.Properties()) {
                        if (code.Value.Type == JTokenType.Integer && code.Value.Value<long>() > 0) {
                            AddCount(logCodes, code.Name, (int)Math.Min(code.Value.Value<long>(), countCap));
                        }
                    }
                }
            }
        }

        private static int ReadCount(JObject o, string key, int fallback)
        {
            JToken token = o[key];
            if (token == null || token.Type != JTokenType.Integer) {
                return fallback;
            }
            long value = token.Value<long>();
            return value < 0 ? fallback : (int)Math.Min(value, int.MaxValue);
        }

        /// <summary>
        /// Stores all counters, the method usage and log codes included
        /// </summary>
        internal void SaveState()
        {
            JObject o = new JObject();
            lock (hcLock) {
                o["LErr"] = countError;
                o["LWar"] = countWarn;
                o["RStatC"] = statusCode;
                o["REMsg"] = errorMessage;
                o["BReq"] = backoff;
                o["CBReq"] = consecutiveBackoff;
                o["MU"] = UsageToJson();
                o["LC"] = JObject.FromObject(logCodes);
            }
            Storage.Instance.SaveToFile<HealthCheckEntity>(healthCheckFilename,
                new HealthCheckEntity { Json = o.ToString(Formatting.None) }).Wait();
        }

        internal void ClearAndSave()
        {
            ResetCounters();
            Storage.Instance.SaveToFile<HealthCheckEntity>(healthCheckFilename,
                new HealthCheckEntity { Json = "" }).Wait();
        }

        private void ResetCounters()
        {
            lock (hcLock) {
                countWarn = 0;
                countError = 0;
                statusCode = -1;
                errorMessage = "";
                backoff = 0;
                consecutiveBackoff = 0;
                methodUsage.Clear();
                logCodes.Clear();
            }
        }

        // called with hcLock held
        private JObject UsageToJson()
        {
            JObject usage = new JObject();
            foreach (KeyValuePair<string, Dictionary<string, int>> area in methodUsage) {
                usage[area.Key] = JObject.FromObject(area.Value);
            }
            return usage;
        }

        /// <summary>
        /// The counters as the health check reports them
        /// </summary>
        /// <returns>a copy of the counters</returns>
        internal HealthSnapshot Snapshot()
        {
            lock (hcLock) {
                HealthSnapshot snapshot = new HealthSnapshot {
                    el = countError,
                    wl = countWarn,
                    sc = statusCode,
                    em = errorMessage,
                    bom = backoff,
                    cbom = consecutiveBackoff,
                    lc = new Dictionary<string, int>(logCodes)
                };
                foreach (KeyValuePair<string, Dictionary<string, int>> area in methodUsage) {
                    snapshot.fu[area.Key] = new Dictionary<string, int>(area.Value);
                }
                return snapshot;
            }
        }

        /// <summary>
        /// Takes away what a health check the server accepted reported, so what was counted while it was on its way is
        /// reported next time. The last failed request and the longest backoff run are cleared only while they still
        /// hold the reported values. The remaining counters are stored.
        /// </summary>
        /// <param name="sent">the snapshot the delivered health check was built from</param>
        internal void ClearSentValues(HealthSnapshot sent)
        {
            lock (hcLock) {
                countError = Math.Max(0, countError - sent.el);
                countWarn = Math.Max(0, countWarn - sent.wl);
                backoff = Math.Max(0, backoff - sent.bom);
                if (consecutiveBackoff == sent.cbom) {
                    consecutiveBackoff = 0;
                }
                if (statusCode == sent.sc && errorMessage == sent.em) {
                    statusCode = -1;
                    errorMessage = "";
                }
                foreach (KeyValuePair<string, Dictionary<string, int>> area in sent.fu) {
                    Dictionary<string, int> methods;
                    if (!methodUsage.TryGetValue(area.Key, out methods)) {
                        continue;
                    }
                    SubtractCounts(methods, area.Value);
                    if (methods.Count == 0) {
                        methodUsage.Remove(area.Key);
                    }
                }
                SubtractCounts(logCodes, sent.lc);
            }
            SaveState();
        }

        private static void SubtractCounts(Dictionary<string, int> counts, Dictionary<string, int> sent)
        {
            foreach (KeyValuePair<string, int> entry in sent) {
                int current;
                counts.TryGetValue(entry.Key, out current);
                int left = current - entry.Value;
                if (left > 0) {
                    counts[entry.Key] = left;
                } else {
                    counts.Remove(entry.Key);
                }
            }
        }

        // ---- send ----
        /// <summary>
        /// Sends the health check once per SDK start, as a direct request. Once the server confirms it, the reported
        /// amounts are taken away from the counters; otherwise everything is sent again at the next start. Logs
        /// nothing at warning or error level, since such a log would be counted here again.
        /// </summary>
        internal async Task SendHealthCheck()
        {
            if (disabled) {
                UtilityHelper.CountlyLogging("[ModuleHealthCheck] SendHealthCheck, disabled, skipping");
                return;
            }

            lock (hcLock) {
                if (healthCheckSent) {
                    UtilityHelper.CountlyLogging("[ModuleHealthCheck] SendHealthCheck, already sent this lifecycle, skipping");
                    return;
                }
                healthCheckSent = true;
            }

            HealthSnapshot sent = Snapshot();
            string metricsJson = new JObject { { "_app_version", appVersion } }.ToString(Formatting.None);
            string hcJson = sent.ToJson();

            string basePayload = await requestHelper.BuildRequest();
            string payload = string.Format("{0}&metrics={1}&hc={2}", basePayload,
                UtilityHelper.EncodeDataForURL(metricsJson), UtilityHelper.EncodeDataForURL(hcJson));

            RequestResult result = await Api.Instance.SendDirectRequest(ServerUrl(), payload, "/i");
            if (result != null && result.IsSuccess()) {
                ClearSentValues(sent);
            } else {
                UtilityHelper.CountlyLogging("[ModuleHealthCheck] SendHealthCheck, send failed, retaining counters");
            }
        }
    }

    /// <summary>
    /// The health check counters at one moment, as they are sent
    /// </summary>
    internal class HealthSnapshot
    {
        internal int el;
        internal int wl;
        internal int sc;
        internal string em;
        internal int bom;
        internal int cbom;
        internal Dictionary<string, Dictionary<string, int>> fu = new Dictionary<string, Dictionary<string, int>>();
        internal Dictionary<string, int> lc = new Dictionary<string, int>();

        /// <summary>
        /// The counters in the format of the "hc" request parameter
        /// </summary>
        /// <returns>JSON text</returns>
        internal string ToJson()
        {
            JObject usage = new JObject();
            foreach (KeyValuePair<string, Dictionary<string, int>> area in fu) {
                usage[area.Key] = JObject.FromObject(area.Value);
            }
            JObject o = new JObject {
                ["el"] = el,
                ["wl"] = wl,
                ["sc"] = sc,
                ["em"] = em,
                ["bom"] = bom,
                ["cbom"] = cbom,
                ["fu"] = usage,
                ["lc"] = JObject.FromObject(lc)
            };
            return o.ToString(Formatting.None);
        }
    }

    [DataContract]
    internal class HealthCheckEntity
    {
        [DataMember]
        public string Json;
    }
}
