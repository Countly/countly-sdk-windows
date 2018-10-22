using CountlySDK;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.Helpers;
using PCLStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TestProject_common
{
    public class LegacyInitTests : CountlyTestCases
    {
        ITestOutputHelper output;

        /// <summary>
        /// Test setup
        /// </summary>
        public LegacyInitTests(ITestOutputHelper output)
        {
            this.output = output;
            Storage.Instance.fileSystem = FileSystem.Current;
            Countly.Halt();
            TestHelper.CleanDataFiles();
            Countly.StartSession(ServerInfo.serverURL, ServerInfo.appKey, ServerInfo.appVersion, FileSystem.Current).Wait();
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {
            TestHelper.ValidateDataPointUpload().Wait();
            Countly.EndSession().Wait();
            TestHelper.ValidateDataPointUpload().Wait();
        }        
    }
}
