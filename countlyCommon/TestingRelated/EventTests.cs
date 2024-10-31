using System;
using System.Collections.Generic;
using CountlySDK;
using CountlySDK.Entities;
using Xunit;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace TestProject_common
{
    public class EventTests : IDisposable
    {

        private readonly string _serverUrl = "https://xyz.com/";
        private readonly string _appKey = "772c091355076ead703f987fee94490";
        /// <summary>
        /// Test setup
        /// </summary>
        public EventTests()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Halt();
            TestHelper.CleanDataFiles();
            Countly.Instance.deferUpload = true;
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {
            Countly.Instance.HaltInternal().Wait();
        }

        private void validateEventData(CountlyEvent countlyEvent, string key, int count, double sum, double dur, Segmentation segmentation = null)
        {
            Assert.Equal(key, countlyEvent.Key);
            Assert.Equal(sum, countlyEvent.Sum);
            Assert.Equal(count, countlyEvent.Count);
            Assert.True(countlyEvent.Duration >= dur);

            if (segmentation != null) {
                Assert.Equal(0, segmentation.CompareTo(countlyEvent.Segmentation));
            } else {
                Assert.Null(countlyEvent.Segmentation);
            }

        }

        [Fact]
        /// <summary>
        /// It validates the limits key, value and segmentation of an event.
        /// </summary>
        public async void TestEventLimits()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            cc.MaxKeyLength = 4;
            cc.MaxValueSize = 6;
            cc.MaxSegmentationValues = 2;

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.SessionBegin().Wait();

            Countly.Instance.deferUpload = true;

            Segmentation segm = new Segmentation();
            segm.Add("key1", "value1");
            segm.Add("key2_00", "value2_00");
            segm.Add("key3_00", "value3");

            bool res = await Countly.RecordEvent("test_event", 1, 23, 5.0, Segmentation: segm);
            Assert.True(res);

            CountlyEvent countlyEvent = Countly.Instance.Events[0];
            validateEventData(countlyEvent, "test", 1, 23, 5, segm);
            Countly.Instance.SessionEnd().Wait();
        }

        [Fact]
        /// <summary>
        /// It validates that segmentation cannot have same keys
        /// </summary>
        public async void TestEventSameSegmentationKey()
        {
            CountlyConfig cc = TestHelper.CreateConfig();

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.SessionBegin().Wait();

            Countly.Instance.deferUpload = true;

            Segmentation segm = new Segmentation();
            segm.Add("key1", "value1");
            segm.Add("key2", "value2");
            segm.Add("key2", "value3");

            Assert.Equal("key1", segm.segmentation[0].Key);
            Assert.Equal("value1", segm.segmentation[0].Value);
            Assert.Equal("key2", segm.segmentation[1].Key);
            Assert.Equal("value3", segm.segmentation[1].Value);

            Assert.True(segm.segmentation.Count == 2); // not 3 becasue key 2 overridden and segmentation does not permit same keys
            bool res = await Countly.RecordEvent("test_event", 1, 23, 5.0, Segmentation: segm);
            Assert.True(res);

            CountlyEvent countlyEvent = Countly.Instance.Events[0];
            validateEventData(countlyEvent, "test_event", 1, 23, 5, segm);
            Countly.Instance.SessionEnd().Wait();
        }

        /// <summary>
        /// It validates the cancellation of timed events on changing device id without merge.
        ///
        /// todo potentially flakey test
        /// </summary>
        [Fact]
        public async void TestTimedEventsCancelationOnDeviceIdChange()
        {
            CountlyConfig configuration = new CountlyConfig {
                serverUrl = _serverUrl,
                appKey = _appKey,
            };

            Countly.Instance.Init(configuration).Wait();

            Countly.Instance.StartEvent("test_event");
            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(1, Countly.Instance.TimedEvents.Count);

            Countly.Instance.StartEvent("test_event_1");
            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(2, Countly.Instance.TimedEvents.Count);

            await Countly.Instance.ChangeDeviceId("new_device_id");
            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(0, Countly.Instance.TimedEvents.Count);
        }

        /// <summary>
        /// It validates functionality of 'Timed Events' methods .
        /// </summary>
        [Fact]
        public void TestTimedEventMethods()
        {
            CountlyConfig configuration = new CountlyConfig {
                serverUrl = _serverUrl,
                appKey = _appKey,
            };

            Countly.Instance.Init(configuration).Wait();

            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(0, Countly.Instance.TimedEvents.Count);

            // Start a timed event
            Countly.Instance.StartEvent("test_event");
            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(1, Countly.Instance.TimedEvents.Count);

            // Start an existing timed event
            Countly.Instance.StartEvent("test_event");
            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(1, Countly.Instance.TimedEvents.Count);

            // Start another timed event
            Countly.Instance.StartEvent("test_event_1");
            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(2, Countly.Instance.TimedEvents.Count);

            // Cancel a timed event
            Countly.Instance.CancelEvent("test_event_1");
            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(1, Countly.Instance.TimedEvents.Count);

            // Cancel a not started timed event
            Countly.Instance.CancelEvent("test_event_2");
            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(1, Countly.Instance.TimedEvents.Count);

            // End a canceled timed event
            Countly.Instance.EndEvent("test_event_1").Wait();
            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(1, Countly.Instance.TimedEvents.Count);

            System.Threading.Thread.Sleep(2000);

            // End a timed event
            Countly.Instance.EndEvent("test_event").Wait();
            Assert.Single(Countly.Instance.Events);
            Assert.Equal(0, Countly.Instance.TimedEvents.Count);

            CountlyEvent countlyEvent = Countly.Instance.Events[0];
            validateEventData(countlyEvent, "test_event", 1, 0, 2);
        }


        /// <summary>
        /// It validates functionality of method 'RecordEventAsync'.
        /// </summary>
        [Fact]
        public void TestTimedEventWithSegmentation()
        {
            CountlyConfig configuration = new CountlyConfig {
                serverUrl = _serverUrl,
                appKey = _appKey,
            };

            Countly.Instance.Init(configuration).Wait();

            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(0, Countly.Instance.TimedEvents.Count);

            // Start a timed event
            Countly.Instance.StartEvent("test_event");
            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(1, Countly.Instance.TimedEvents.Count);

            // Start another timed event
            Countly.Instance.StartEvent("test_event_1");
            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(2, Countly.Instance.TimedEvents.Count);

            System.Threading.Thread.Sleep(3000);

            Segmentation segm = new Segmentation();
            segm.Add("key1", "value1");
            segm.Add("key2", "value2");

            // End a timed event
            Countly.Instance.EndEvent("test_event", segm, 5, 10).Wait();
            Assert.Single(Countly.Instance.Events);
            Assert.Equal(1, Countly.Instance.TimedEvents.Count);

            CountlyEvent countlyEvent = Countly.Instance.Events[0];
            validateEventData(countlyEvent, "test_event", 5, 10, 3, segm);
        }

        /// <summary>
        /// It validates the cancellation of timed events on consent removal.
        /// </summary>
        [Fact]
        public void TestTimedEventsCancelationOnConsentRemoval()
        {
            CountlyConfig configuration = new CountlyConfig {
                serverUrl = _serverUrl,
                appKey = _appKey,
                consentRequired = true
            };

            Countly.Instance.Init(configuration).Wait();

            Assert.Empty(Countly.Instance.Events);

            Countly.Instance.StartEvent("test_event");
            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(0, Countly.Instance.TimedEvents.Count);

            //Give 'event' consent
            Countly.Instance.SetConsent(new Dictionary<ConsentFeatures, bool>() { { ConsentFeatures.Events, true } }).Wait();

            Countly.Instance.StartEvent("test_event");
            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(1, Countly.Instance.TimedEvents.Count);

            Countly.Instance.StartEvent("test_event_1");
            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(2, Countly.Instance.TimedEvents.Count);

            //remove 'event' consent
            Countly.Instance.SetConsent(new Dictionary<ConsentFeatures, bool>() { { ConsentFeatures.Events, false } }).Wait();
            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(0, Countly.Instance.TimedEvents.Count);

            Countly.Instance.StartEvent("test_event");
            Assert.Empty(Countly.Instance.Events);
            Assert.Equal(0, Countly.Instance.TimedEvents.Count);
        }
    }
}
