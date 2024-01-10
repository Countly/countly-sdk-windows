using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using static CountlySDK.CountlyCommon.CountlyBase;
using static CountlySDK.Helpers.TimeHelper;

namespace CountlySDK.CountlyCommon
{
    internal class ModuleBackendMode : BackendMode
    {
        private readonly EventPool eventPool;
        private readonly IRequestHelperImpl requestHelper;
        private readonly CountlyBase _cly;
        internal static string[] crashMetricKeys = { "_os", "_os_version", "_ram_total", "_ram_current", "_disk_total", "_disk_current", "_online", "_muted", "_resolution", "_app_version", "_manufacture", "_device", "_orientation", "_run" };

        public ModuleBackendMode(CountlyBase countly)
        {
            _cly = countly;
            eventPool = new EventPool(_cly.Configuration.EventQueueThreshold, _cly.Configuration.BackendModeAppEQSize, _cly.Configuration.BackendModeServerEQSize, ProcessQueue);
            requestHelper = new IRequestHelperImpl(Countly.Instance);
        }

        private void RecordEventInternal(string deviceId, string appKey, string eventKey, double? eventSum = null, int eventCount = 1, long? eventDuration = null, Segmentation segmentations = null, long timestamp = 0)
        {
            if (string.IsNullOrEmpty(deviceId)) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] RecordEventInternal, deviceId cannot be null or empty, returning");
                return;
            }

