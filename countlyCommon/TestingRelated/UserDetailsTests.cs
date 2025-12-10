using System;
using System.Collections.Generic;
using CountlySDK;
using CountlySDK.Entities;
using Xunit;
#if RUNNING_ON_40
using Xunit.Abstractions;
#endif

namespace TestProject_common
{
    public class UserDetailsTests : IDisposable
    {
#if RUNNING_ON_40
        private readonly ITestOutputHelper _output;
#endif

        /// <summary>
        /// Test setup
        /// </summary>
#if RUNNING_ON_40

        public UserDetailsTests(ITestOutputHelper output)
        {
            _output = output;
#else
        public UserDetailsTests()
        {
#endif
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
        /// It validate user detail segments limits.
        /// </summary>
        public void TestUserProfileFieldsLimits()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            cc.MaxValueSize = 3;
            cc.DisableManualUserDetailsSave().DisableAutoSendUserDetails();

            Countly.Instance.Init(cc).Wait();

            Countly.UserDetails.Name = "Full Name";
            Countly.UserDetails.Username = "username";
            Countly.UserDetails.Email = "useremail@email.com";
            Countly.UserDetails.Organization = "Organization";
            Countly.UserDetails.Phone = "222-222-222";
            Countly.UserDetails.Gender = "Male";
            Countly.UserDetails.Picture = "12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890" +
                    "12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890" +
                    "12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890" +
                    "12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890" +
                    "12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890" +
                    "12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890.png";

            Assert.Equal("Ful", Countly.UserDetails.Name);
            Assert.Equal("use", Countly.UserDetails.Username);
            Assert.Equal("use", Countly.UserDetails.Email);
            Assert.Equal("Org", Countly.UserDetails.Organization);
            Assert.Equal("222", Countly.UserDetails.Phone);
            Assert.Equal(4096, Countly.UserDetails.Picture.Length);
            Assert.Equal("Mal", Countly.UserDetails.Gender);
        }

        [Fact]
        /// <summary>
        /// It validate user detail segments limits.
        /// </summary>
        public void TestUserDetailSegmentLimits()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            cc.MaxKeyLength = 5;
            cc.MaxValueSize = 6;
            cc.DisableManualUserDetailsSave().DisableAutoSendUserDetails();
            Countly.Instance.Init(cc).Wait();

            Countly.UserDetails.Custom.Add("Hair", "Black_1");
            Countly.UserDetails.Custom.Add("Height", "5.9");

            Dictionary<string, string> custom = Countly.UserDetails.Custom.ToDictionary();

            Assert.Equal("Black_", custom["Hair"].ToString());
            Assert.Equal("5.9", custom["Heigh"].ToString());

        }

        [Fact]
        /// <summary>
        /// It validates that user property changes triggered with session calls
        /// </summary>
        public void SetUserDetails_SessionTriggers()
        {
#if RUNNING_ON_40
            var server = new MockHttpServer(_output);
#else
            var server = new MockHttpServer();
#endif
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;

            Countly.Instance.Init(cc).Wait();

            Countly.UserDetails.Custom.Add("Papa", "Black_1");
            Countly.Instance.SessionBegin().Wait();
            System.Threading.Thread.Sleep(2000);

            Countly.UserDetails.Name = "John";
            Countly.Instance.SessionUpdate(2).Wait();
            System.Threading.Thread.Sleep(2000);

            Countly.UserDetails.Email = "Doe@doe.com";
            Countly.Instance.SessionEnd().Wait();
            System.Threading.Thread.Sleep(2000);

            Assert.Equal(6, server.Requests.Count);

            TestHelper.ValidateRequest(server.Requests[0].Params, TestHelper.Dict("user_details", TestHelper.Json("custom", TestHelper.Dict("Papa", "Black_1"))));
            TestHelper.ValidateRequest(server.Requests[1].Params, TestHelper.Dict("begin_session", "1", "metrics", TestHelper.GetSessionMetrics()));
            TestHelper.ValidateRequest(server.Requests[2].Params, TestHelper.Dict("user_details", TestHelper.Json("name", "John")));
            TestHelper.ValidateRequest(server.Requests[3].Params, TestHelper.Dict("session_duration", 2));
            TestHelper.ValidateRequest(server.Requests[4].Params, TestHelper.Dict("user_details", TestHelper.Json("email", "Doe@doe.com")));
            TestHelper.ValidateRequest(server.Requests[5].Params, TestHelper.Dict("end_session", "1", "session_duration", 2));

            server.Dispose();
        }

