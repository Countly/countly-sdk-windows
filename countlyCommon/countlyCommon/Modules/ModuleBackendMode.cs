using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static CountlySDK.CountlyCommon.CountlyBase;
using static CountlySDK.Helpers.TimeHelper;

namespace CountlySDK.CountlyCommon
{
    internal class ModuleBackendMode : BackendMode
    {
        private readonly EventPool eventPool;
        private readonly IRequestHelperImpl requestHelper;
        private readonly CountlyBase _cly;
        internal static string[] crashMetricKeys = new string[] { "_os", "_os_version", "_ram_total", "_ram_current", "_disk_total", "_disk_current", "_online", "_muted", "_resolution", "_app_version", "_manufacture", "_device", "_orientation", "_run" };

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

        private async void BeginSessionInternal(string deviceId = null, string appKey = null, IDictionary<string, string> metrics = null, IDictionary<string, string> location = null, long timestamp = 0)
        {
            Tuple<string, string> deviceIdAppKey = await GetDeviceIdAppKey(deviceId, appKey);
            string beginSessionParams = "&begin_session=1&metrics=";
            if (metrics == null) {
                beginSessionParams += GetURLEncodedJson(_cly.GetSessionMetrics());
            } else {
                beginSessionParams += GetURLEncodedJson(metrics);
            }
            if (location != null && location.Count > 0) {
                beginSessionParams += CreateQueryParamsFromDictionary(location);
            }

            await _cly.AddRequest(CreateSessionRequest(deviceIdAppKey.Item1, deviceIdAppKey.Item2, paramOverload: beginSessionParams, timestamp: timestamp));
            await _cly.Upload();
        }

        private async void EndSessionInternal(string deviceId = null, string appKey = null, int duration = 0, long timestamp = 0)
        {
            Tuple<string, string> deviceIdAppKey = await GetDeviceIdAppKey(deviceId, appKey);
            await _cly.AddRequest(CreateSessionRequest(deviceIdAppKey.Item1, deviceIdAppKey.Item2, duration, "&end_session=1", timestamp));
            await _cly.Upload();
        }

        private async void UpdateSessionInternal(string deviceId = null, string appKey = null, int duration = 0, long timestamp = 0)
        {
            Tuple<string, string> deviceIdAppKey = await GetDeviceIdAppKey(deviceId, appKey);
            await _cly.AddRequest(CreateSessionRequest(deviceIdAppKey.Item1, deviceIdAppKey.Item2, duration, timestamp: timestamp));
            await _cly.Upload();
        }

        public async void RecordDirectRequestInternal(IDictionary<string, string> paramaters, string deviceId = null, string appKey = null, long timestamp = 0)
        {
            Tuple<string, string> deviceIdAppKey = await GetDeviceIdAppKey(deviceId, appKey);
            await _cly.AddRequest(CreateBaseRequest(deviceIdAppKey.Item1, deviceIdAppKey.Item2, CreateQueryParamsFromDictionary(paramaters) + "&dr=1", timestamp));
            await _cly.Upload();
        }

        private async void RecordExceptionInternal(string deviceId, string appKey, string error, string stackTrace, IList<string> breadcrumbs, IDictionary<string, string> customInfo, IDictionary<string, string> metrics, bool unhandled, long timestamp)
        {
            Tuple<string, string> deviceIdAppKey = await GetDeviceIdAppKey(deviceId, appKey);
            IDictionary<string, object> crashData = new Dictionary<string, object> {
                { "_name", error },
                { "_nonfatal", !unhandled }
            };
            if (breadcrumbs != null && breadcrumbs.Count > 0) {
                crashData.Add("_logs", string.Join("\n", breadcrumbs));
            }
            if (!string.IsNullOrEmpty(stackTrace)) {
                crashData.Add("_error", stackTrace);
            }
            if (customInfo != null && customInfo.Count > 0) {
                crashData.Add("_custom", customInfo);
            }
            if (metrics != null && metrics.Count > 0) {
                foreach (KeyValuePair<string, string> kv in metrics) {
                    if (crashMetricKeys.Contains(kv.Key)) {
                        crashData.Add(kv.Key, kv.Value);
                    }
                }
            }

            await _cly.AddRequest(CreateBaseRequest(deviceIdAppKey.Item1, deviceIdAppKey.Item2, "&crash=" + GetURLEncodedJson(crashData), timestamp));
            await _cly.Upload();
        }

