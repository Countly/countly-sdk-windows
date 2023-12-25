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

        private async void BeginSessionInternal(string deviceId = null, string appKey = null)
        {
            Tuple<string, string> deviceIdAppKey = await GetDeviceIdAppKey(deviceId, appKey);
            await _cly.AddRequest(CreateSessionRequest(deviceIdAppKey.Item1, deviceIdAppKey.Item2, paramOverload: "&begin_session=1"));
            await _cly.Upload();
        }

        private async void EndSessionInternal(string deviceId = null, string appKey = null, int duration = 0)
        {
            Tuple<string, string> deviceIdAppKey = await GetDeviceIdAppKey(deviceId, appKey);
            await _cly.AddRequest(CreateSessionRequest(deviceIdAppKey.Item1, deviceIdAppKey.Item2, duration, "&end_session=1"));
            await _cly.Upload();
        }

        private async void UpdateSessionInternal(string deviceId = null, string appKey = null, int duration = 0)
        {
            Tuple<string, string> deviceIdAppKey = await GetDeviceIdAppKey(deviceId, appKey);
            await _cly.AddRequest(CreateSessionRequest(deviceIdAppKey.Item1, deviceIdAppKey.Item2, duration));
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

        internal async void OnTimer()
        {
            UtilityHelper.CountlyLogging($"[ModuleBackendMode] OnTimer");
            lock (eventPool) {
                eventPool.Dump();
            }
        }

        private string CreateEventRequest(string deviceId, string appKey, List<CountlyEvent> events)
        {
            //&events={2}
            string eventsJson = JsonConvert.SerializeObject(events, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            return CreateBaseRequest(deviceId, appKey, string.Format("&events={0}", UtilityHelper.EncodeDataForURL(eventsJson)));
        }

        private string CreateSessionRequest(string deviceId, string appKey, int duration = -1, string paramOverload = "")
        {
            if (duration >= 0) {
                paramOverload += "&session_duration=" + duration;
            }
            return CreateBaseRequest(deviceId, appKey, paramOverload);
        }

        private string CreateBaseRequest(string deviceId, string appKey, string extraParams)
        {
            TimeInstant timeInstant = _cly.timeHelper.GetUniqueInstant();
            string did = UtilityHelper.EncodeDataForURL(deviceId);
            string app = UtilityHelper.EncodeDataForURL(appKey);
            return string.Format("/i?app_key={0}&device_id={1}&sdk_version={2}&sdk_name={3}&hour={4}&dow={5}&tz={6}&timestamp={7}&t=0{8}", app, did, requestHelper.GetSDKVersion(), requestHelper.GetSDKName(), timeInstant.Hour, timeInstant.Dow, timeInstant.Timezone, timeInstant.Timestamp, extraParams);
        }

        public void BeginSession(string deviceId = null, string appKey = null)
        {
            BeginSessionInternal(deviceId, appKey);
        }


        public void UpdateSession(int duration, string deviceId = null, string appKey = null)
        {
            UpdateSessionInternal(deviceId, appKey, duration);
        }

        public void EndSession(int duration = 0, string deviceId = null, string appKey = null)
        {
            EndSessionInternal(deviceId, appKey, duration);
        }

        public async void RecordEvent(string deviceId, string appKey, string eventKey, double? eventSum, int eventCount, long? eventDuration, Segmentation segmentations, long timestamp)
        {
            RecordEventInternal(deviceId, appKey, eventKey, eventSum, eventCount, eventDuration, segmentations, timestamp);
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
        /// <param name="timestamp">Defaults to current timestamp</param>
        void RecordEvent(string deviceId, string appKey, string eventKey, double? eventSum = null, int eventCount = 1, long? eventDuration = null, Segmentation segmentations = null, long timestamp = 0);

        /// <summary>
        /// Begin session with multiple apps and devices
        /// </summary>
        /// <param name="deviceId">If it is empty or null, defaults to device id given or generated internal</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        void BeginSession(string deviceId = null, string appKey = null);

        /// <summary>
        /// Update session with multiple apps and devices
        /// </summary>
        /// <param name="duration">Session duration in seconds, required</param>
        /// <param name="deviceId">If it is empty or null, defaults to device id given or generated internal</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        void UpdateSession(int duration, string deviceId = null, string appKey = null);

        /// <summary>
        /// End session with multiple apps and devices
        /// </summary>
        /// <param name="duration">Session duration in seconds, default is 0 seconds</param>
        /// <param name="deviceId">If it is empty or null, defaults to device id given or generated internal</param>
        /// <param name="appKey">If it is empty or null, defaults to app key given in the config</param>
        void EndSession(int duration = 0, string deviceId = null, string appKey = null);
    }

}

