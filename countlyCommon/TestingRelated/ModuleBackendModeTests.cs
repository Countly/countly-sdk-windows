using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using CountlySDK;
using CountlySDK.Entities;
using Newtonsoft.Json;
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
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("old_device_id", TestHelper.DEVICE_ID));
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
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("old_device_id", TestHelper.v[1]));

            Countly.Instance.BackendMode().ChangeDeviceIdWithMerge(TestHelper.v[0], appKey: TestHelper.v[1], timestamp: 1044151383000);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.v[1], Dict("old_device_id", TestHelper.DEVICE_ID), 1, 2, 1044151383000);

            Countly.Instance.BackendMode().ChangeDeviceIdWithMerge(TestHelper.v[0], TestHelper.v[1], TestHelper.v[2]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.v[2], Dict("old_device_id", TestHelper.v[1]), 2, 3);
        }

        [Fact]
        /// <summary>
        /// "EndSession" with different device id and app keys
        /// Validate that a session end request is generated after each call and expected behaviour should happen
        /// 
        /// 1. If device id is given but app key not given, app key should fallback to init given app key
        /// 2. If app key is given but device id not given, device id should fallback to generated/init given device id
        /// 3. If both of them are given values should be match
        /// 4. None of them given, both values fallback to the init generated/given values
        /// 5. Both of them are given as empty string, defaults to init generated/given values, duration given as negative, should not exist in the request
        /// 
        /// RQ size must increase by 1 after each call, and expected values should match
        /// </summary>
        public void EndSession_DeviceIdAndAppKeyFallbacks()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().EndSession(deviceId: TestHelper.v[0]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("end_session", 1));

            Countly.Instance.BackendMode().EndSession(appKey: TestHelper.v[1], timestamp: 1044151383000);
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.v[1], Dict("end_session", 1), 1, 2, 1044151383000);

            Countly.Instance.BackendMode().EndSession(45, TestHelper.v[0], TestHelper.v[1]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.v[1], Dict("end_session", 1, "session_duration", 45), 2, 3);

            Countly.Instance.BackendMode().EndSession(67);
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.APP_KEY, Dict("end_session", 1, "session_duration", 67), 3, 4);

            Countly.Instance.BackendMode().EndSession(-4, "", "");
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.APP_KEY, Dict("end_session", 1), 4, 5);
        }

        [Fact]
        /// <summary>
        /// "UpdateSession" with different device id and app keys
        /// Validate that an update session request is generated after each call and expected behaviour should happen
        /// 
        /// 1. If device id is given but app key not given, app key should fallback to init given app key,
        /// 2. If app key is given but device id not given, device id should fallback to generated/init given device id
        /// 3. If both of them are given values should be match
        /// 4. None of them given, both values fallback to the init generated/given values
        /// 5. Both of them are given as empty string, defaults to init generated/given values,
        /// 
        /// RQ size must increase by 1 after each call, and expected values should match
        /// </summary>
        public void UpdateSession_DeviceIdAndAppKeyFallbacks()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().UpdateSession(1, deviceId: TestHelper.v[0]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("session_duration", 1));

            Countly.Instance.BackendMode().UpdateSession(1, appKey: TestHelper.v[1], timestamp: 1044151383000);
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.v[1], Dict("session_duration", 1), 1, 2, 1044151383000);

            Countly.Instance.BackendMode().UpdateSession(1, TestHelper.v[0], TestHelper.v[1]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.v[1], Dict("session_duration", 1), 2, 3);

            Countly.Instance.BackendMode().UpdateSession(1);
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.APP_KEY, Dict("session_duration", 1), 3, 4);

            Countly.Instance.BackendMode().UpdateSession(1, "", "");
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.APP_KEY, Dict("session_duration", 1), 4, 5);
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

            Countly.Instance.BackendMode().UpdateSession(-11, deviceId: TestHelper.v[0]);
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

            Countly.Instance.BackendMode().UpdateSession(78, TestHelper.v[0], TestHelper.v[1]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.v[1], Dict("session_duration", 78));
        }

        [Fact]
        /// <summary>
        /// "RecordDirectRequest" with null and empty parameters
        /// Validate that a direct request is not generated with both calls, empty and null parameters
        /// RQ size must be 0 after each "RecordDirectRequest" call
        /// </summary>
        public void RecordDirectRequest_NullOrEmpty()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().RecordDirectRequest(null, TestHelper.v[0], TestHelper.v[1]);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().RecordDirectRequest(new Dictionary<string, string>(), TestHelper.v[0]);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
        }

        [Fact]
        /// <summary>
        /// "RecordDirectRequest" with different device id and app keys
        /// Validate that a direct request is generated after each call and expected behaviour should happen
        /// 
        /// 1. If device id is given but app key not given, app key should fallback to init given app key,
        /// 2. If app key is given but device id not given, device id should fallback to generated/init given device id
        /// 3. If both of them are given values should be match
        /// 4. None of them given, both values fallback to the init generated/given values
        /// 5. Both of them are given as empty string, defaults to init generated/given values,
        /// 
        /// RQ size must increase by 1 after each call, and expected values should match
        /// </summary>
        public void RecordDirectRequest_DeviceIdAndAppKeyFallbacks()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().RecordDirectRequest(DictS("test", "true"), deviceId: TestHelper.v[0]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("test", "true", "dr", 1));

            Countly.Instance.BackendMode().RecordDirectRequest(DictS("gender", "M"), appKey: TestHelper.v[1], timestamp: 1044151383000);
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.v[1], Dict("gender", "M", "dr", 1), 1, 2, 1044151383000);

            Countly.Instance.BackendMode().RecordDirectRequest(DictS("level", "5", "class", "Knight"), TestHelper.v[0], TestHelper.v[1]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.v[1], Dict("level", "5", "class", "Knight", "dr", 1), 2, 3);

            Countly.Instance.BackendMode().RecordDirectRequest(DictS("boss", "Utyirko"));
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.APP_KEY, Dict("boss", "Utyirko", "dr", 1), 3, 4);

            Countly.Instance.BackendMode().RecordDirectRequest(DictS("pk", "yes"), "", "");
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.APP_KEY, Dict("pk", "yes", "dr", 1), 4, 5);
        }

        [Fact]
        /// <summary>
        /// "RecordDirectRequest"
        /// Validate that given parameters to the function exists in the request and dr param exists.
        /// RQ size must be 1 and request should contain "dr" parameter
        /// </summary>
        public void RecordDirectRequest()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().RecordDirectRequest(DictS("name", "SDK", "module", "Backend"), deviceId: TestHelper.v[0]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("name", "SDK", "module", "Backend", "dr", 1), rqSize: 1);
        }

        [Fact]
        /// <summary>
        /// "StartView"
        /// Validate that given parameters to the function exists in the request and visit and start params exists.
        /// RQ size must be 2 and first request should contain start and visit params, second one should contain visit param only
        ///
        /// Flow is this, also per app EQ size is 1 to generate request for every view
        /// 1. Start view with first view as true
        /// 2. Start view with non first view, provide custom segment and segmentation and timestamp
        /// </summary>
        public void StartView()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode().SetBackendModeAppEQSizeToSend(1);

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().StartView(TestHelper.v[0], TestHelper.v[1], TestHelper.v[3], null, true);
            Countly.Instance.BackendMode().StartView(TestHelper.v[0], TestHelper.v[2], TestHelper.v[4], "Android", segmentations: Segm("bip", "boop"), timestamp: 1044151383000);

            ValidateEventInRequestQueue("[CLY]_view", TestHelper.v[0], TestHelper.v[1], segmentation: Segm("name", TestHelper.v[3], "start", "1", "visit", "1", "segment", "Windows"), reqCount: 2);
            ValidateEventInRequestQueue("[CLY]_view", TestHelper.v[0], TestHelper.v[2], segmentation: Segm("name", TestHelper.v[4], "segment", "Android", "visit", "1", "bip", "boop"), reqCount: 2, rqIdx: 1, timestamp: 1044151383000);
        }

        [Fact]
        /// <summary>
        /// "StartView" with null and empty name, device id and app key, server EQ size is 1 to trigger request generation after each call
        /// Validate that no request exists in the RQ after each call
        /// RQ size must be zero after each call
        /// </summary>
        public void StartView_NullEmpty()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode().SetBackendModeServerEQSizeToSend(1);

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().StartView(TestHelper.v[0], TestHelper.v[1], null, null, true);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().StartView(TestHelper.v[0], TestHelper.v[1], "", null, true);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().StartView(TestHelper.v[0], null, "t", null, true);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().StartView(TestHelper.v[0], "", "t", null, true);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().StartView(null, TestHelper.v[1], "t", null, true);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().StartView("", TestHelper.v[1], "t", null, true);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
        }

        [Fact]
        /// <summary>
        /// "StopView" with null and empty name, device id and app key, server EQ size is 1 to trigger request generation after each call
        /// Validate that no request exists in the RQ after each call
        /// RQ size must be zero after each call
        /// </summary>
        public void StopView_NullEmpty()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode().SetBackendModeServerEQSizeToSend(1);

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().StopView(TestHelper.v[0], TestHelper.v[1], null, null, 1);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().StopView(TestHelper.v[0], TestHelper.v[1], "", null, 1);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().StopView(TestHelper.v[0], null, "t", null, 1);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().StopView(TestHelper.v[0], "", "t", null, 1);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().StopView(null, TestHelper.v[1], "t", null, 1);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
            Countly.Instance.BackendMode().StopView("", TestHelper.v[1], "t", null, 1);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
        }

        [Fact]
        /// <summary>
        /// "StopView"
        /// Validate that given parameters to the function exists in the request
        /// RQ size must be 2 and requests should contain view related segment and duration
        ///
        /// Flow is this, also server EQ size is 1 to generate request for every view
        /// 1. Stop view with positive duration, validate event in RQ first request
        /// 2. Stop view with positive duration, provide custom segment and segmentation and timestamp, validate event in rq second request
        /// 3. Stop view with negative duration, no request should be created
        /// </summary>
        public void StopView()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode().SetBackendModeServerEQSizeToSend(1);

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().StopView(TestHelper.v[0], TestHelper.v[1], TestHelper.v[3], null, 45);
            ValidateEventInRequestQueue("[CLY]_view", TestHelper.v[0], TestHelper.v[1], duration: 45, segmentation: Segm("name", TestHelper.v[3], "segment", "Windows"));

            Countly.Instance.BackendMode().StopView(TestHelper.v[0], TestHelper.v[2], TestHelper.v[4], "Android", 180, segmentations: Segm("bip", "boop"), timestamp: 1044151383000);
            ValidateEventInRequestQueue("[CLY]_view", TestHelper.v[0], TestHelper.v[2], duration: 180, segmentation: Segm("name", TestHelper.v[4], "segment", "Android", "bip", "boop"), reqCount: 2, rqIdx: 1, timestamp: 1044151383000);

            Countly.Instance.BackendMode().StopView(TestHelper.v[0], TestHelper.v[2], TestHelper.v[5], null, -56);
            Assert.Equal(2, Countly.Instance.StoredRequests.Count);
        }

        [Fact]
        /// <summary>
        /// "RecordException" with different device id and app keys
        /// Validate that an exception request is generated after each call and expected behaviour should happen
        /// 
        /// 1. If device id is given but app key not given, app key should fallback to init given app key,
        /// 2. If app key is given but device id not given, device id should fallback to generated/init given device id
        /// 3. If both of them are given values should be match
        /// 4. None of them given, both values fallback to the init generated/given values
        /// 5. Both of them are given as empty string, defaults to init generated/given values,
        /// 
        /// RQ size must increase by 1 after each call, and expected values should match
        /// </summary>
        public void RecordException_DeviceIdAndAppKeyFallbacks()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().RecordException(error: "Test", deviceId: TestHelper.v[0]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("crash", Json("_name", "Test", "_nonfatal", true)));

            Countly.Instance.BackendMode().RecordException(error: "Test", appKey: TestHelper.v[1], timestamp: 1044151383000, unhandled: true);
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.v[1], Dict("crash", Json("_name", "Test", "_nonfatal", false)), 1, 2, 1044151383000);

            Countly.Instance.BackendMode().RecordException(TestHelper.v[0], TestHelper.v[1], "Test");
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.v[1], Dict("crash", Json("_name", "Test", "_nonfatal", true)), 2, 3);

            Countly.Instance.BackendMode().RecordException(error: "Test", unhandled: true);
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.APP_KEY, Dict("crash", Json("_name", "Test", "_nonfatal", false)), 3, 4);

            Countly.Instance.BackendMode().RecordException("", "", "Test");
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.APP_KEY, Dict("crash", Json("_name", "Test", "_nonfatal", true)), 4, 5);
        }


        [Fact]
        /// <summary>
        /// "RecordException"
        /// Validate that an exception request is generated and expected values should match, and unsupported custom data type should be erased
        /// RQ size must increase by 1 after each call, and expected values should match
        /// </summary>
        public void RecordException()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();
            IList<string> breadcrumbs = new List<string> {
                "Given",
                "Breadcrumb"
            };

            IDictionary<string, object> customInfo = Dict(
                "int", 5,
                "long", 1044151383000,
                "float", 56.45678,
                "string", "value",
                "bool", true,
                "double", -5.4E-79,
                "invalid", Dict("test", "out")
                );

            Countly.Instance.BackendMode().RecordException(TestHelper.v[0], TestHelper.v[1], "Crashed", "Trace", breadcrumbs, customInfo, null, true, 1044151383000);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.v[1], Dict("crash", Json("_name", "Crashed", "_nonfatal", false,
                "_logs", string.Join("\n", breadcrumbs.ToArray()), "_error", "Trace",
                "_custom", Dict("int", 5, "long", 1044151383000, "float", 56.45678, "string", "value", "bool", true, "double", -5.4E-79))));
        }

        [Fact]
        /// <summary>
        /// "RecordException" with null and empty error
        /// Validate that an exception request is not generated after each call
        /// RQ size must increase 0 after each call
        /// </summary>
        public void RecordException_NullAndEmptyError()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().RecordException(TestHelper.v[0], TestHelper.v[1], "");
            Assert.True(Countly.Instance.StoredRequests.Count == 0);

            Countly.Instance.BackendMode().RecordException(TestHelper.v[0], TestHelper.v[1], null);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
        }

        [Fact]
        /// <summary>
        /// "RecordException" with metrics
        /// Validate that an exception request is generated with provided supported metrics
        /// RQ size must be 1 and supported metrics should exist in the request
        /// </summary>
        public void RecordException_Metrics()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            IDictionary<string, string> metrics = DictS("_device_brand", "Mac", "_user", "localhost", "_os", "MyOs", "_os_version", "MyOs1.2", "_ram_total", "1024",
                "_ram_current", "512", "_disk_total", "1024", "_disk_current", "512", "_online", "false", "_muted", "false", "_orientation", "Portrait",
                "_resolution", "1x1", "_app_version", "1.2", "_manufacture", "MyCompany", "_device", "MyDevice");

            Countly.Instance.BackendMode().RecordException(TestHelper.v[0], TestHelper.v[1], "Error", metrics: metrics);
            metrics.Remove("_device_brand");
            metrics.Remove("_user");

            ValidateRequestInQueue(TestHelper.v[0], TestHelper.v[1], Dict("crash", Json("_name", "Error", "_nonfatal", true, "_os", "MyOs", "_os_version", "MyOs1.2", "_ram_total", "1024",
                "_ram_current", "512", "_disk_total", "1024", "_disk_current", "512", "_online", "false", "_muted", "false", "_orientation", "Portrait",
                "_resolution", "1x1", "_app_version", "1.2", "_manufacture", "MyCompany", "_device", "MyDevice")));

        }

        [Fact]
        /// <summary>
        /// "BeginSession" with different device id and app keys
        /// Validate that an begin session request is generated after each call and expected behaviour should happen
        /// 
        /// 1. If device id is given but app key not given, app key should fallback to init given app key,
        /// 2. If app key is given but device id not given, device id should fallback to generated/init given device id
        /// 3. If both of them are given values should be match
        /// 4. None of them given, both values fallback to the init generated/given values
        /// 5. Both of them are given as empty string, defaults to init generated/given values,
        /// 
        /// RQ size must increase by 1 after each call, and expected values should match
        /// </summary>
        public void BeginSession_DeviceIdAndAppKeyFallbacks()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().BeginSession(deviceId: TestHelper.v[0]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("begin_session", "1", "metrics", GetSessionMetrics()));

            Countly.Instance.BackendMode().BeginSession(appKey: TestHelper.v[1], timestamp: 1044151383000);
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.v[1], Dict("begin_session", "1", "metrics", GetSessionMetrics()), 1, 2, 1044151383000);

            Countly.Instance.BackendMode().BeginSession(TestHelper.v[0], TestHelper.v[1]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.v[1], Dict("begin_session", "1", "metrics", GetSessionMetrics()), 2, 3);

            Countly.Instance.BackendMode().BeginSession();
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.APP_KEY, Dict("begin_session", "1", "metrics", GetSessionMetrics()), 3, 4);

            Countly.Instance.BackendMode().BeginSession();
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.APP_KEY, Dict("begin_session", "1", "metrics", GetSessionMetrics()), 4, 5);
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
        /// "RecordUserProperties" with different device id and app keys
        /// Validate that a user properties request is generated after each call and expected behaviour should happen
        /// 
        /// 1. If device id is given but app key not given, app key should fallback to init given app key,
        /// 2. If app key is given but device id not given, device id should fallback to generated/init given device id
        /// 3. If both of them are given values should be match
        /// 4. None of them given, both values fallback to the init generated/given values
        /// 5. Both of them are given as empty string, defaults to init generated/given values,
        /// 
        /// RQ size must increase by 1 after each call, and expected values should match
        /// </summary>
        public void RecordUserProperties_DeviceIdAndAppKeyFallbacks()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().RecordUserProperties(Dict("name", "John"), deviceId: TestHelper.v[0]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("user_details", Json("name", "John")));

            Countly.Instance.BackendMode().RecordUserProperties(Dict("name", "John"), appKey: TestHelper.v[1], timestamp: 1044151383000);
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.v[1], Dict("user_details", Json("name", "John")), 1, 2, 1044151383000);

            Countly.Instance.BackendMode().RecordUserProperties(Dict("name", "John"), TestHelper.v[0], TestHelper.v[1]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.v[1], Dict("user_details", Json("name", "John")), 2, 3);

            Countly.Instance.BackendMode().RecordUserProperties(Dict("name", "John"));
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.APP_KEY, Dict("user_details", Json("name", "John")), 3, 4);

            Countly.Instance.BackendMode().RecordUserProperties(Dict("name", "John"));
            ValidateRequestInQueue(TestHelper.DEVICE_ID, TestHelper.APP_KEY, Dict("user_details", Json("name", "John")), 4, 5);
        }

        [Fact]
        /// <summary>
        /// "RecordUserProperties" with different user properties
        /// Validate that a user properties request is generated after each call and expected parameters should be added
        /// RQ size must be 1 and parameters should match
        /// </summary>
        public void RecordUserProperties()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            IDictionary<string, object> userProperties = Dict(
                 "int", 5,
                 "long", 1044151383000,
                 "float", 56.45678,
                 "string", "value",
                 "bool", true,
                 "double", -5.4E-79,
                 "invalid", Dict("test", "out"),
                 "name", "John",
                 "username", "Dohn",
                 "organization", "Fohn",
                 "email", "johnjohn@john.jo",
                 "phone", "+123456789",
                 "gender", "Unkown",
                 "byear", 1969,
                 "picture", "http://someurl.png",
                 "nullable", null,
                 "action", "{$push: \"black\"}"
             );

            Countly.Instance.BackendMode().RecordUserProperties(userProperties, deviceId: TestHelper.v[0]);
            ValidateRequestInQueue(TestHelper.v[0], TestHelper.APP_KEY, Dict("user_details", Json("name", "John", "username", "Dohn",
                 "organization", "Fohn",
                 "email", "johnjohn@john.jo",
                 "phone", "+123456789",
                 "gender", "Unkown",
                 "byear", 1969,
                 "picture", "http://someurl.png", "custom", Dict("int", 5,
                 "long", 1044151383000,
                 "float", 56.45678,
                 "string", "value",
                 "bool", true,
                 "double", -5.4E-79,
                 "action", Dict("$push", "black")))));
        }

        [Fact]
        /// <summary>
        /// "RecordUserProperties" with null and empty properties
        /// Validate that a user properties request is not generated after each call
        /// RQ size must increase 0 after each call
        /// </summary>
        public void RecordUserProperties_NullAndEmptyProperties()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.EnableBackendMode();

            Countly.Instance.Init(cc).Wait();

            Countly.Instance.BackendMode().RecordUserProperties(null, TestHelper.v[0], TestHelper.v[1]);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);

            Countly.Instance.BackendMode().RecordUserProperties(Dict(), TestHelper.v[0], TestHelper.v[1]);
            Assert.True(Countly.Instance.StoredRequests.Count == 0);
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
            Assert.Equal(9 + paramaters.Count, queryParams.Count); //TODO 11 after merge
            foreach (KeyValuePair<string, object> item in paramaters) {
                Assert.Equal(queryParams[item.Key], item.Value.ToString());
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
