using System;
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
        /// and this causes NullReferenceException
        /// </summary>
        public async void RecordEvent_BackendModeNotEnabled()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            Countly.Instance.Init(cc).Wait();

            Assert.Throws<NullReferenceException>(() => Countly.Instance.BackendMode().RecordEvent("", "", ""));

        }


        [Fact]
        /// <summary>
        /// Validate that every call to "RecordEvent" function of the BackenMode do not create a request in the queue
        /// All event queue sizes are given as 1 to trigger request creation.
        /// After each call validating that request queue size is 0
        /// </summary>
        public async void RecordEvent_NullOrEmpty_AppKey_DeviceID_EventKey()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();
            // made all queues 1 to look to the queue to detect eq changes
            cc.SetEventQueueSizeToSend(1).SetBackendModeAppEQSizeToSend(1).SetBackendModeServerEQSizeToSend(1);

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.BackendMode().RecordEvent("", "APP_KEY", "EVENT_KEY");
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().RecordEvent(null, "APP_KEY", "EVENT_KEY");
            Assert.True(Countly.Instance.StoredRequests.Count == 0);

            Countly.Instance.BackendMode().RecordEvent("DEVICE_ID", "APP_KEY", "");
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().RecordEvent("DEVICE_ID", "APP_KEY", null);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);

            Countly.Instance.BackendMode().RecordEvent("DEVICE_ID", "", "EVENT_KEY");
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().RecordEvent("DEVICE_ID", null, "EVENT_KEY");
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
        }

        [Fact]
        /// <summary>
        /// Validate that "RecordEvent" function of the BackenMode does create positive timestamp event even if it is given as negative
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
        /// Validate that "RecordEvent" function of the BackenMode does create positive count event even if it is given as negative
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
        /// Validate that "RecordEvent" function of the BackenMode with bunch of valid parameters
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
        /// Validate that "RecordEvent" function of the BackenMode with bunch of valid parameters
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
        /// Validate that "RecordEvent" function of the BackenMode with bunch of valid parameters
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
        /// Validate that "RecordEvent" function of the BackenMode with bunch of valid parameters
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
        /// "ChangeDeviceIdWithMerge" with init given deivce id
        /// Validate that a device id merge request is generated and exists with the init given device id
        /// RQ size must be 1 and expected values should match
        /// </summary>
        public void ChangeDeviceIdWithMerge()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();


            Countly.Instance.BackendMode().ChangeDeviceIdWithMerge(TestHelper.v[0]);
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.APP_KEY, Dict("old_device_id", TestHelper.v[0]));
        }

        [Fact]
        /// <summary>
        /// "ChangeDeviceIdWithMerge" with null or empty old device id
        /// Validate that a device id merge request is not generated
        /// RQ size must be 0 after each call
        /// </summary>
        public void ChangeDeviceIdWithMerge_NullOrEmpty()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();


            Countly.Instance.BackendMode().ChangeDeviceIdWithMerge("");
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().ChangeDeviceIdWithMerge(null);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
        }

        [Fact]
        /// <summary>
        /// "ChangeDeviceIdWithMerge" with different device id and app keys
        /// Validate that a device id merge request is generated after each call and expected behaviour should happen
        /// 
        /// 1. If device id is given but app key not given, app key should fallback to init given app key
        /// 2. If app key is given but device id not given, device id should fallback to generated/init given device id
        /// 3. If both of them are given values should be match
        /// 
        /// RQ size must increase by 1 after each call, and expected values should match
        /// </summary>
        public void ChangeDeviceIdWithMerge_DeviceIdAndAppKeyFallbacks()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();


            Countly.Instance.BackendMode().ChangeDeviceIdWithMerge(TestHelper.v[0], TestHelper.v[1]);
            ValidateRequestInQueue(TestHelper.v[1], TestHelper.APP_KEY, Dict("old_device_id", TestHelper.v[0]));

            Countly.Instance.BackendMode().ChangeDeviceIdWithMerge(TestHelper.v[0], appKey: TestHelper.v[1]);
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.v[1], Dict("old_device_id", TestHelper.v[0]), 1, 2);

            Countly.Instance.BackendMode().ChangeDeviceIdWithMerge(TestHelper.v[0], TestHelper.v[1], TestHelper.v[2]);
            ValidateRequestInQueue(TestHelper.v[1], TestHelper.v[2], Dict("old_device_id", TestHelper.v[0]), 2, 3);
        }

        private void ValidateEventInRequestQueue(string key, string deviceId, string appKey, int eventCount = 1, double eventSum = -1, Segmentation segmentation = null, long duration = -1, int eventIdx = 0, int rqIdx = 0, int reqCount = 1, int eventQCount = 1)
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
            Assert.Equal(segmentation, events[eventIdx].Segmentation);
            Assert.True(events[eventIdx].Timestamp > 0);
        }

        private void ValidateRequestInQueue(string deviceId, string appKey, IDictionary<string, object> paramaters, int rqIdx = 0, int rqSize = 1, long timestamp = 0)
        {
            Assert.Equal(rqSize, Countly.Instance.StoredRequests.Count);
            string request = Countly.Instance.StoredRequests.ElementAt(rqIdx).Request;

            Dictionary<string, string> queryParams = TestHelper.GetParams(request);
            ValidateBaseParams(queryParams, deviceId, appKey, timestamp);
            Assert.Equal(9 + paramaters.Count, queryParams.Count); //TODO 11 after merge
            foreach (KeyValuePair<string, object> item in paramaters) {
                Assert.Equal(queryParams[item.Key], paramaters[item.Key].ToString());
            }
        }

        private IDictionary<string, object> Dict(params object[] values)
        {
            IDictionary<string, object> result = new Dictionary<string, object>();
            if (values == null || values.Length == 0 || values.Length % 2 != 0) { return result; }

            for (int i = 0; i < values.Length; i += 2) {
                result[values[i].ToString()] = values[i + 1];
            }

            return result;
        }

        private List<CountlyEvent> ParseEventsFromRequestQueue(int idx, int count, string deviceId, string appKey)
        {
            Assert.Equal(count, Countly.Instance.StoredRequests.Count);
            string request = Countly.Instance.StoredRequests.ElementAt(idx).Request;
            Assert.Contains("events", request);

            Dictionary<string, string> queryParams = TestHelper.GetParams(request);
            ValidateBaseParams(queryParams, deviceId, appKey);
            Assert.Equal(10, queryParams.Count); //TODO 12 after merge

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
            //Assert.Equal(queryParams["av"], TestHelper.APP_VERSION); TODO enable after merge
            Assert.Equal(queryParams["sdk_name"], Countly.Instance.sdkName());
            Assert.Equal(queryParams["sdk_version"], TestHelper.SDK_VERSION);
            Assert.Equal(queryParams["device_id"], deviceId);
            Assert.Equal(queryParams["app_key"], appKey);
            Assert.Equal("0", queryParams["t"]);
            //Assert.True(int.Parse(queryParams["rr"]) >= 0); TODO enable after merge
        }
    }
}
