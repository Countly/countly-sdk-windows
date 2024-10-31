using System;
using System.Collections.Generic;
using CountlySDK;
using CountlySDK.Entities;
using Xunit;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace TestProject_common
{
    public class SegmentationTests : IDisposable
    {
        /// <summary>
        /// Test setup
        /// </summary>
        public SegmentationTests()
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

        [Fact]
        /// <summary>
        /// It validates the invalid key values
        /// All values are accepted
        /// Only validated invalid keys
        /// </summary>
        public void InvalidKeyValues()
        {
            Segmentation segmentation = new Segmentation();

            segmentation.Add(null, null);
            Assert.Empty(segmentation.segmentation);

            segmentation.Add("", null);
            Assert.Empty(segmentation.segmentation);

            segmentation.Add(" ", null);
            Assert.Equal(" ", segmentation.segmentation[0].Key);
            Assert.Null(segmentation.segmentation[0].Value);
        }

        [Fact]
        /// <summary>
        /// It validates valid values
        /// </summary>
        public void ValidKeyValues()
        {
            Segmentation segmentation = new Segmentation();

            segmentation.Add("1", "2");
            Assert.Equal("1", segmentation.segmentation[0].Key);
            Assert.Equal("2", segmentation.segmentation[0].Value);

            segmentation.Add("3", "4");
            Assert.Equal("3", segmentation.segmentation[1].Key);
            Assert.Equal("4", segmentation.segmentation[1].Value);
            Assert.Equal(2, segmentation.segmentation.Count);

        }

        [Fact]
        /// <summary>
        /// It validates valid values with same keys,
        /// this test shows no same keys accepted
        /// </summary>
        public void ValidKeyValues_SameKeys()
        {
            Segmentation segmentation = new Segmentation();

            segmentation.Add("1", "2");
            Assert.Equal("1", segmentation.segmentation[0].Key);
            Assert.Equal("2", segmentation.segmentation[0].Value);

            segmentation.Add("1", "4");
            Assert.Equal("1", segmentation.segmentation[0].Key);
            Assert.Equal("4", segmentation.segmentation[0].Value);
            Assert.Single(segmentation.segmentation);

        }

    }
}
