using CountlySDK;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using PCLStorage;
using System;
using System.Collections.Generic;
using System.IO;
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
            TestHelper.ValidateDataPointUpload();
            Countly.EndSession().Wait();
            
        }        

        [Fact]
        public async void BasicDeviceID()
        {

        }

        [Fact]
        public async void SettingUserDetailsSingle()
        {
            CountlyUserDetails cud = Countly.UserDetails;
            cud.Name = "George";

            bool res = await Countly.Instance.Upload();

            Assert.True(res);
        }

        [Fact]
        public async void SettingUserDetailsMultiple()
        {
            CountlyUserDetails cud = Countly.UserDetails;
            for(int a = 0; a < 2; a++)
            {
                for(int b = 0; b < 2; b++)
                {
                    TestHelper.PopulateCountlyUserDetails(cud, a, b);
                }
            }            
            
            bool res = await Countly.Instance.Upload();

            Assert.True(res);
        }

        [Fact]
        public void ReadWriteDummyImage()
        {
            MemoryStream ms = TestHelper.MemoryStreamRead(TestHelper.testDataLocation + "\\sample_image.png");
            TestHelper.MemoryStreamWrite("out.png", ms);
            ms.Close();
        }

        [Fact]
        public async void UploadingUserPicture()
        {
            CountlyUserDetails cud = Countly.UserDetails;
            cud.Name = "PinocioWithARealImage";

            var res = await Countly.Instance.Upload();

            //todo, test is succeeding, but not really uploading
            MemoryStream ms = TestHelper.MemoryStreamRead(TestHelper.testDataLocation + "\\sample_image.png");

            res = await Countly.Instance.UploadUserPicture(ms);
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

            res = await Countly.RecordEvent("Some event3", 4234, 1236.12, 234.5, null);
            Assert.True(res);

            Segmentation segm = new Segmentation();
            segm.Add("oneKey", "SomeValue");
            segm.Add("anotherKey", "SomeOtherValue");

            res = await Countly.RecordEvent("Some event4", 123, 456, segm);

            res = await Countly.RecordEvent("Some event5", 123, 456, 42.54, segm);
            Assert.True(res);
        }
    }
}
