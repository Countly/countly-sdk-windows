﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using CountlySDK;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace TestProject_common
{
    public class ModuleBackendModeTests : IDisposable
    {
        /// <summary>
        /// Test setup
        /// </summary>
        public ModuleBackendModeTests()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Halt();
            TestHelper.CleanDataFiles();
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {

        }

        [Fact]
        /// <summary>
        /// It validates that if backend mode disabled call to interface functions return null
        /// </summary>
        public async void RecordEvent_BackendModeNotEnabled()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            Countly.Instance.Init(cc).Wait();

            Assert.Equal(null, Countly.Instance.BackendMode());
        }


        [Fact]
        /// <summary>
        /// Validate that every call to "RecordEvent" function of the BackendMode do not create a request in the queue
        /// All event queue sizes are given as 1 to trigger request creation.
        /// After each call validating that request queue size is 0
        /// </summary>
        public async void RecordEvent_NullOrEmpty_DeviceID()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();
            // made all queues 1 to look to the queue to detect eq changes
            cc.SetEventQueueSizeToSend(1).SetBackendModeAppEQSizeToSend(1).SetBackendModeServerEQSizeToSend(1);

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.BackendMode().RecordEvent("", "APP_KEY", TestHelper.v[0]);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().RecordEvent(null, "APP_KEY", TestHelper.v[1]);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
        }

        [Fact]
        /// <summary>
        /// Validate that every call to "RecordEvent" function of the BackendMode do not create a request in the queue
        /// All event queue sizes are given as 1 to trigger request creation.
        /// After each call validating that request queue size is 0
        /// </summary>
        public async void RecordEvent_NullOrEmpty_EventKey()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();
            // made all queues 1 to look to the queue to detect eq changes
            cc.SetEventQueueSizeToSend(1).SetBackendModeAppEQSizeToSend(1).SetBackendModeServerEQSizeToSend(1);

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.BackendMode().RecordEvent("DEVICE_ID", "APP_KEY", "");
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().RecordEvent("DEVICE_ID", "APP_KEY", eventKey: null);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
        }

        [Fact]
        /// <summary>
        /// Validate that every call to "RecordEvent" function of the BackendMode create a request in the queue
        /// All event queue sizes are given as 1 to trigger request creation.
        /// After each call validating that request queue size increases and app key defaults to init given
        /// </summary>
        public async void RecordEvent_NullOrEmpty_AppKey()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();
            // made all queues 1 to look to the queue to detect eq changes
            cc.SetEventQueueSizeToSend(1).SetBackendModeAppEQSizeToSend(1).SetBackendModeServerEQSizeToSend(1);

            Countly.Instance.BackendMode().RecordEvent("DEVICE_ID", "", TestHelper.v[2]);
            ValidateEventInRequestQueue(TestHelper.v[2], "DEVICE_ID", TestHelper.APP_KEY);
            Countly.Instance.BackendMode().RecordEvent("DEVICE_ID", null, TestHelper.v[3]);
            ValidateEventInRequestQueue(TestHelper.v[3], "DEVICE_ID", TestHelper.APP_KEY, reqCount: 2, rqIdx: 1);
        }

        [Fact]
        /// <summary>
        /// Validate that "RecordEvent" function of the BackendMode does create positive timestamp event even if it is given as negative
        /// All event queue sizes are given as 1 to trigger request creation.
        /// Validating that an events request is generated with positive timestamp
        /// </summary>
        public async void RecordEvent_NegativeTimestamp()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();
            // made all queues 1 to look to the queue to detect eq changes
            cc.SetEventQueueSizeToSend(1).SetBackendModeAppEQSizeToSend(1).SetBackendModeServerEQSizeToSend(1);

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.BackendMode().RecordEvent("DEVICE_ID", "APP_KEY", TestHelper.v[0], timestamp: -1);
            ValidateEventInRequestQueue(TestHelper.v[0], "DEVICE_ID", "APP_KEY");

        }

        [Fact]
        /// <summary>
        /// Validate that "RecordEvent" function of the BackendMode does create positive count event even if it is given as negative
        /// All event queue sizes are given as 1 to trigger request creation.
        /// Validating that an events request is generated with positive count
        /// </summary>
        public async void RecordEvent_NegativeCount()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();
            // made all queues 1 to look to the queue to detect eq changes
            cc.SetEventQueueSizeToSend(1).SetBackendModeAppEQSizeToSend(1).SetBackendModeServerEQSizeToSend(1);

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.BackendMode().RecordEvent("DEVICE_ID", "APP_KEY", TestHelper.v[0], eventCount: -1);
            ValidateEventInRequestQueue(TestHelper.v[0], "DEVICE_ID", "APP_KEY");

        }

        [Fact]
        /// <summary>
        /// Validate that "RecordEvent" function of the BackendMode with bunch of valid parameters
        /// All event queue sizes are given as 1 to trigger request creation.
        /// Validating that an events request is generated and same with expected values
        /// </summary>
        public async void RecordEvent()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();
            // made all queues 1 to look to the queue to detect eq changes
            cc.SetEventQueueSizeToSend(1).SetBackendModeAppEQSizeToSend(1).SetBackendModeServerEQSizeToSend(1);

            Countly.Instance.Init(cc).Wait();

            Segmentation segmentation = new Segmentation();
            segmentation.Add(TestHelper.v[0], "true");
            segmentation.Add(TestHelper.v[1], "683");
            segmentation.Add(TestHelper.v[2], "68.99");

            Countly.Instance.BackendMode().RecordEvent("DEVICE_ID", "APP_KEY", TestHelper.v[3], 0.56, 6, 65, segmentation);
            ValidateEventInRequestQueue(TestHelper.v[3], "DEVICE_ID", "APP_KEY", 6, 0.56, segmentation);

        }

        [Fact]
        /// <summary>
        /// Validate that "RecordEvent" function of the BackendMode with bunch of valid parameters
        /// Event queue size is given as 2 to check that if size is exceeded request is generated
        /// Validating that an events request is generated 2 events exist for device id and app key and other device id is not generates request
        /// </summary>
        public async void RecordEvent_EQSize()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();
            // made all queues 1 to look to the queue to detect eq changes
            cc.SetEventQueueSizeToSend(2);

            Countly.Instance.Init(cc).Wait();

            Segmentation segmentation = new Segmentation();
            segmentation.Add(TestHelper.v[0], "true");
            segmentation.Add(TestHelper.v[1], "683");
            segmentation.Add(TestHelper.v[2], "68.99");

            Countly.Instance.BackendMode().RecordEvent(TestHelper.v[6], TestHelper.v[7], TestHelper.v[3], 0.56, 6, 65, segmentation);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().RecordEvent(TestHelper.v[8], TestHelper.v[7], TestHelper.v[4]);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().RecordEvent(TestHelper.v[6], TestHelper.v[7], TestHelper.v[5], 0.72, 3);

            ValidateEventInRequestQueue(TestHelper.v[3], TestHelper.v[6], TestHelper.v[7], 6, 0.56, segmentation, eventQCount: 2);
            ValidateEventInRequestQueue(TestHelper.v[5], TestHelper.v[6], TestHelper.v[7], 3, 0.72, eventIdx: 1, eventQCount: 2);
        }

        [Fact]
        /// <summary>
        /// Validate that "RecordEvent" function of the BackendMode with bunch of valid parameters
        /// App Event queue size is given as 2 to check that if size is exceeded events requests are generated for that app
        /// Validating that events request is generated, and none request is generated for not exceeded app keys
        /// </summary>
        public async void RecordEvent_AppEQSize()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();
            // made all queues 1 to look to the queue to detect eq changes
            cc.SetBackendModeAppEQSizeToSend(2);

            Countly.Instance.Init(cc).Wait();


            Countly.Instance.BackendMode().RecordEvent(TestHelper.v[0], TestHelper.v[1], TestHelper.v[3], 0.56, 6);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().RecordEvent(TestHelper.v[2], TestHelper.v[6], TestHelper.v[7]);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().RecordEvent(TestHelper.v[4], TestHelper.v[1], TestHelper.v[5]);

            ValidateEventInRequestQueue(TestHelper.v[3], TestHelper.v[0], TestHelper.v[1], 6, 0.56, reqCount: 2);
            ValidateEventInRequestQueue(TestHelper.v[5], TestHelper.v[4], TestHelper.v[1], rqIdx: 1, reqCount: 2);
        }

        [Fact]
        /// <summary>
        /// Validate that "RecordEvent" function of the BackendMode with bunch of valid parameters
        /// Server Event queue size is given as 2 to check that if size is exceeded events requests are generated for whole apps and devices
        /// Validating that events requests are generated for the whole apps and devices, after flushed next recorded event should not be recorded
        /// </summary>
        public void RecordEvent_ServerEQSize()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();
            // made all queues 1 to look to the queue to detect eq changes
            cc.SetBackendModeServerEQSizeToSend(2);

            Countly.Instance.Init(cc).Wait();


            Countly.Instance.BackendMode().RecordEvent(TestHelper.v[0], TestHelper.v[1], TestHelper.v[3], 0.56, 6);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().RecordEvent(TestHelper.v[2], TestHelper.v[6], TestHelper.v[7]);
            Assert.True(Countly.Instance.StoredRequests.Count == 2);
            Countly.Instance.BackendMode().RecordEvent(TestHelper.v[4], TestHelper.v[8], TestHelper.v[5]);
            Assert.True(Countly.Instance.StoredRequests.Count == 2);

            ValidateEventInRequestQueue(TestHelper.v[3], TestHelper.v[0], TestHelper.v[1], 6, 0.56, reqCount: 2);
            ValidateEventInRequestQueue(TestHelper.v[7], TestHelper.v[2], TestHelper.v[6], rqIdx: 1, reqCount: 2);
        }

        [Fact]
        /// <summary>
        /// "BeginSession" with different device id and app keys
        /// Validate that an begin session request is generated after each call and expected behaviour should happen
        /// 
        /// 1. If device id is given but app key not given, app key should fallback to init given app key,
        /// 2. If both of them are given values should be match
        /// 3. If app key is given as empty string it fallbacks to default one
        /// 
        /// RQ size must increase by 1 after each call, and expected values should match
        /// </summary>
        public void BeginSession_AppKeyFallback()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().BeginSession(deviceId: TestHelper.v[0]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("begin_session", "1", "metrics", GetSessionMetrics()));

            Countly.Instance.BackendMode().BeginSession(deviceId: TestHelper.v[0], appKey: TestHelper.v[1], timestamp: 1044151383000);
            ValidateRequestInQueue(deviceId: TestHelper.v[0], TestHelper.v[1], Dict("begin_session", "1", "metrics", GetSessionMetrics()), 1, 2, 1044151383000);

            Countly.Instance.BackendMode().BeginSession(deviceId: TestHelper.v[0], appKey: "", timestamp: 1044151383000);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("begin_session", "1", "metrics", GetSessionMetrics()), 2, 3, 1044151383000);
        }

        [Fact]
        /// <summary>
        /// "BeginSession" with metric override
        /// Validate that an begin session request is generated and given metrics should be in the request
        /// RQ size must be 1 and all given values should exists in the request
        /// </summary>
        public void BeginSession_MetricOverride()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();
            cc.SetMetricOverride(new Dictionary<string, string> {
                { "_os", "OS" },
                { "_os_version", "OS_V" },
                { "_app_version", "AV" },
                { "_resolution", "100x100" },
                { "_locale", "LOCALE" },
                 { "_device", "Test" },
                { "_carrier", "CARRIER" },
                { "_build_version", "1.0" },
                { "", "1.0" },
                { "empty", "" } }
            );

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().BeginSession(TestHelper.v[0], TestHelper.v[1], timestamp: 1044151383000);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.v[1], Dict("begin_session", "1", "metrics", Json("_os", "OS", "_os_version", "OS_V", "_resolution", "100x100", "_app_version", "AV", "_locale", "LOCALE", "_device", "Test", "_carrier", "CARRIER", "_build_version", "1.0")), 0, 1, 1044151383000);
        }

        [Fact]
        /// <summary>
        /// "BeginSession"
        /// Validate that an begin session request is generated and given params should be in the request
        /// RQ size must be 1 and all given values should exists in the request
        /// </summary>
        public void BeginSession()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().BeginSession(TestHelper.v[0], TestHelper.v[1], DictS("_device_model", "Laptop", "c", "a"), DictS("loc", "1", "location", "1,2"), 1044151383000);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.v[1], Dict("begin_session", "1", "metrics", Json("_device_model", "Laptop", "c", "a"), "loc", "1", "location", "1,2"), 0, 1, 1044151383000);
        }

        [Fact]
        /// <summary>
        /// "EndSession" with different device id and app keys
        /// Validate that a session end request is generated after each call and expected behaviour should happen
        /// 
        /// 1. If device id is given but app key not given, app key should fallback to init given app key,
        /// 2. If both of them are given values should be match
        /// 3. If app key is given as empty string it fallbacks to default one
        /// 
        /// RQ size must increase by 1 after each call, and expected values should match
        /// </summary>
        public void EndSession_AppKeyFallback()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().EndSession(TestHelper.v[0], -1);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("end_session", 1));

            Countly.Instance.BackendMode().EndSession(TestHelper.v[0], 45, appKey: TestHelper.v[1], timestamp: 1044151383000);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.v[1], Dict("end_session", 1, "session_duration", 45), 1, 2, 1044151383000);

            Countly.Instance.BackendMode().EndSession(TestHelper.v[0], 67, "");
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("end_session", 1, "session_duration", 67), 2, 3);
        }

        [Fact]
        /// <summary>
        /// "EndSession" with different negative duration
        /// Validate that session duration is not sent with it
        /// RQ size must increase by 1 after each call, and expected values should match
        /// </summary>
        public void EndSession_NegativeDuration()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().EndSession(TestHelper.v[0], -1);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("end_session", 1));
        }

        [Fact]
        /// <summary>
        /// "UpdateSession" with different device id and app keys
        /// Validate that an update session request is generated after each call and expected behaviour should happen
        /// 
        /// 1. If device id is given but app key not given, app key should fallback to init given app key,
        /// 2. If both of them are given values should be match
        /// 3. If app key is given as empty string it fallbacks to default one
        /// 
        /// RQ size must increase by 1 after each call, and expected values should match
        /// </summary>
        public void UpdateSession_AppKeyFallback()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().UpdateSession(TestHelper.v[0], 1);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("session_duration", 1));

            Countly.Instance.BackendMode().UpdateSession(TestHelper.v[0], 1, appKey: TestHelper.v[1], timestamp: 1044151383000);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.v[1], Dict("session_duration", 1), 1, 2, 1044151383000);

            Countly.Instance.BackendMode().UpdateSession(TestHelper.v[0], 1, "");
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("session_duration", 1), 2, 3);
        }

        [Fact]
        /// <summary>
        /// "UpdateSession" with negative duration
        /// Validate that an update session request is not generated with negative duration
        /// RQ must be empty
        /// </summary>
        public void UpdateSession_NegativeDuration()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().UpdateSession(TestHelper.v[0], -11);
            Assert.Empty(Countly.Instance.StoredRequests);
        }

        [Fact]
        /// <summary>
        /// "UpdateSession"
        /// Validate that an update session request is generated and exists in the queue
        /// RQ size must be 1 and it should be a session update request
        /// </summary>
        public void UpdateSession()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().UpdateSession(TestHelper.v[0], 78, TestHelper.v[1]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.v[1], Dict("session_duration", 78));
        }

        private void ValidateEventInRequestQueue(string key, string deviceId, string appKey, int eventCount = 1, double eventSum = -1, Segmentation segmentation = null, long duration = -1, int eventIdx = 0, int rqIdx = 0, int reqCount = 1, int eventQCount = 1, long timestamp = 0)
        {
            List<CountlyEvent> events = ParseEventsFromRequestQueue(rqIdx, reqCount, deviceId, appKey);
            Assert.Equal(eventQCount, events.Count);

            Assert.Equal(key, events[eventIdx].Key);
            Assert.Equal(eventCount, events[eventIdx].Count);
            if (eventSum > 0) {
                Assert.Equal(eventSum, events[eventIdx].Sum);

            }
            if (duration > 0) {
                Assert.Equal(duration, events[eventIdx].Duration);
            }

            if (segmentation != null) {
                Assert.Equal(segmentation.segmentation.Count, events[eventIdx].Segmentation.segmentation.Count);

                foreach (SegmentationItem item in segmentation.segmentation) {
                    SegmentationItem itemK = events[eventIdx].Segmentation.segmentation.Find((itemT) => itemT.Key == item.Key);
                    Assert.Equal(itemK.Value, item.Value);
                }
            }

            if (timestamp > 0) {
                Assert.Equal(timestamp, events[eventIdx].Timestamp);

            }
            Assert.True(events[eventIdx].Timestamp > 0);
        }

        private void ValidateRequestInQueue(string deviceId, string appKey, IDictionary<string, object> paramaters, int rqIdx = 0, int rqSize = 1, long timestamp = 0)
        {
            Assert.Equal(rqSize, Countly.Instance.StoredRequests.Count);
            string request = Countly.Instance.StoredRequests.ElementAt(rqIdx).Request;
            Dictionary<string, string> queryParams = TestHelper.GetParams(request);
            ValidateBaseParams(queryParams, deviceId, appKey, timestamp);
            Assert.Equal(10 + paramaters.Count, queryParams.Count); //TODO 11 after merge
            foreach (KeyValuePair<string, object> item in paramaters) {
                Assert.Equal(item.Value.ToString(), queryParams[item.Key]);
            }
        }

        private string GetSessionMetrics()
        {
            return Json("_os", Countly.Instance.DeviceData.OS, "_os_version", Countly.Instance.DeviceData.OSVersion, "_resolution", Countly.Instance.DeviceData.Resolution, "_app_version", TestHelper.APP_VERSION, "_locale", CultureInfo.CurrentUICulture.Name);
        }

        private IDictionary<string, T> DictGeneric<T>(params T[] values)
        {
            IDictionary<string, T> result = new Dictionary<string, T>();
            if (values == null || values.Length == 0 || values.Length % 2 != 0) { return result; }

            for (int i = 0; i < values.Length; i += 2) {
                result[values[i].ToString()] = values[i + 1];
            }

            return result;
        }

        private Segmentation Segm(params string[] values)
        {
            Segmentation result = new Segmentation();
            if (values == null || values.Length == 0 || values.Length % 2 != 0) { return result; }

            for (int i = 0; i < values.Length; i += 2) {
                result.Add(values[i], values[i + 1]);
            }

            return result;
        }

        private string Json(params object[] values)
        {
            return JsonConvert.SerializeObject(Dict(values).Where(p => p.Value != null)
                .ToDictionary(p => p.Key, p => p.Value), Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        private IDictionary<string, object> Dict(params object[] values)
        {
            return DictGeneric(values);
        }

        private IDictionary<string, string> DictS(params string[] values)
        {
            return DictGeneric(values);
        }

        private List<CountlyEvent> ParseEventsFromRequestQueue(int idx, int count, string deviceId, string appKey)
        {
            Assert.Equal(count, Countly.Instance.StoredRequests.Count);
            string request = Countly.Instance.StoredRequests.ElementAt(idx).Request;
            Assert.Contains("events", request);

            Dictionary<string, string> queryParams = TestHelper.GetParams(request);
            ValidateBaseParams(queryParams, deviceId, appKey);
            Assert.Equal(11, queryParams.Count); //TODO 12 after merge

            return JsonConvert.DeserializeObject<List<CountlyEvent>>(queryParams["events"]);

        }

        private void ValidateBaseParams(Dictionary<string, string> queryParams, string deviceId, string appKey, long timestamp = 0)
        {
            //Time related params
            if (timestamp > 0) {
                TimeSpan time = TimeSpan.FromMilliseconds(timestamp);
                DateTime dateTime = new DateTime(1970, 1, 1) + time;

                int dow = (int)dateTime.DayOfWeek;
                int hour = dateTime.TimeOfDay.Hours;
                string timezone = TimeZoneInfo.Local.GetUtcOffset(dateTime).TotalMinutes.ToString(CultureInfo.InvariantCulture);

                Assert.Equal(queryParams["tz"], timezone);
                Assert.Equal(int.Parse(queryParams["hour"]), hour);
                Assert.Equal(int.Parse(queryParams["dow"]), dow);
                Assert.Equal(long.Parse(queryParams["timestamp"]), timestamp);
            } else {
                Assert.True(int.Parse(queryParams["tz"]) >= 0);
                Assert.True(int.Parse(queryParams["hour"]) >= 0);
                Assert.True(int.Parse(queryParams["dow"]) >= 0);
                Assert.True(long.Parse(queryParams["timestamp"]) > 0);
            }

            //sdk related params
            Assert.Equal(queryParams["av"], TestHelper.APP_VERSION);
            Assert.Equal(queryParams["sdk_name"], Countly.Instance.sdkName());
            Assert.Equal(queryParams["sdk_version"], TestHelper.SDK_VERSION);
            Assert.Equal(queryParams["device_id"], deviceId);
            Assert.Equal(queryParams["app_key"], appKey);
            Assert.Equal("0", queryParams["t"]);
            //Assert.True(int.Parse(queryParams["rr"]) >= 0); TODO enable after merge
        }
    }
}
