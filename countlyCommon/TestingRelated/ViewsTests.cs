using System;
using CountlySDK;
using CountlySDK.Entities;
using Xunit;

namespace TestProject_common
{
    public class ViewsTests : IDisposable
    {
        /// <summary>
        /// Test setup
        /// </summary>
        public ViewsTests()
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
        /// It validates the limit of the view's name size.
        ///
        /// todo potentially flakey test
        /// </summary>
        public async void TestEventLimits()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            cc.MaxKeyLength = 5;


            Countly.Instance.Init(cc).Wait();
            Countly.Instance.SessionBegin().Wait();

            Countly.Instance.deferUpload = true;

            bool res = await Countly.Instance.RecordView("open_view");
            Assert.True(res);

            CountlyEvent model = Countly.Instance.Events[0];

            SegmentationItem item = model.Segmentation.segmentation[0];
            Assert.Equal("name", item.Key);
            Assert.Equal("open_", item.Value);

            item = model.Segmentation.segmentation[1];
            Assert.Equal("visit", item.Key);
            Assert.Equal("1", item.Value);

            item = model.Segmentation.segmentation[2];
            Assert.Equal("segment", item.Key);
            Assert.Equal("Windows", item.Value);

            item = model.Segmentation.segmentation[3];
            Assert.Equal("start", item.Key);
            Assert.Equal("1", item.Value);

            Countly.Instance.SessionEnd().Wait();

        }
    }
}