        private async Task<Tuple<string, string>> GetDeviceIdAppKey(string deviceId, string appKey)
        {
            string extractedDeviceID = deviceId;
            string extractedAppKey = appKey;
            if (string.IsNullOrEmpty(deviceId)) {
                extractedDeviceID = (await _cly.DeviceData.GetDeviceId()).deviceId;
            }

            if (string.IsNullOrEmpty(appKey)) {
                extractedAppKey = requestHelper.GetAppKey();
            }

            return new Tuple<string, string>(extractedDeviceID, extractedAppKey);
        }

        private async Task ProcessQueue(string deviceId, string appKey, List<CountlyEvent> events)
        {
            UtilityHelper.CountlyLogging($"[ModuleBackendMode] ProcessQueue, deviceId:[{deviceId}] appKey:[{appKey}] eventsCount:[{events.Count}]");
            if (events.Count > 0) {
                await _cly.AddRequest(CreateEventRequest(deviceId, appKey, events));
                await _cly.Upload();
            }
        }

        private string GetURLEncodedJson(object obj)
        {
            return UtilityHelper.EncodeDataForURL(JsonConvert.SerializeObject(obj, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
        }

        internal async void OnTimer()
        {
            UtilityHelper.CountlyLogging($"[ModuleBackendMode] OnTimer");
            lock (eventPool) {
                eventPool.Dump();
            }
        }

        private string CreateQueryParamsFromDictionary(IDictionary<string, string> parameters)
        {
            string query = string.Empty;

            foreach (KeyValuePair<string, string> kvp in parameters) {
                query += string.Format("&{0}={1}", kvp.Key, UtilityHelper.EncodeDataForURL(kvp.Value));
            }

            return query;
        }

        private string CreateEventRequest(string deviceId, string appKey, List<CountlyEvent> events)
        {
            string eventsJson = JsonConvert.SerializeObject(events, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            return CreateBaseRequest(deviceId, appKey, string.Format("&events={0}", UtilityHelper.EncodeDataForURL(eventsJson)));
        }

        private string CreateSessionRequest(string deviceId, string appKey, int duration = -1, string paramOverload = "", long timestamp = 0)
        {
            if (duration >= 0) {
                paramOverload += "&session_duration=" + duration;
            }
            return CreateBaseRequest(deviceId, appKey, paramOverload, timestamp);
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
            return string.Format("/i?app_key={0}&device_id={1}&sdk_version={2}&sdk_name={3}&hour={4}&dow={5}&tz={6}&timestamp={7}&t=0{8}", app, did, requestHelper.GetSDKVersion(), requestHelper.GetSDKName(), timeInstant.Hour, timeInstant.Dow, timeInstant.Timezone, timeInstant.Timestamp, extraParams);
        }

        public void BeginSession(string deviceId, string appKey, IDictionary<string, string> metrics, IDictionary<string, string> location, long timestamp)
        {
            BeginSessionInternal(deviceId, appKey, metrics, location, timestamp);
        }


        public void UpdateSession(int duration, string deviceId = null, string appKey = null, long timestamp = 0)
        {
            UpdateSessionInternal(deviceId, appKey, duration, timestamp);
        }

        public void EndSession(int duration = 0, string deviceId = null, string appKey = null, long timestamp = 0)
        {
            EndSessionInternal(deviceId, appKey, duration, timestamp);
        }

        public async void RecordEvent(string deviceId, string appKey, string eventKey, double? eventSum, int eventCount, long? eventDuration, Segmentation segmentations, long timestamp)
        {
            RecordEventInternal(deviceId, appKey, eventKey, eventSum, eventCount, eventDuration, segmentations, timestamp);
        }

        public void RecordView(string deviceId, string appKey, string name, Segmentation segmentations, long timestamp)
        {

            if (string.IsNullOrEmpty(name)) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] RecordView, name is empty or null returning", LogLevel.WARNING);
                return;
            }

            if (segmentations == null) {
                segmentations = new Segmentation();
            }

            segmentations.Add("name", name);
            RecordEventInternal(deviceId, appKey, "[CLY]_view", null, 1, null, segmentations, timestamp);
        }

        public void RecordDirectRequest(IDictionary<string, string> paramaters, string deviceId, string appKey, long timestamp)
        {
            if (paramaters == null || paramaters.Count < 1) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] RecordDirectRequest, parameters are empty or null, ignoring");
                return;
            }
            RecordDirectRequestInternal(paramaters, deviceId, appKey, timestamp);
        }

