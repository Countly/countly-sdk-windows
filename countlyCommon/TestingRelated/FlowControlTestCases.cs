using CountlySDK;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TestProject_common
{
    public class FlowControlTestCases : IDisposable
    {
        /// <summary>
        /// Test setup
        /// </summary>
        public FlowControlTestCases()
        {
            TestHelper.CleanDataFiles();
            Countly.Halt();
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {
        }

        [Fact]
        public async void SimpleControlFlow()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            await Countly.Instance.Init(cc);
            await Countly.Instance.SessionBegin();
            await Countly.Instance.SessionUpdate(10);
            Countly.Instance.lastSessionUpdateTime = DateTime.Now.AddSeconds(-10);
            await Countly.Instance.SessionEnd();
            await TestHelper.ValidateDataPointUpload();
        }

        [Fact]
        public async void LegacyInitSimple()
        {            
            await CountlyImpl.StartLegacyCountlySession("123", "234", "345");
            await Countly.EndSession();
        }

        [Fact]
        public async void LegacyInitSimpleFail()
        {
            Exception exToCatch = null;

            try
            {
                await CountlyImpl.StartLegacyCountlySession(null, "234", "345");
            }
            catch(Exception ex) { exToCatch = ex; }

            Assert.NotNull(exToCatch);
            exToCatch = null;
            Countly.Halt();

            try
            {
                await CountlyImpl.StartLegacyCountlySession("123", null, "345");
            }
            catch (Exception ex) { exToCatch = ex; }

            Assert.NotNull(exToCatch);
            exToCatch = null;
            Countly.Halt();

            try
            {
                await CountlyImpl.StartLegacyCountlySession(null, null, null);
            }
            catch (Exception ex) { exToCatch = ex; }

            Assert.NotNull(exToCatch);
            exToCatch = null;            
        }
    }
}
