using System;
using System.Collections.Generic;
using CountlySDK;
using CountlySDK.Entities;
using Xunit;

namespace TestProject_common
{
    public class UserDetailsTests : IDisposable
    {
        /// <summary>
        /// Test setup
        /// </summary>
        public UserDetailsTests()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Instance.HaltInternal().Wait(); // synchronous teardown: avoid async-void Halt racing with the next Init
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
            var server = new MockHttpServer();
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

            var reqs = TestHelper.NonServerConfigRequests(server);
            Assert.Equal(6, reqs.Count);

            TestHelper.ValidateRequest(reqs[0].Params, TestHelper.Dict("user_details", TestHelper.Json("custom", TestHelper.Dict("Papa", "Black_1"))));
            TestHelper.ValidateRequest(reqs[1].Params, TestHelper.Dict("begin_session", "1", "metrics", TestHelper.GetSessionMetrics()));
            TestHelper.ValidateRequest(reqs[2].Params, TestHelper.Dict("user_details", TestHelper.Json("name", "John")));
            TestHelper.ValidateRequest(reqs[3].Params, TestHelper.Dict("session_duration", 2));
            TestHelper.ValidateRequest(reqs[4].Params, TestHelper.Dict("user_details", TestHelper.Json("email", "Doe@doe.com")));
            TestHelper.ValidateRequest(reqs[5].Params, TestHelper.Dict("end_session", "1", "session_duration", 2));

            server.Dispose();
        }

        [Fact]
        /// <summary>
        /// A user-details change notification with EMPTY details (nothing to send) must not leave
        /// 'isChanged' stuck true - upload waiters (e.g. ValidateDataPointUpload) would spin on it forever.
        /// </summary>
        public void EmptyUserDetails_SessionFlow_DoesNotStayChanged()
        {
            var server = new MockHttpServer();
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            Countly.Instance.Init(cc).Wait();

            // Arm the pending-change flag while the details themselves stay empty ("{}"),
            // mirroring what the lazy first load of UserDetails does on a fresh run.
            Countly.UserDetails._custom = new Dictionary<string, string>();
            Countly.Instance.SessionBegin().Wait();

            Assert.False(Countly.UserDetails.isChanged);
            server.Dispose();
        }

        [Fact]
        /// <summary>
        /// It validates that user property changes are not triggered with session calls when disabled
        /// </summary>
        public void SetUserDetails_SessionTriggers_Disable()
        {
            var server = new MockHttpServer();
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

            var reqs = TestHelper.NonServerConfigRequests(server);
            Assert.Equal(4, reqs.Count);

            TestHelper.ValidateRequest(reqs[0].Params, TestHelper.Dict("begin_session", "1", "metrics", TestHelper.GetSessionMetrics()));
            TestHelper.ValidateRequest(reqs[1].Params, TestHelper.Dict("session_duration", 2));
            TestHelper.ValidateRequest(reqs[2].Params, TestHelper.Dict("end_session", "1", "session_duration", 2));

            TestHelper.ValidateRequest(reqs[3].Params, TestHelper.Dict("user_details", TestHelper.Json("name", "John", "email", "Doe@doe.com", "custom", TestHelper.Dict("Papa", "Black_1"))));

            server.Dispose();
        }

        [Fact]
        /// <summary>
        /// This shows the default behavior before the auto send user properties changes
        /// </summary>
        public void SetUserDetails_SessionTriggers_Disable_ManualSaveDisabled()
        {
            var server = new MockHttpServer();
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

            var reqs = TestHelper.NonServerConfigRequests(server);
            Assert.Equal(6, reqs.Count);

            TestHelper.ValidateRequest(reqs[0].Params, TestHelper.Dict("user_details", TestHelper.Json("custom", TestHelper.Dict("Papa", "Black_1"))));
            TestHelper.ValidateRequest(reqs[1].Params, TestHelper.Dict("begin_session", "1", "metrics", TestHelper.GetSessionMetrics()));
            TestHelper.ValidateRequest(reqs[2].Params, TestHelper.Dict("user_details", TestHelper.Json("name", "John", "custom", TestHelper.Dict("Papa", "Black_1"))));
            TestHelper.ValidateRequest(reqs[3].Params, TestHelper.Dict("session_duration", 2));
            TestHelper.ValidateRequest(reqs[4].Params, TestHelper.Dict("user_details", TestHelper.Json("name", "John", "email", "Doe@doe.com", "custom", TestHelper.Dict("Papa", "Black_1"))));
            TestHelper.ValidateRequest(reqs[5].Params, TestHelper.Dict("end_session", "1", "session_duration", 2));

            server.Dispose();
        }


        [Fact]
        /// <summary>
        /// It validates that user property changes triggered with session and event calls
        /// </summary>
        public void SetUserDetails_SessionEventsTriggers()
        {
            var server = new MockHttpServer();
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

            var reqs = TestHelper.NonServerConfigRequests(server);
            Assert.Equal(7, reqs.Count);

            TestHelper.ValidateRequest(reqs[0].Params, TestHelper.Dict("user_details", TestHelper.Json("custom", TestHelper.Dict("Papa", "Black_1"))));
            TestHelper.ValidateRequest(reqs[1].Params, TestHelper.Dict("begin_session", "1", "metrics", TestHelper.GetSessionMetrics()));
            Assert.Contains("Test", reqs[2].Params["events"]);
            TestHelper.ValidateRequest(reqs[3].Params, TestHelper.Dict("user_details", TestHelper.Json("name", "John")));
            Assert.Contains("Test1", reqs[4].Params["events"]);
            TestHelper.ValidateRequest(reqs[5].Params, TestHelper.Dict("user_details", TestHelper.Json("email", "Doe@doe.com")));
            // session_duration here is wall-clock derived (~2.4s of sleeps), so it rounds to 2 or 3
            // depending on machine/CI speed. Validate its presence with a tolerant range instead of
            // an exact value to avoid timing flakiness.
            TestHelper.ValidateRequest(reqs[6].Params, TestHelper.Dict("end_session", "1", "session_duration", 3),
                new Dictionary<string, Action<string, object>> {
                    { "session_duration", (actual, _) => Assert.True(int.Parse(actual) >= 2 && int.Parse(actual) <= 4, "session_duration was " + actual) }
                });

            server.Dispose();
        }
    }
}
