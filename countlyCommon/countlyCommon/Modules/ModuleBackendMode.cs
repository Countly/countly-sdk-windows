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
    }

    public interface BackendMode
    {
        void RecordEvent(string deviceId, string appKey, string eventKey, double? eventSum = null, int eventCount = 1, long? eventDuration = null, Segmentation segmentations = null, long timestamp = 0);

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
    }

}

