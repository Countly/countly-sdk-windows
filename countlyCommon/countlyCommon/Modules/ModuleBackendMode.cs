using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using static CountlySDK.CountlyCommon.CountlyBase;
using static CountlySDK.Entities.EntityBase.DeviceBase;
using static CountlySDK.Helpers.TimeHelper;

namespace CountlySDK.CountlyCommon
{
    internal class ModuleBackendMode : BackendMode
    {
        private readonly EventPool eventPool;
        private readonly IRequestHelperImpl requestHelper;
        private readonly CountlyBase _cly;

        public ModuleBackendMode(CountlyBase countly)
        {
            _cly = countly;
            eventPool = new EventPool(_cly.Configuration.EventQueueThreshold, _cly.Configuration.BackendModeAppEQSize, _cly.Configuration.BackendModeServerEQSize, ProcessQueue);
            requestHelper = new IRequestHelperImpl(Countly.Instance);
        }

        private async void RecordEventInternal(string deviceId, string appKey, string eventKey, double? eventSum = null, int eventCount = 1, long? eventDuration = null, Segmentation segmentations = null, long timestamp = 0)
        {
            if (string.IsNullOrEmpty(deviceId)) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] RecordEventInternal, deviceId is empty or null, ignoring", LogLevel.WARNING);
                return;
            }

            string _appKey = GetAppKey(appKey);

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
                eventPool.Put(deviceId, _appKey, new CountlyEvent(eventKey, eventCount, eventSum, eventDuration, segmentations, timestamp));
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

        private async void ChangeDeviceIdWithMergeInternal(string newDeviceId, string appKey, long timestamp, string oldDeviceId)
        {
            await _cly.DeviceData.SetPreferredDeviceIdMethod(DeviceIdMethodInternal.developerSupplied, newDeviceId);

            string _appKey = GetAppKey(appKey);

            await _cly.AddRequest(CreateBaseRequest(newDeviceId, _appKey, "&old_device_id=" + UtilityHelper.EncodeDataForURL(oldDeviceId), timestamp));
            await _cly.Upload();
        }

        public async void RecordDirectRequestInternal(IDictionary<string, string> paramaters, string deviceId, string appKey = null, long timestamp = 0)
        {
            string _appKey = GetAppKey(appKey);
            await _cly.AddRequest(CreateBaseRequest(deviceId, _appKey, CreateQueryParamsFromDictionary(paramaters) + "&dr=1", timestamp));
            await _cly.Upload();
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

        private async void BeginSessionInternal(string deviceId, string appKey = null, IDictionary<string, string> metrics = null, IDictionary<string, string> location = null, long timestamp = 0)
        {
            string _appKey = GetAppKey(appKey);
            string beginSessionParams = "&begin_session=1&metrics=";
            if (metrics == null) {
                beginSessionParams += GetURLEncodedJson(_cly.GetSessionMetrics().MetricsDict);
            } else {
                beginSessionParams += GetURLEncodedJson(metrics);
            }
            if (location != null && location.Count > 0) {
                beginSessionParams += CreateQueryParamsFromDictionary(location);
            }

            await _cly.AddRequest(CreateSessionRequest(deviceId, _appKey, paramOverload: beginSessionParams, timestamp: timestamp));
            await _cly.Upload();
        }

        private async void EndSessionInternal(string deviceId, string appKey, int duration, long timestamp = 0)
        {
            string _appKey = GetAppKey(appKey);
            await _cly.AddRequest(CreateSessionRequest(deviceId, _appKey, duration, "&end_session=1", timestamp));
            await _cly.Upload();
        }

        private async void UpdateSessionInternal(string deviceId, string appKey = null, int duration = 0, long timestamp = 0)
        {
            string _appKey = GetAppKey(appKey);
            await _cly.AddRequest(CreateSessionRequest(deviceId, _appKey, duration, timestamp: timestamp));
            await _cly.Upload();
        }

        public async void RecordEvent(string deviceId, string appKey, string eventKey, double? eventSum, int eventCount, long? eventDuration, Segmentation segmentations, long timestamp)
        {
            RecordEventInternal(deviceId, appKey, eventKey, eventSum, eventCount, eventDuration, segmentations, timestamp);
        }

        public void ChangeDeviceIdWithMerge(string newDeviceId, string oldDeviceId, string appKey = null, long timestamp = 0)
        {
            if (string.IsNullOrEmpty(newDeviceId)) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] ChangeDeviceIdWithMerge, new device id is empty or null, ignoring", LogLevel.WARNING);
                return;

            }

