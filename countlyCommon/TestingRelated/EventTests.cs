using CountlySDK;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace TestProject_common
{
    public class EventTests : IDisposable
    {
        /// <summary>
        /// Test setup
        /// </summary>
        public EventTests()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Halt();
            TestHelper.CleanDataFiles();
            Countly.Instance.deferUpload = false;
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {

        }

        [Fact]
        /// <summary>
        /// It validates the event key, value and segmentation limits.
        /// </summary>
        public async void TestEventLimits()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            cc.MaxKeyLength = 4;
            cc.MaxValueSize = 6;
            cc.MaxSegmentationValues = 2;

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.SessionBegin().Wait();



            Dictionary<string, object> segments = new Dictionary<string, object>{
            { "key1", "value1"},
            { "key2_00", "value2_00"},
            { "key3_00", "value3"}
            };

            Segmentation segm = new Segmentation();
            segm.Add("key1", "value1");
            segm.Add("key2_00", "value2_00");
            segm.Add("key3_00", "value3");

            bool res = await Countly.RecordEvent("test_event", 1, 23, 5.0, Segmentation: segm);
            Assert.True(res);

            CountlyEvent model = Countly.Instance.Events[0];
            Assert.Equal("test", model.Key);
            Assert.Equal(23, model.Sum);
            Assert.Equal(1, model.Count);
            Assert.Equal(5, model.Duration);
            Assert.Equal(5, model.Segmentation.segmentation.Count);

            SegmentationItem item = model.Segmentation.segmentation[0];
            Assert.Equal("key1", item.Key);
            Assert.Equal("value1", item.Value);

            item = model.Segmentation.segmentation[1];
            Assert.Equal("key2", item.Key);
            Assert.Equal("value2", item.Value);

        }
    }
}
