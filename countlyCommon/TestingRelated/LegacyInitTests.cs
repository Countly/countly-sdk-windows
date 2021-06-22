using CountlySDK;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TestProject_common
{
    public class LegacyInitTests : CountlyTestCases
    {
        /// <summary>
        /// Test setup
        /// </summary>
        public LegacyInitTests()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Halt();
            TestHelper.CleanDataFiles();
            CountlyImpl.StartLegacyCountlySession(ServerInfo.serverURL, ServerInfo.appKey, ServerInfo.appVersion).Wait();
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {
            TestHelper.ValidateDataPointUpload().Wait();
            //the original legacy call has been removed 
            //this is added only for compatability
            Countly.Instance.SessionEnd().Wait();
            TestHelper.ValidateDataPointUpload().Wait();
        }
    }
}
