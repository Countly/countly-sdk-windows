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
        /// It validates the limit of total allowed bread crumbs.
        /// </summary>
        public void TestLimitOfAllowedBreadCrumbs()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            cc.MaxBreadcrumbCount = 5;


            Countly.Instance.Init(cc).Wait();
            Countly.Instance.SessionBegin().Wait();

            Countly.AddBreadCrumb("bread_crumbs_1");
            Countly.AddBreadCrumb("bread_crumbs_2");
            Countly.AddBreadCrumb("bread_crumbs_3");
            Countly.AddBreadCrumb("bread_crumbs_4");
            Countly.AddBreadCrumb("bread_crumbs_5");
            Countly.AddBreadCrumb("bread_crumbs_6");

            Assert.Equal(5, Countly.Instance.CrashBreadcrumbs.Count);

            Assert.Equal("bread_crumbs_2", Countly.Instance.CrashBreadcrumbs.Dequeue());
            Assert.Equal("bread_crumbs_3", Countly.Instance.CrashBreadcrumbs.Dequeue());
            Assert.Equal("bread_crumbs_4", Countly.Instance.CrashBreadcrumbs.Dequeue());
            Assert.Equal("bread_crumbs_5", Countly.Instance.CrashBreadcrumbs.Dequeue());
            Assert.Equal("bread_crumbs_6", Countly.Instance.CrashBreadcrumbs.Dequeue());

            Countly.Instance.SessionEnd().Wait();

        }
    }
}
