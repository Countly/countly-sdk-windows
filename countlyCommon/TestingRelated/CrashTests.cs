using System;
using System.Collections.Generic;
using CountlySDK;
using CountlySDK.Entities;
using Xunit;

namespace TestProject_common
{
    public class CrashTests : IDisposable
    {
        /// <summary>
        /// Test setup
        /// </summary>
        public CrashTests()
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

        }

        [Fact]
        /// <summary>
        /// It validates the limits of total allowed bread crumbs and size of a breadcrumb.
        /// </summary>
        public void TestLimitOfAllowedBreadCrumbs()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            cc.MaxValueSize = 14;
            cc.MaxBreadcrumbCount = 5;


            Countly.Instance.Init(cc).Wait();
            Countly.Instance.SessionBegin().Wait();

            Countly.Instance.AddCrashBreadCrumb("bread_crumbs_1");
            Countly.Instance.AddCrashBreadCrumb("bread_crumbs_2");
            Countly.Instance.AddCrashBreadCrumb("bread_crumbs_3_");
            Countly.Instance.AddCrashBreadCrumb("bread_crumbs_4");
            Countly.Instance.AddCrashBreadCrumb("bread_crumbs_5");
            Countly.Instance.AddCrashBreadCrumb("bread_crumbs_6_");

            Assert.Equal(5, Countly.Instance.CrashBreadcrumbs.Count);

            Assert.Equal("bread_crumbs_2", Countly.Instance.CrashBreadcrumbs.Dequeue());
            Assert.Equal("bread_crumbs_3", Countly.Instance.CrashBreadcrumbs.Dequeue());
            Assert.Equal("bread_crumbs_4", Countly.Instance.CrashBreadcrumbs.Dequeue());
            Assert.Equal("bread_crumbs_5", Countly.Instance.CrashBreadcrumbs.Dequeue());
            Assert.Equal("bread_crumbs_6", Countly.Instance.CrashBreadcrumbs.Dequeue());

            Countly.Instance.SessionEnd().Wait();

        }

        [Fact]
        /// <summary>
        /// It validates the Count of allowed Segmentation Values.
        /// </summary>
        public void TestSizeOfAllowedSegment()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            cc.MaxValueSize = 14;
            cc.MaxBreadcrumbCount = 5;
            cc.MaxSegmentationValues = 2;


            Countly.Instance.Init(cc).Wait();
            Countly.Instance.SessionBegin().Wait();

            Dictionary<string, object> seg = new Dictionary<string, object>{
                { "Time Spent", "1234455"},
                { "Retry Attempts", "10"},
                { "Extra", "10"}
            };

            Dictionary<string, string> customInfo = new Dictionary<string, string>();
            customInfo.Add("Key1", "Value1");
            customInfo.Add("Key2", "Value2");
            customInfo.Add("Key3", "Value3");
            Countly.RecordException("error", "stackTrace", customInfo).Wait();

            ExceptionEvent model = Countly.Instance.Exceptions[0];

            Assert.Equal("error", model.Name);
            Assert.Equal("stackTrace", model.Error);
            Assert.Equal(2, model.Custom.Count);
            Assert.Equal("Value1", model.Custom["Key1"]);
            Assert.Equal("Value2", model.Custom["Key2"]);
            Assert.False(model.Custom.ContainsKey("Key3"));

            Countly.Instance.SessionEnd().Wait();

        }
    }
}