            if (string.IsNullOrEmpty(oldDeviceId)) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] ChangeDeviceIdWithMerge, old device id is empty or null, ignoring", LogLevel.WARNING);
                return;

            }

            if (newDeviceId == oldDeviceId) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] ChangeDeviceIdWithMerge, new device id is equal to the old one, ignoring", LogLevel.WARNING);
                return;
            }

            ChangeDeviceIdWithMergeInternal(newDeviceId, appKey, timestamp, oldDeviceId);
        }

        public void RecordDirectRequest(string deviceId, IDictionary<string, string> paramaters, string appKey, long timestamp)
        {
            if (paramaters == null || paramaters.Count < 1) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] RecordDirectRequest, parameters are empty or null, ignoring", LogLevel.WARNING);
                return;
            }

            if (string.IsNullOrEmpty(deviceId)) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] RecordDirectRequest, deviceId is empty or null, ignoring", LogLevel.WARNING);
                return;
            }

            RecordDirectRequestInternal(paramaters, deviceId, appKey, timestamp);
        }

        private void RecordView(string deviceId, string appKey, string name, string segment, long? dur, Segmentation segmentations, long timestamp)
        {

            if (string.IsNullOrEmpty(name)) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] RecordView, name is empty or null returning", LogLevel.WARNING);
                return;
            }

            if (string.IsNullOrEmpty(segment)) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] RecordView, segment is empty or null, defaulting to Windows", LogLevel.WARNING);
                segment = "Windows";
            }

            segmentations.Add("segment", segment);
            segmentations.Add("name", name);
            RecordEventInternal(deviceId, appKey, "[CLY]_view", null, 1, dur, segmentations, timestamp);
        }

        public void StartView(string deviceId, string name, Segmentation segmentations, string segment, string appKey, bool firstView, long timestamp)
        {
            if (segmentations == null) {
                segmentations = new Segmentation();
            }
            if (firstView) {
                segmentations.Add("start", "1");
            }

            segmentations.Add("visit", "1");

            RecordView(deviceId, appKey, name, segment, null, segmentations, timestamp);

        }

        public void StopView(string deviceId, string name, long duration, Segmentation segmentations, string segment, string appKey, long timestamp)
        {
            if (duration < 0) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] StopView, dur should not be negative, returning", LogLevel.WARNING);
                return;
            }

            if (segmentations == null) {
                segmentations = new Segmentation();
            }

            RecordView(deviceId, appKey, name, segment, duration, segmentations, timestamp);
        }

        public void RecordEvent(string deviceId, string eventKey, Segmentation segmentations, int count, double? sum, long? duration, string appKey, long timestamp)
        {
            RecordEventInternal(deviceId, appKey, eventKey, sum, count, duration, segmentations, timestamp);
        }
        
        public void BeginSession(string deviceId, string appKey, IDictionary<string, string> metrics, IDictionary<string, string> location, long timestamp)
        {
            BeginSessionInternal(deviceId, appKey, metrics, location, timestamp);
        }

        public void UpdateSession(string deviceId, int duration, string appKey = null, long timestamp = 0)
        {
            if (duration < 1) {
                UtilityHelper.CountlyLogging("[ModuleBackendMode] UpdateSession, duration could not be negative, returning");
                return;
            }
            UpdateSessionInternal(deviceId, appKey, duration, timestamp);
        }

        public void EndSession(string deviceId = null, int duration = -1, string appKey = null, long timestamp = 0)
        {
            EndSessionInternal(deviceId, appKey, duration, timestamp);
        }
    }

    public interface BackendMode
    {
        /// <summary>
        /// Record event with multiple app and device support
        /// </summary>
        /// <param name="deviceId">If it is empty or null, returns. required</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        /// <param name="eventKey">Event key, required</param>
        /// <param name="eventSum">Defaults to null</param>
        /// <param name="eventCount">Defaults to 1</param>
        /// <param name="eventDuration">Defaults to null</param>
        /// <param name="segmentations">Defaults to null</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        void RecordEvent(string deviceId, string appKey = null, string eventKey = null, double? eventSum = null, int eventCount = 1, long? eventDuration = null, Segmentation segmentations = null, long timestamp = 0);

        /// <summary>
        /// Change device id with server merge
        /// </summary>
        /// <param name="newDeviceId">The id that will going to be merged with the provided device id, should not be null or empty</param>
        /// <param name="oldDeviceId">If it is empty or null, returns. required</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        void ChangeDeviceIdWithMerge(string newDeviceId, string oldDeviceId, string appKey = null, long timestamp = 0);

        /// <summary>
        /// Send direct request to the server
        /// </summary>
        /// <param name="paramaters">Should not be null or empty, otherwise ignored</param>
        /// <param name="deviceId">If it is empty or null, returns. required</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        void RecordDirectRequest(string deviceId, IDictionary<string, string> paramaters, string appKey = null, long timestamp = 0);

        /// <summary>
        /// Start view with multiple app and device support
        /// 
        /// <param name="name">View name, required</param>
        /// <param name="segment">Platform of the device or domain, required</param>
        /// <param name="deviceId">If it is empty or null, returns. required</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        /// <param name="firstView">To indicate wheter or not view is the first view of the session or flow, default false</param>
        /// <param name="segmentations">Defaults to null</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        void StartView(string deviceId, string name, Segmentation segmentations = null, string segment = null, string appKey = null, bool firstView = false, long timestamp = 0);

        /// <summary>
        /// Stop view with multiple app and device support
        /// 
        /// </summary>
        /// <param name="name">View name, required</param>
        /// <param name="segment">Platform of the device or domain, required</param>
        /// <param name="duration">Duration of the view, required</param>
        /// <param name="deviceId">If it is empty or null, returns. required</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        /// <param name="segmentations">Defaults to null</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        void StopView(string deviceId, string name, long duration, Segmentation segmentations = null, string segment = null, string appKey = null, long timestamp = 0);

        /// <summary>
        /// Record event with multiple app and device support
        /// </summary>
        /// <param name="deviceId">If it is empty or null, returns. required</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        /// <param name="eventKey">Event key, required</param>
        /// <param name="sum">Defaults to null</param>
        /// <param name="count">Defaults to 1</param>
        /// <param name="duration">Defaults to null</param>
        /// <param name="segmentations">Defaults to null</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        void RecordEvent(string deviceId, string eventKey, Segmentation segmentations = null, int count = 1, double? sum = null, long? duration = null, string appKey = null, long timestamp = 0);

        // <summary>
        /// Begin session with multiple apps and devices
        /// </summary>
        /// <param name="deviceId">If it is empty or null, returns. required</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        /// <param name="metrics">If it is not provided, internal metrics will be used</param>
        /// <param name="location">If not given, will not be added</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        void BeginSession(string deviceId, string appKey = null, IDictionary<string, string> metrics = null, IDictionary<string, string> location = null, long timestamp = 0);

        /// <summary>
        /// Update session with multiple apps and devices
        /// </summary>
        /// <param name="duration">Session duration in seconds, required</param>
        /// <param name="deviceId">If it is empty or null, returns. required</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        void UpdateSession(string deviceId, int duration, string appKey = null, long timestamp = 0);

        /// <summary>
        /// End session with multiple apps and devices
        /// </summary>
        /// <param name="duration">Session duration in seconds,required</param>
        /// <param name="deviceId">If it is empty or null, returns. required</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        /// <param name="timestamp">Defaults to current timestamp if not provided</param>
        void EndSession(string deviceId, int duration, string appKey = null, long timestamp = 0);
    }

}

