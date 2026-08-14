using System;
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

        private readonly RequestHelper requestHelper;
        private readonly string ServerUrl;
        private readonly string appVersion;
        private readonly bool disabled;
        private readonly object hcLock = new object();

        // Counters since the last successful health check submission.
        private int countWarn = 0;
        private int countError = 0;
        private int statusCode = -1;
        private string errorMessage = "";
        // Backoff counters: no backoff mechanism exists yet; serialized as 0 for wire compatibility.
        private int backoff = 0;
        private int consecutiveBackoff = 0;

        private bool healthCheckSent = false;

        internal ModuleHealthCheck(RequestHelper requestHelper, string serverUrl, bool disabled, string appVersion)
        {
            this.requestHelper = requestHelper;
            ServerUrl = serverUrl;
            this.disabled = disabled;
            this.appVersion = appVersion;
            LoadState();
        }

        // ---- static hook wiring ----
        internal void RegisterHooks()
        {
            UtilityHelper.InternalLogHook = OnInternalLog;
            CountlySDK.CountlyCommon.Server.ApiBase.FailedRequestHook = LogFailedNetworkRequest;
        }

        internal void UnregisterHooks()
        {
            UtilityHelper.InternalLogHook = null;
            CountlySDK.CountlyCommon.Server.ApiBase.FailedRequestHook = null;
        }

        private void OnInternalLog(LogLevel level)
        {
            if (level == LogLevel.WARNING) { LogWarning(); } else if (level == LogLevel.ERROR) { LogError(); }
        }

        // ---- test/read accessors ----
        internal int WarningCount { get { lock (hcLock) { return countWarn; } } }
        internal int ErrorCount { get { lock (hcLock) { return countError; } } }
        internal int LastStatusCode { get { lock (hcLock) { return statusCode; } } }
        internal string LastErrorMessage { get { lock (hcLock) { return errorMessage; } } }

        // ---- counter accumulation ----
        internal void LogWarning() { lock (hcLock) { countWarn++; } }

        internal void LogError() { lock (hcLock) { countError++; } }

        internal void LogFailedNetworkRequest(int statusCodeIn, string errorResponse)
        {
            lock (hcLock) {
                if (statusCodeIn > 0 && statusCodeIn < 1000) { statusCode = statusCodeIn; }
                if (errorResponse == null) { errorResponse = ""; }
                if (errorResponse.Length > 1000) { errorResponse = errorResponse.Substring(0, 1000); }
                errorMessage = errorResponse;
            }
        }

        // ---- persistence ----
        private void LoadState()
        {
            HealthCheckEntity e = Storage.Instance.LoadFromFile<HealthCheckEntity>(healthCheckFilename).Result;
            if (e == null || string.IsNullOrEmpty(e.Json)) { return; }

            try {
                JObject o = JObject.Parse(e.Json);
                lock (hcLock) {
                    countError = o.Value<int?>("LErr") ?? 0;
                    countWarn = o.Value<int?>("LWar") ?? 0;
                    statusCode = o.Value<int?>("RStatC") ?? -1;
                    errorMessage = o.Value<string>("REMsg") ?? "";
                    backoff = o.Value<int?>("BReq") ?? 0;
                    consecutiveBackoff = o.Value<int?>("CBReq") ?? 0;
                }
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("[ModuleHealthCheck] malformed stored state, resetting: " + ex.Message, LogLevel.WARNING);
                ResetCounters();
                ClearAndSave();
            }
        }

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
            }
        }

        // ---- send ----
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

            string metricsJson = new JObject { { "_app_version", appVersion } }.ToString(Formatting.None);
            string hcJson = BuildHealthCheckJson();

            string basePayload = await requestHelper.BuildRequest();
            string payload = string.Format("{0}&metrics={1}&hc={2}", basePayload,
                UtilityHelper.EncodeDataForURL(metricsJson), UtilityHelper.EncodeDataForURL(hcJson));

            RequestResult result = await Api.Instance.SendDirectRequest(ServerUrl, payload, "/i");
            if (result != null && result.IsSuccess()) {
                ClearAndSave();
            } else {
                UtilityHelper.CountlyLogging("[ModuleHealthCheck] SendHealthCheck, send failed, retaining counters", LogLevel.WARNING);
            }
        }

        private string BuildHealthCheckJson()
        {
            JObject o = new JObject();
            lock (hcLock) {
                o["el"] = countError;
                o["wl"] = countWarn;
                o["sc"] = statusCode;
                o["em"] = errorMessage;
                o["bom"] = backoff;
                o["cbom"] = consecutiveBackoff;
            }
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