            if (string.IsNullOrEmpty(appKey)) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] RecordEventInternal, appKey cannot be null or empty, returning");
                return;
            }

            if (string.IsNullOrEmpty(eventKey)) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] RecordEventInternal, eventKey cannot be null or empty, returning");
                return;
            }

            if (timestamp <= 0) {
                timestamp = _cly.timeHelper.ToUnixTime(DateTime.UtcNow);
            }

            if (eventCount < 0) {
                eventCount = 1;
            }

            lock (eventPool) {
                eventPool.Put(deviceId, appKey, new CountlyEvent(eventKey, eventCount, eventSum, eventDuration, segmentations, timestamp));
            }
        }

        private async Task ProcessQueue(string deviceId, string appKey, List<CountlyEvent> events)
        {
            UtilityHelper.CountlyLogging($"[ModuleBackendMode] ProcessQueue, deviceId:[{deviceId}] appKey:[{appKey}] eventsCount:[{events.Count}]");
            if (events.Count > 0) {
                await _cly.AddRequest(CreateEventRequest(deviceId, appKey, events));
                await _cly.Upload();
            }
        }

        internal async void OnTimer()
        {
            UtilityHelper.CountlyLogging($"[ModuleBackendMode] OnTimer");
            lock (eventPool) {
                eventPool.Dump();
            }
        }

        private string CreateEventRequest(string deviceId, string appKey, List<CountlyEvent> events)
        {
            string eventsJson = JsonConvert.SerializeObject(events, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            TimeInstant timeInstant = _cly.timeHelper.GetUniqueInstant();
            string did = UtilityHelper.EncodeDataForURL(deviceId);
            return string.Format("/i?app_key={0}&device_id={1}&events={2}&sdk_version={3}&sdk_name={4}&hour={5}&dow={6}&tz={7}&timestamp={8}&t={9}", appKey, did, UtilityHelper.EncodeDataForURL(eventsJson), requestHelper.GetSDKVersion(), requestHelper.GetSDKName(), timeInstant.Hour, timeInstant.Dow, timeInstant.Timezone, timeInstant.Timestamp, 0);
        }

        private string GetAppKey(string appKey)
        {
            string extractedAppKey = appKey;

            if (string.IsNullOrEmpty(appKey)) {
                extractedAppKey = requestHelper.GetAppKey();
            }

            return extractedAppKey;
        }

        private string GetURLEncodedJson(object obj)
        {
            return UtilityHelper.EncodeDataForURL(JsonConvert.SerializeObject(obj, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
        }

        private void RemoveInvalidDataFromDictionary(IDictionary<string, object> dict)
        {
            List<string> toRemove = new List<string>();
            foreach (KeyValuePair<string, object> item in dict) {
                object type = item.Value;

                bool isValidDataType = (type is bool || type is int || type is long || type is string || type is double || type is float);

                if (!isValidDataType) {
                    toRemove.Add(item.Key);
                    UtilityHelper.CountlyLogging("[ModuleBackendMode] RemoveInvalidDataFromDictionary, In segmentation Data type '" + type + "' of item '" + item.Value + "' isn't valid.", LogLevel.WARNING);
                }
            }

            foreach (string k in toRemove) {
                dict.Remove(k);
            }
        }

        private string CreateBaseRequest(string deviceId, string appKey, string extraParams, long timestamp = 0)
        {
            TimeInstant timeInstant;
            if (timestamp > 0) {
                timeInstant = TimeInstant.Get(timestamp);
            } else {
                timeInstant = _cly.timeHelper.GetUniqueInstant();
            }
            string did = UtilityHelper.EncodeDataForURL(deviceId);
            string app = UtilityHelper.EncodeDataForURL(appKey);
            return string.Format("/i?app_key={0}&device_id={1}&sdk_version={2}&sdk_name={3}&hour={4}&dow={5}&tz={6}&timestamp={7}&t=0&av={8}{9}", app, did, requestHelper.GetSDKVersion(), requestHelper.GetSDKName(), timeInstant.Hour, timeInstant.Dow, timeInstant.Timezone, timeInstant.Timestamp, requestHelper.GetAppVersion(), extraParams);
        }

        private string CreateQueryParamsFromDictionary(IDictionary<string, string> parameters)
        {
            string query = string.Empty;

            foreach (KeyValuePair<string, string> kvp in parameters) {
                query += string.Format("&{0}={1}", kvp.Key, UtilityHelper.EncodeDataForURL(kvp.Value));
            }

            return query;
        }

        private async void RecordExceptionInternal(string deviceId, string appKey, string error, string stackTrace, IList<string> breadcrumbs, IDictionary<string, object> customInfo, IDictionary<string, string> metrics, bool unhandled, long timestamp)
        {
            string _appKey = GetAppKey(appKey);
            IDictionary<string, object> crashData = new Dictionary<string, object> {
                { "_name", error },
                { "_nonfatal", !unhandled }
            };
            if (breadcrumbs != null && breadcrumbs.Count > 0) {
                crashData.Add("_logs", string.Join("\n", breadcrumbs.ToArray()));
            }
            if (!string.IsNullOrEmpty(stackTrace)) {
                crashData.Add("_error", stackTrace);
            }
            if (customInfo != null && customInfo.Count > 0) {
                RemoveInvalidDataFromDictionary(customInfo);
                crashData.Add("_custom", customInfo);
            }
            if (metrics != null && metrics.Count > 0) {
                foreach (KeyValuePair<string, string> kv in metrics) {
                    if (crashMetricKeys.Contains(kv.Key)) {
                        crashData.Add(kv.Key, kv.Value);
                    }
                }
            }

            await _cly.AddRequest(CreateBaseRequest(deviceId, _appKey, "&crash=" + GetURLEncodedJson(crashData), timestamp));
            await _cly.Upload();
        }

        public async void RecordEvent(string deviceId, string appKey, string eventKey, double? eventSum, int eventCount, long? eventDuration, Segmentation segmentations, long timestamp)
        {
            RecordEventInternal(deviceId, appKey, eventKey, eventSum, eventCount, eventDuration, segmentations, timestamp);
        }

        public void RecordException(string deviceId, string error, string stackTrace, IList<string> breadcrumbs, IDictionary<string, object> customInfo, IDictionary<string, string> metrics, bool unhandled, string appKey, long timestamp)
        {
            if (string.IsNullOrEmpty(error)) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] RecordException, error is empty or null, ignoring", LogLevel.WARNING);
                return;
            }

            if (string.IsNullOrEmpty(deviceId)) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] RecordException, deviceId is empty or null, ignoring", LogLevel.WARNING);
                return;
            }

            RecordExceptionInternal(deviceId, appKey, error, stackTrace, breadcrumbs, customInfo, metrics, unhandled, timestamp);

        }
    }

    public interface BackendMode
    {
        void RecordEvent(string deviceId, string appKey, string eventKey, double? eventSum = null, int eventCount = 1, long? eventDuration = null, Segmentation segmentations = null, long timestamp = 0);

        /// <summary>
        /// Record an exception
        /// </summary>
        /// <param name="deviceId">If it is empty or null, returns. required</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        /// <param name="customInfo">Custom info about exception, default null</param>
        /// <param name="error">name of the error, required</param>
        /// <param name="stackTrace">trace of the error, defaults to null</param>
        /// <param name="unhandled">bool indicates is exception is fatal or not, default is false</param>
        /// <param name="breadcrumbs">breadcrumbs if any</param>
        /// <param name="metrics">if any. otherwise null</param>
        void RecordException(string deviceId, string error, string stackTrace = null, IList<string> breadcrumbs = null, IDictionary<string, object> customInfo = null, IDictionary<string, string> metrics = null, bool unhandled = false, string appKey = null, long timestamp = 0);

    }

}

