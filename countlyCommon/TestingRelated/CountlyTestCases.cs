using CountlySDK;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace TestProject_common
{
    public class CountlyTestCases : IDisposable
    {
        /// <summary>
        /// Used for testing legacy session control
        /// </summary>
        public CountlyTestCases()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Halt();
            TestHelper.CleanDataFiles();
            CountlyConfig cc = TestHelper.CreateConfig();
            Countly.Instance.Init(cc).Wait();
            Countly.Instance.SessionBegin().Wait();
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {
            TestHelper.ValidateDataPointUpload().Wait();
            Countly.Instance.SessionEnd().Wait();
            TestHelper.ValidateDataPointUpload().Wait();
        }

        [Fact]
        public async void BasicDeviceID()
        {
            String dId = await Countly.Instance.DeviceData.GetDeviceId();
            Assert.NotNull(dId);
            Assert.NotEqual(0, dId.Length);
        }

        [Fact]
        public async void ChengeDeviceIDNoMerge()
        {
            bool res;
            res = await Countly.RecordEvent("Some event user1", 123);
            Assert.True(res);
            String dId = await Countly.Instance.DeviceData.GetDeviceId();
            Assert.NotNull(dId);
            Assert.NotEqual(0, dId.Length);

            String newId = "qweqwe";

            await Countly.Instance.ChangeDeviceId(newId, false);
            String dId2 = await Countly.Instance.DeviceData.GetDeviceId();
            Assert.Equal(newId, dId2);
            res = await Countly.RecordEvent("Some event user2", 123);
            Assert.True(res);
        }

        [Fact]
        public async void ChengeDeviceIDNoMergeMultiple()
        {
            bool res;

            String dId = await Countly.Instance.DeviceData.GetDeviceId();
            Assert.NotNull(dId);
            Assert.NotEqual(0, dId.Length);

            String newId = "qweqwe";

            await Countly.Instance.ChangeDeviceId(newId + "1", false);
            res = await Countly.RecordEvent("Some event user2", 123);
            await Countly.Instance.ChangeDeviceId(newId + "2", false);
            res = await Countly.RecordEvent("Some event user2", 123);
            await Countly.Instance.ChangeDeviceId(newId + "3", false);
            res = await Countly.RecordEvent("Some event user2", 123);
            await Countly.Instance.ChangeDeviceId(newId + "4", false);

            res = await Countly.RecordEvent("Some event user2", 123);
            Assert.True(res);
        }

        [Fact]
        public async void ChengeDeviceIDWithMerge()
        {
            bool res;
            res = await Countly.RecordEvent("Some event user1", 123);
            Assert.True(res);
            String dId = await Countly.Instance.DeviceData.GetDeviceId();
            Assert.NotNull(dId);
            Assert.NotEqual(0, dId.Length);

            String newId = "qweqwe";
            Api.Instance.DeviceMergeWaitTime = 500;
            await Countly.Instance.ChangeDeviceId(newId, true);
            String dId2 = await Countly.Instance.DeviceData.GetDeviceId();
            Assert.Equal(newId, dId2);
            res = await Countly.RecordEvent("Some event same user", 123);
            Assert.True(res);
        }

        [Fact]
        public async void ChengeDeviceIDWithMergeMultiple()
        {
            bool res;

            String dId = await Countly.Instance.DeviceData.GetDeviceId();
            Assert.NotNull(dId);
            Assert.NotEqual(0, dId.Length);

            String newId = "qweqwe";

            Api.Instance.DeviceMergeWaitTime = 500;

            await Countly.Instance.ChangeDeviceId(newId + "1", true);
            res = await Countly.RecordEvent("Some event user2", 123);
            await Countly.Instance.ChangeDeviceId(newId + "2", true);
            res = await Countly.RecordEvent("Some event user2", 123);
            await Countly.Instance.ChangeDeviceId(newId + "3", true);
            res = await Countly.RecordEvent("Some event user2", 123);
            await Countly.Instance.ChangeDeviceId(newId + "4", true);

            res = await Countly.RecordEvent("Some event user2", 123);
            Assert.True(res);
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
            for (int a = 0; a < 2; a++)
            {
                for (int b = 0; b < 2; b++)
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
        public async void SetLocationSimple()
        {
            String loc = "63.445821, 10.898868";
            bool res = await Countly.Instance.SetLocation(loc);
        }

        [Fact]
        public async void DisableLocationSimple()
        {
            bool res = await Countly.Instance.DisableLocation();
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
        
        [Fact]
        public async void VeryLargeEventName()
        {
            bool res;
            int steps = 10;
            String[] str = TestHelper.CreateLargeStrings(steps, 1000);

            for(int a = 0; a < steps; a++)
            {
                String pre = "" + (a + 1) + "/" + steps + " | ";
                res = await Countly.RecordEvent(pre + str[a]);
                Thread.Sleep(5);
                Assert.True(res);                
            }
        }

        [Fact]
        public async void VeryLargeException()
        {
            bool res;
            int steps = 10;
            String[] str = TestHelper.CreateLargeStrings(steps, 1000);

            for (int a = 0; a < steps; a++)
            {
                String pre = "" + (a + 1) + "/" + steps + " | ";
                String msg = pre + str[a];

                res = await Countly.RecordException("" + (a + 1) + "SASAS", msg);
                Thread.Sleep(200);
                Assert.True(res);                
            }
        }
    }
}