        public void RecordException(string deviceId, string appKey, string error, string stackTrace, IList<string> breadcrumbs, IDictionary<string, string> customInfo, IDictionary<string, string> metrics, bool unhandled, long timestamp)
        {
            if (string.IsNullOrEmpty(error)) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] RecordException, error is empty or null, ignoring");
                return;
            }

            RecordExceptionInternal(deviceId, appKey, error, stackTrace, breadcrumbs, customInfo, metrics, unhandled, timestamp);

        }
    }

    public interface BackendMode
    {
        /// <summary>
        /// Record event with multiple app and device support
        /// </summary>
        /// <param name="deviceId">Device Id, required</param>
        /// <param name="appKey">App Key, required</param>
        /// <param name="eventKey">Event key, required</param>
        /// <param name="eventSum">Defaults to null</param>
        /// <param name="eventCount">Defaults to 1</param>
        /// <param name="eventDuration">Defaults to null</param>
        /// <param name="segmentations">Defaults to null</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        void RecordEvent(string deviceId, string appKey, string eventKey, double? eventSum = null, int eventCount = 1, long? eventDuration = null, Segmentation segmentations = null, long timestamp = 0);

        /// <summary>
        /// Record event with multiple app and device support
        /// </summary>
        /// <param name="deviceId">Device Id, required</param>
        /// <param name="appKey">App Key, required</param>
        /// <param name="name">View name, required</param>
        /// <param name="segmentations">Defaults to null</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        void RecordView(string deviceId, string appKey, string name, Segmentation segmentations = null, long timestamp = 0);


        /// <summary>
        /// Begin session with multiple apps and devices
        /// </summary>
        /// <param name="deviceId">If it is empty or null, defaults to device id given or generated internal</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        /// <param name="metrics">If it is not provided, internal metrics will be used</param>
        /// <param name="location">If not given, will not be added</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        void BeginSession(string deviceId = null, string appKey = null, IDictionary<string, string> metrics = null, IDictionary<string, string> location = null, long timestamp = 0);

        /// <summary>
        /// Update session with multiple apps and devices
        /// </summary>
        /// <param name="duration">Session duration in seconds, required</param>
        /// <param name="deviceId">If it is empty or null, defaults to device id given or generated internal</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        void UpdateSession(int duration, string deviceId = null, string appKey = null, long timestamp = 0);

        /// <summary>
        /// End session with multiple apps and devices
        /// </summary>
        /// <param name="duration">Session duration in seconds, default is 0 seconds</param>
        /// <param name="deviceId">If it is empty or null, defaults to device id given or generated internal</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        void EndSession(int duration = 0, string deviceId = null, string appKey = null, long timestamp = 0);

        /// <summary>
        /// Send direct request to the server
        /// </summary>
        /// <param name="paramaters">Should not be null or empty, otherwise ignored</param>
        /// <param name="deviceId">If it is empty or null, defaults to device id given or generated internal</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        void RecordDirectRequest(IDictionary<string, string> paramaters, string deviceId = null, string appKey = null, long timestamp = 0);

        /// <summary>
        /// Record an exception
        /// </summary>
        /// <param name="deviceId">If it is empty or null, defaults to device id given or generated internal</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        /// <param name="customInfo">Custom info about exception, default null</param>
        /// <param name="error">name of the error, required</param>
        /// <param name="stackTrace">trace of the error, defaults to null</param>
        /// <param name="unhandled">bool indicates is exception is fatal or not, default is false</param>
        /// <param name="breadcrumbs">breadcrumbs if any</param>
        /// <param name="metrics">if any. otherwise null</param>
        void RecordException(string deviceId = null, string appKey = null, string error = null, string stackTrace = null, IList<string> breadcrumbs = null, IDictionary<string, string> customInfo = null, IDictionary<string, string> metrics = null, bool unhandled = false, long timestamp = 0);
    }

}