        [Fact]
        /// <summary>
        /// It validates that user property changes are not triggered with session calls when disabled
        /// </summary>
        public void SetUserDetails_SessionTriggers_Disable()
        {
#if RUNNING_ON_40
            var server = new MockHttpServer(_output);
#else
            var server = new MockHttpServer();
#endif
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            cc.DisableAutoSendUserDetails();

            Countly.Instance.Init(cc).Wait();

            Countly.UserDetails.Custom.Add("Papa", "Black_1");
            Countly.Instance.SessionBegin().Wait();
            System.Threading.Thread.Sleep(2000);

            Countly.UserDetails.Name = "John";
            Countly.Instance.SessionUpdate(2).Wait();
            System.Threading.Thread.Sleep(2000);

            Countly.UserDetails.Email = "Doe@doe.com";
            Countly.Instance.SessionEnd().Wait();
            System.Threading.Thread.Sleep(2000);
            Countly.UserDetails.Save();
            System.Threading.Thread.Sleep(200);

            Assert.Equal(4, server.Requests.Count);

            TestHelper.ValidateRequest(server.Requests[0].Params, TestHelper.Dict("begin_session", "1", "metrics", TestHelper.GetSessionMetrics()));
            TestHelper.ValidateRequest(server.Requests[1].Params, TestHelper.Dict("session_duration", 2));
            TestHelper.ValidateRequest(server.Requests[2].Params, TestHelper.Dict("end_session", "1", "session_duration", 2));

            TestHelper.ValidateRequest(server.Requests[3].Params, TestHelper.Dict("user_details", TestHelper.Json("name", "John", "email", "Doe@doe.com", "custom", TestHelper.Dict("Papa", "Black_1"))));

            server.Dispose();
        }

        [Fact]
        /// <summary>
        /// This shows the default behavior before the auto send user properties changes
        /// </summary>
        public void SetUserDetails_SessionTriggers_Disable_ManualSaveDisabled()
        {
#if RUNNING_ON_40
            var server = new MockHttpServer(_output);
#else
            var server = new MockHttpServer();
#endif
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            cc.DisableAutoSendUserDetails();
            cc.DisableManualUserDetailsSave();

            Countly.Instance.Init(cc).Wait();

            Countly.UserDetails.Custom.Add("Papa", "Black_1");
            Countly.Instance.SessionBegin().Wait();
            System.Threading.Thread.Sleep(2000);

            Countly.UserDetails.Name = "John";
            Countly.Instance.SessionUpdate(2).Wait();
            System.Threading.Thread.Sleep(2000);

            Countly.UserDetails.Email = "Doe@doe.com";
            Countly.Instance.SessionEnd().Wait();
            System.Threading.Thread.Sleep(2000);
            Countly.UserDetails.Save(); // will not work

            Assert.Equal(6, server.Requests.Count);

            TestHelper.ValidateRequest(server.Requests[0].Params, TestHelper.Dict("user_details", TestHelper.Json("custom", TestHelper.Dict("Papa", "Black_1"))));
            TestHelper.ValidateRequest(server.Requests[1].Params, TestHelper.Dict("begin_session", "1", "metrics", TestHelper.GetSessionMetrics()));
            TestHelper.ValidateRequest(server.Requests[2].Params, TestHelper.Dict("user_details", TestHelper.Json("name", "John", "custom", TestHelper.Dict("Papa", "Black_1"))));
            TestHelper.ValidateRequest(server.Requests[3].Params, TestHelper.Dict("session_duration", 2));
            TestHelper.ValidateRequest(server.Requests[4].Params, TestHelper.Dict("user_details", TestHelper.Json("name", "John", "email", "Doe@doe.com", "custom", TestHelper.Dict("Papa", "Black_1"))));
            TestHelper.ValidateRequest(server.Requests[5].Params, TestHelper.Dict("end_session", "1", "session_duration", 2));

            server.Dispose();
        }


        [Fact]
        /// <summary>
        /// It validates that user property changes triggered with session and event calls
        /// </summary>
        public void SetUserDetails_SessionEventsTriggers()
        {
#if RUNNING_ON_40
            var server = new MockHttpServer(_output);
#else
            var server = new MockHttpServer();
#endif
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;

            Countly.Instance.Init(cc).Wait();

            Countly.UserDetails.Custom.Add("Papa", "Black_1");
            Countly.Instance.SessionBegin().Wait();
            System.Threading.Thread.Sleep(2000);

            Countly.RecordEvent("Test");
            Countly.UserDetails.Name = "John";
            System.Threading.Thread.Sleep(200);
            Countly.RecordEvent("Test1");
            System.Threading.Thread.Sleep(200);

            Countly.UserDetails.Email = "Doe@doe.com";
            Countly.Instance.SessionEnd().Wait();
            System.Threading.Thread.Sleep(2000);
            Countly.UserDetails.Save(); // will not work

            Assert.Equal(7, server.Requests.Count);

            TestHelper.ValidateRequest(server.Requests[0].Params, TestHelper.Dict("user_details", TestHelper.Json("custom", TestHelper.Dict("Papa", "Black_1"))));
            TestHelper.ValidateRequest(server.Requests[1].Params, TestHelper.Dict("begin_session", "1", "metrics", TestHelper.GetSessionMetrics()));
            Assert.Contains("Test", server.Requests[2].Params["events"]);
            TestHelper.ValidateRequest(server.Requests[3].Params, TestHelper.Dict("user_details", TestHelper.Json("name", "John")));
            Assert.Contains("Test1", server.Requests[4].Params["events"]);
            TestHelper.ValidateRequest(server.Requests[5].Params, TestHelper.Dict("user_details", TestHelper.Json("email", "Doe@doe.com")));
            TestHelper.ValidateRequest(server.Requests[6].Params, TestHelper.Dict("end_session", "1", "session_duration", 3));

            server.Dispose();
        }
    }
}
