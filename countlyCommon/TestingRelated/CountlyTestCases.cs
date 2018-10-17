using CountlySDK;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using PCLStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace TestProject_common
{
    public class CountlyTestCases : IDisposable
    {
        ITestOutputHelper output;

        /// <summary>
        /// Test setup
        /// </summary>
        public CountlyTestCases(ITestOutputHelper output)
        {
            this.output = output;   
            Countly.StartSession(ServerInfo.serverURL, ServerInfo.appKey, ServerInfo.appVersion, FileSystem.Current).Wait();
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {
            Countly.EndSession().Wait();
        }

        [Fact]
        public async void BasicStorage()
        {
            const String filename = "SampleFilename.xml";

            List<String> sampleValues = new List<string>();
            sampleValues.Add("Book");
            sampleValues.Add("Car");

            await Storage.Instance.DeleteFile(filename);
            List<String> res1 = await Storage.Instance.LoadFromFile<List<String>>(filename);            
            Assert.Null(res1);

            await Storage.Instance.SaveToFile<List<String>>(filename, sampleValues);

            List<String> res2 = await Storage.Instance.LoadFromFile<List<String>>(filename);

            Assert.Equal(sampleValues, res2);
        }

        [Fact]
        public async void BasicDeviceID()
        {

        }

        [Fact]
        public async void SettingUserDetails()
        {
            CountlyUserDetails cud = Countly.UserDetails;
            TestHelper.PopulateCountlyUserDetails(cud, 0, 0);
            
            bool res = await Countly.Instance.UploadUserDetails();

            Assert.True(res);
        }

        [Fact]
        public async void MultipleExceptions()
        {
            bool res;

            try
            {
                throw new Exception("This is some bad exception 3");
            }
            catch (Exception ex)
            {
                Dictionary<string, string> customInfo = new Dictionary<string, string>();
                customInfo.Add("customData", "importantStuff");
                res = await Countly.RecordException(ex.Message, ex.StackTrace, customInfo);
                Assert.True(res);
            }

            Exception exToUse;
            try
            {
                throw new Exception("This is some bad exception 35454");
            }
            catch (Exception ex)
            {
                exToUse = ex;
            }

            Dictionary<String, String> dict = new Dictionary<string, string>();
            dict.Add("booh", "waah");
           
            res = await Countly.RecordException("Big error 1");
            Assert.True(res);

            res = await Countly.RecordException(exToUse.Message, exToUse.StackTrace);
            Assert.True(res);

            res = await Countly.RecordException(exToUse.Message, exToUse.StackTrace, dict);
            Assert.True(res);

            res = await Countly.RecordException(exToUse.Message, exToUse.StackTrace, dict, false);
            Assert.True(res);
        }

        [Fact]
        public async void MultipleEvents()
        {
            bool res;
            res = await Countly.RecordEvent("Some event0");
            Assert.True(res);

            res = await Countly.RecordEvent("Some event1", 123);
            Assert.True(res);

            res = await Countly.RecordEvent("Some event2", 123, 456);
            Assert.True(res);

            Segmentation segm = new Segmentation();
            segm.Add("oneKey", "SomeValue");
            segm.Add("anotherKey", "SomeOtherValue");

            res = await Countly.RecordEvent("Some event3", 123, 456, segm);
            Assert.True(res);
        }
    }
}
