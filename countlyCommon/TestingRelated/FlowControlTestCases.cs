using CountlySDK;
using CountlySDK.CountlyCommon.Helpers;
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
    public class FlowControlTestCases : IDisposable
    {
        ITestOutputHelper output;

        /// <summary>
        /// Test setup
        /// </summary>
        public FlowControlTestCases(ITestOutputHelper output)
        {
            this.output = output;
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
        public async void LegacyInitSimple()
        {
            await Countly.StartSession("123", "234", "345", FileSystem.Current);
            await Countly.EndSession();
        }

        [Fact]
        public async void LegacyInitSimpleFail()
        {
            Exception exToCatch = null;

            try
            {
                await Countly.StartSession(null, "234", "345", FileSystem.Current);
            }
            catch(Exception ex) { exToCatch = ex; }

            Assert.NotNull(exToCatch);
            exToCatch = null;
            Countly.Halt();

            try
            {
                await Countly.StartSession("123", null, "345", FileSystem.Current);
            }
            catch (Exception ex) { exToCatch = ex; }

            Assert.NotNull(exToCatch);
            exToCatch = null;
            Countly.Halt();

            try
            {
                await Countly.StartSession(null, null, null, FileSystem.Current);
            }
            catch (Exception ex) { exToCatch = ex; }

            Assert.NotNull(exToCatch);
            exToCatch = null;            
        }
    }
}
