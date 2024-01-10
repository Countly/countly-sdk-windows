using System;
using System.Collections.Generic;
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

        public async void RecordEvent(string deviceId, string appKey, string eventKey, double? eventSum, int eventCount, long? eventDuration, Segmentation segmentations, long timestamp)
        {
            RecordEventInternal(deviceId, appKey, eventKey, eventSum, eventCount, eventDuration, segmentations, timestamp);
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
    }

    public interface BackendMode
    {
        void RecordEvent(string deviceId, string appKey, string eventKey, double? eventSum = null, int eventCount = 1, long? eventDuration = null, Segmentation segmentations = null, long timestamp = 0);

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
    }

}

