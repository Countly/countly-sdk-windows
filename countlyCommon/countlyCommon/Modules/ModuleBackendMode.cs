﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.CountlyCommon.Server.Responses;
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


        public ModuleBackendMode(int deviceEventLimit)
        {

            eventPool = new EventPool(deviceEventLimit, ProcessQueue);
            requestHelper = new IRequestHelperImpl(Countly.Instance);
        }

        private void RecordEventInternal(string deviceId, string appKey, string eventKey, double eventSum, int eventCount, long eventDuration, Segmentation segmentations, long timestamp)
        {
            eventPool.Put(deviceId, appKey, new CountlyEvent(eventKey, eventCount, eventSum, eventDuration, segmentations, timestamp));
        }


        private async Task ProcessQueue(string deviceId, string appKey, List<CountlyEvent> events)
        {

            UtilityHelper.CountlyLogging("[ModuleBackendMode] ProcessQueue,");

            if (events.Count > 0)
            {
                Countly.Instance.AddRequest(CreateEventRequest(deviceId, appKey, events));
            }


        }

        private string CreateEventRequest(string deviceId, string appKey, List<CountlyEvent> events)
        {
            string eventsJson = JsonConvert.SerializeObject(events, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            TimeInstant timeInstant = Countly.Instance.timeHelper.GetUniqueInstant();
            string did = UtilityHelper.EncodeDataForURL(deviceId);
            return string.Format("/i?app_key={0}&device_id={1}&events={2}&sdk_version={3}&sdk_name={4}&hour={5}&dow={6}&tz={7}&timestamp={8}&t={9}", appKey, did, UtilityHelper.EncodeDataForURL(eventsJson), requestHelper.GetSDKVersion(), requestHelper.GetSDKName(), timeInstant.Hour, timeInstant.Dow, timeInstant.Timezone, timeInstant.Timestamp, 0);
        }

        public async void RecordEvent(string deviceId, string appKey, string eventKey, double eventSum, int eventCount, long eventDuration, Segmentation segmentations, long timestamp)
        {
            RecordEventInternal(deviceId, appKey, eventKey, eventSum, eventCount, eventDuration, segmentations, timestamp);
            await Countly.Instance.Upload();
        }
    }

    public interface BackendMode
    {
        void RecordEvent(string deviceId, string appKey, string eventKey, double eventSum, int eventCount, long eventDuration, Segmentation segmentations, long timestamp);
    }

}

