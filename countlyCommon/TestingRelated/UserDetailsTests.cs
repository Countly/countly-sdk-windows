using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CountlySDK;
using CountlySDK.Entities;
using Xunit;
using static CountlySDK.CountlyCommon.CountlyBase;

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

        [Fact]
        /// <summary>
        /// "Increment" sends "$inc" with 1 for the key
        /// </summary>
        public void Modifier_Increment()
        {
            ValidateSingleModifierRequest(() => Countly.UserDetails.Increment("visits"), TestHelper.Dict("visits", TestHelper.Dict("$inc", 1)));
        }

        [Fact]
        /// <summary>
        /// "IncrementBy" sends "$inc" with the given value
        /// </summary>
        public void Modifier_IncrementBy()
        {
            ValidateSingleModifierRequest(() => Countly.UserDetails.IncrementBy("score", 5), TestHelper.Dict("score", TestHelper.Dict("$inc", 5)));
        }

        [Fact]
        /// <summary>
        /// "Multiply" sends "$mul" with the given value
        /// </summary>
        public void Modifier_Multiply()
        {
            ValidateSingleModifierRequest(() => Countly.UserDetails.Multiply("price", 3), TestHelper.Dict("price", TestHelper.Dict("$mul", 3)));
        }

        [Fact]
        /// <summary>
        /// "SaveMax" sends "$max" with the given value
        /// </summary>
        public void Modifier_SaveMax()
        {
            ValidateSingleModifierRequest(() => Countly.UserDetails.SaveMax("best", 10), TestHelper.Dict("best", TestHelper.Dict("$max", 10)));
        }

        [Fact]
        /// <summary>
        /// "SaveMin" sends "$min" with the given value
        /// </summary>
        public void Modifier_SaveMin()
        {
            ValidateSingleModifierRequest(() => Countly.UserDetails.SaveMin("fastest", 2), TestHelper.Dict("fastest", TestHelper.Dict("$min", 2)));
        }

        [Fact]
        /// <summary>
        /// "SetOnce" sends "$setOnce" with the given value
        /// </summary>
        public void Modifier_SetOnce()
        {
            ValidateSingleModifierRequest(() => Countly.UserDetails.SetOnce("first_seen", "2026-10-26"), TestHelper.Dict("first_seen", TestHelper.Dict("$setOnce", "2026-10-26")));
        }

        [Fact]
        /// <summary>
        /// "Push" sends "$push" with the given value
        /// </summary>
        public void Modifier_Push()
        {
            ValidateSingleModifierRequest(() => Countly.UserDetails.Push("tags", "news"), TestHelper.Dict("tags", TestHelper.Dict("$push", "news")));
        }

        [Fact]
        /// <summary>
        /// "PushUnique" sends "$addToSet" with the given value
        /// </summary>
        public void Modifier_PushUnique()
        {
            ValidateSingleModifierRequest(() => Countly.UserDetails.PushUnique("tags", "news"), TestHelper.Dict("tags", TestHelper.Dict("$addToSet", "news")));
        }

        [Fact]
        /// <summary>
        /// "Pull" sends "$pull" with the given value
        /// </summary>
        public void Modifier_Pull()
        {
            ValidateSingleModifierRequest(() => Countly.UserDetails.Pull("tags", "news"), TestHelper.Dict("tags", TestHelper.Dict("$pull", "news")));
        }

        [Fact]
        /// <summary>
        /// Array modifiers on one key accumulate into that key's pending object; a repeated one becomes an array
        /// </summary>
        public void Modifiers_ArrayOpsOnOneKey_Accumulate()
        {
            ValidateSingleModifierRequest(() => {
                Countly.UserDetails.Push("tags", "a");
                Countly.UserDetails.Push("tags", "b");
                Countly.UserDetails.Pull("tags", "c");
                Countly.UserDetails.PushUnique("tags", "d");
            }, TestHelper.Dict("tags", TestHelper.Dict("$push", new[] { "a", "b" }, "$pull", "c", "$addToSet", "d")));
        }

        [Fact]
        /// <summary>
        /// A numeric modifier or "SetOnce" replaces the key's pending object, and a later array modifier joins it
        /// </summary>
        public void Modifiers_NumericOpsOnOneKey_ReplacePending()
        {
            ValidateSingleModifierRequest(() => {
                Countly.UserDetails.Push("a", "x");
                Countly.UserDetails.Increment("a");
                Countly.UserDetails.Multiply("a", 3);
                Countly.UserDetails.SetOnce("b", "first");
                Countly.UserDetails.SaveMax("b", 4);
                Countly.UserDetails.SaveMin("c", 9);
                Countly.UserDetails.SetOnce("c", "first");
                Countly.UserDetails.IncrementBy("d", 2);
                Countly.UserDetails.Push("d", "y");
            }, TestHelper.Dict(
                "a", TestHelper.Dict("$mul", 3),
                "b", TestHelper.Dict("$max", 4),
                "c", TestHelper.Dict("$setOnce", "first"),
                "d", TestHelper.Dict("$inc", 2, "$push", "y")));
        }

        [Fact]
        /// <summary>
        /// Modifiers are sent once: the next user details request no longer carries them
        /// </summary>
        public void Modifiers_ClearedAfterSend()
        {
            MockHttpServer server = InitWithMockServer();

            Countly.UserDetails.Increment("visits");
            SaveAndUpload(server);
            Countly.UserDetails.Name = "John";
            List<MockHttpServer.RequestInfo> requests = SaveAndUpload(server);

            Assert.Equal(2, requests.Count);
            ValidateCustomRequest(requests[0], TestHelper.Dict("visits", TestHelper.Dict("$inc", 1)));
            TestHelper.ValidateRequest(requests[1].Params, TestHelper.Dict("user_details", TestHelper.Json("name", "John")));
            server.Dispose();
        }

        [Fact]
        /// <summary>
        /// Custom string properties stay as they were, date-like ones included, and are sent in the same "custom" object as the modifiers
        /// </summary>
        public void Modifiers_SentBesideCustomStringProperties()
        {
            ValidateSingleModifierRequest(() => {
                Countly.UserDetails.Custom.Add("hair", "black");
                Countly.UserDetails.Custom.Add("signup", "2026-10-26T10:00:00.000+05:30");
                Countly.UserDetails.Increment("visits");
                Assert.Equal(TestHelper.DictS("hair", "black", "signup", "2026-10-26T10:00:00.000+05:30"), Countly.UserDetails.Custom.ToDictionary());
            }, TestHelper.Dict("hair", "black", "signup", "2026-10-26T10:00:00.000+05:30", "visits", TestHelper.Dict("$inc", 1)));
        }

        [Fact]
        /// <summary>
        /// A modifier on the key of a custom string property replaces that property in the request, as in Android
        /// </summary>
        public void Modifier_OverridesCustomPropertyWithSameKey()
        {
            ValidateSingleModifierRequest(() => {
                Countly.UserDetails.Custom.Add("level", "5");
                Countly.UserDetails.IncrementBy("level", 2);
            }, TestHelper.Dict("level", TestHelper.Dict("$inc", 2)));
        }

        [Fact]
        /// <summary>
        /// A modifier recorded while a save clears the user details stays pending and goes out with the next save
        /// </summary>
        public void Modifiers_PendingAfterClear_SentWithNextSave()
        {
            ValidateSingleModifierRequest(() => {
                Countly.UserDetails.Increment("visits");
                Countly.UserDetails.Clear();
            }, TestHelper.Dict("visits", TestHelper.Dict("$inc", 1)));
        }

        [Fact]
        /// <summary>
        /// Pending modifiers live in memory only: the stored user details snapshot never contains them
        /// </summary>
        public void Modifiers_NotInStoredSnapshot()
        {
            MockHttpServer server = InitWithMockServer();

            Countly.UserDetails.Name = "John";
            Countly.UserDetails.Increment("visits");
            string stored = TestHelper.DCSSerialize(Countly.UserDetails);

            Assert.Contains("John", stored);
            Assert.DoesNotContain("visits", stored);
            Assert.DoesNotContain("$inc", stored);
            server.Dispose();
        }

        [Fact]
        /// <summary>
        /// Keys are truncated to "MaxKeyLength" and string values to "MaxValueSize", numbers are kept;
        /// keys that truncate to the same key share one pending object
        /// </summary>
        public void Modifiers_KeyAndValueLimits()
        {
            ValidateSingleModifierRequest(cc => {
                cc.MaxKeyLength = 5;
                cc.MaxValueSize = 3;
            }, () => {
                Countly.UserDetails.Push("colors_1", "yellow");
                Countly.UserDetails.Push("colors_2", "purple");
                Countly.UserDetails.SetOnce("nickname", "Johnny");
                Countly.UserDetails.IncrementBy("counter", -12345);
            }, TestHelper.Dict(
                "color", TestHelper.Dict("$push", new[] { "yel", "pur" }),
                "nickn", TestHelper.Dict("$setOnce", "Joh"),
                "count", TestHelper.Dict("$inc", -12345)));
        }

        [Fact]
        /// <summary>
        /// A null or empty key, or a null value, is ignored without touching valid modifiers; an empty string value is kept
        /// </summary>
        public void Modifiers_InvalidInputIgnored()
        {
            ValidateSingleModifierRequest(() => {
                Countly.UserDetails.Increment(null);
                Countly.UserDetails.IncrementBy("", 2);
                Countly.UserDetails.SetOnce("nickname", null);
                Countly.UserDetails.PushUnique(null, "x");
                Countly.UserDetails.Pull("tags", null);
                Countly.UserDetails.Push("tags", "");
            }, TestHelper.Dict("tags", TestHelper.Dict("$push", "")));
        }

        [Fact]
        /// <summary>
        /// Modifiers recorded before "Init" are ignored without crashing
        /// </summary>
        public void Modifiers_BeforeInit_Ignored()
        {
            Countly.UserDetails.Increment("early");
            Countly.UserDetails.Push("early_tags", "a");
            MockHttpServer server = InitWithMockServer();

            Countly.UserDetails.Increment("late");
            List<MockHttpServer.RequestInfo> requests = SaveAndUpload(server);

            Assert.Single(requests);
            ValidateCustomRequest(requests[0], TestHelper.Dict("late", TestHelper.Dict("$inc", 1)));
            server.Dispose();
        }

        [Fact]
        /// <summary>
        /// When consent is required, modifiers are ignored until "Users" consent is given
        /// </summary>
        public void Modifiers_RequireUsersConsent()
        {
            ValidateSingleModifierRequest(cc => cc.consentRequired = true, () => {
                Countly.UserDetails.Increment("denied");
                Countly.Instance.SetConsent(new Dictionary<ConsentFeatures, bool> { { ConsentFeatures.Users, true } }).Wait();
                Countly.UserDetails.Increment("granted");
            }, TestHelper.Dict("granted", TestHelper.Dict("$inc", 1)));
        }

        [Fact]
        /// <summary>
        /// Removing "Users" consent drops the modifiers that were waiting for a save
        /// </summary>
        public void Modifiers_UsersConsentRemoved_PendingDropped()
        {
            ValidateSingleModifierRequest(cc => {
                cc.consentRequired = true;
                cc.givenConsent = new Dictionary<ConsentFeatures, bool> { { ConsentFeatures.Users, true } };
            }, () => {
                Countly.UserDetails.Increment("before_removal");
                Countly.Instance.SetConsent(new Dictionary<ConsentFeatures, bool> { { ConsentFeatures.Users, false } }).Wait();
                Countly.Instance.SetConsent(new Dictionary<ConsentFeatures, bool> { { ConsentFeatures.Users, true } }).Wait();
                Countly.UserDetails.Increment("after_consent");
            }, TestHelper.Dict("after_consent", TestHelper.Dict("$inc", 1)));
        }

        [Fact]
        /// <summary>
        /// A save drops the pending modifiers once "Users" consent is no longer given,
        /// even before the consent removal has been fully processed
        /// </summary>
        public void Modifiers_SaveWithoutUsersConsent_PendingDropped()
        {
            ValidateSingleModifierRequest(cc => {
                cc.consentRequired = true;
                cc.givenConsent = new Dictionary<ConsentFeatures, bool> { { ConsentFeatures.Users, true } };
            }, () => {
                Countly.UserDetails.Increment("before_removal");
                Countly.Instance.givenConsent[ConsentFeatures.Users] = false;
                Countly.UserDetails.Save();
                WaitUntil(() => !Countly.UserDetails.isChanged);
                Countly.Instance.givenConsent[ConsentFeatures.Users] = true;
                Countly.UserDetails.Increment("after_consent");
            }, TestHelper.Dict("after_consent", TestHelper.Dict("$inc", 1)));
        }

        [Fact]
        /// <summary>
        /// A device ID change without merge first queues the pending modifiers under the old device ID,
        /// so they stay with the user they were recorded for
        /// </summary>
        public void Modifiers_DeviceIdChangeWithoutMerge_SentUnderOldId()
        {
            ValidateSingleModifierRequest(() => {
                Countly.UserDetails.Increment("visits");
                Countly.Instance.ChangeDeviceId("new_device_id").Wait();
            }, TestHelper.Dict("visits", TestHelper.Dict("$inc", 1)));
        }

        [Fact]
        /// <summary>
        /// Keys blocked by the user property filter of the SDK Behavior Settings are ignored,
        /// matched after key truncation as custom properties are
        /// </summary>
        public void Modifiers_UserPropertyFilter()
        {
            ValidateSingleModifierRequest(cc => {
                cc.MaxKeyLength = 7;
                cc.SetSDKBehaviorSettings("{\"c\":{\"upb\":[\"blocked\"]}}");
                cc.DisableSDKBehaviorSettingsUpdates();
            }, () => {
                Countly.UserDetails.Increment("blocked");
                Countly.UserDetails.SaveMax("blocked_after_truncation", 3);
                Countly.UserDetails.Push("allowed", "a");
            }, TestHelper.Dict("allowed", TestHelper.Dict("$push", "a")));
        }

        [Fact]
        /// <summary>
        /// The user property cache limit of the SDK Behavior Settings caps how many keys hold pending modifiers,
        /// while a pending key still takes more modifiers
        /// </summary>
        public void Modifiers_UserPropertyCacheLimit()
        {
            ValidateSingleModifierRequest(cc => {
                cc.SetSDKBehaviorSettings("{\"c\":{\"upcl\":2}}");
                cc.DisableSDKBehaviorSettingsUpdates();
            }, () => {
                Countly.UserDetails.Increment("k1");
                Countly.UserDetails.Increment("k2");
                Countly.UserDetails.Increment("k3");
                Countly.UserDetails.Push("k1", "a");
            }, TestHelper.Dict(
                "k1", TestHelper.Dict("$inc", 1, "$push", "a"),
                "k2", TestHelper.Dict("$inc", 1)));
        }

        [Fact]
        /// <summary>
        /// When the server answers with an error, the modifier request stays queued and is later sent unchanged
        /// </summary>
        public void Modifiers_ServerError_RequestKeptForRetry()
        {
            bool serverFails = true;
            MockHttpServer server = InitWithMockServer(null, body => serverFails ? "{\"error\":\"unavailable\"}" : null);

            Countly.UserDetails.Increment("visits");
            SaveAndUpload(server);

            Assert.Single(Countly.Instance.StoredRequests);
            serverFails = false;
            Countly.Instance.Upload().Wait();
            List<MockHttpServer.RequestInfo> requests = UserDetailsRequests(server);

            Assert.Empty(Countly.Instance.StoredRequests);
            Assert.Equal(2, requests.Count);
            ValidateCustomRequest(requests[0], TestHelper.Dict("visits", TestHelper.Dict("$inc", 1)));
            ValidateCustomRequest(requests[1], TestHelper.Dict("visits", TestHelper.Dict("$inc", 1)));
            server.Dispose();
        }

        [Fact]
        /// <summary>
        /// With "DisableAutoSendUserDetails" the user details snapshot is re-sent whole on every save,
        /// so modifiers get their own queued request and are sent only once
        /// </summary>
        public void Modifiers_AutoSendDisabled_SentOnce()
        {
            MockHttpServer server = InitWithMockServer(cc => cc.DisableAutoSendUserDetails());

            Countly.UserDetails.Increment("visits");
            Countly.UserDetails.Save();
            WaitUntil(() => !Countly.UserDetails.isChanged && Countly.Instance.StoredRequests.Count == 0);
            Countly.UserDetails.Name = "John";
            Countly.UserDetails.Save();
            WaitUntil(() => !Countly.UserDetails.isChanged && Countly.Instance.StoredRequests.Count == 0);

            List<MockHttpServer.RequestInfo> requests = UserDetailsRequests(server);
            List<MockHttpServer.RequestInfo> modifierRequests = requests.Where(r => r.Params["user_details"].Contains("$inc")).ToList();
            Assert.Single(modifierRequests);
            ValidateCustomRequest(modifierRequests[0], TestHelper.Dict("visits", TestHelper.Dict("$inc", 1)));
            TestHelper.ValidateRequest(requests.Last().Params, TestHelper.Dict("user_details", TestHelper.Json("name", "John")));
            server.Dispose();
        }

        [Fact]
        /// <summary>
        /// Modifiers recorded from several threads at once are all kept
        /// </summary>
        public void Modifiers_ConcurrentCalls_AllKept()
        {
            ValidateSingleModifierRequest(() => {
                Thread[] threads = new Thread[4];
                for (int i = 0; i < threads.Length; i++) {
                    threads[i] = new Thread(() => {
                        for (int j = 0; j < 50; j++) {
                            Countly.UserDetails.Push("tags", "v");
                        }
                    });
                    threads[i].Start();
                }
                foreach (Thread thread in threads) {
                    thread.Join();
                }
            }, TestHelper.Dict("tags", TestHelper.Dict("$push", Enumerable.Repeat("v", 200).ToArray())));
        }

        /// <summary>
        /// Initializes the SDK against a new mock server, after letting the test adjust the configuration
        /// </summary>
        private static MockHttpServer InitWithMockServer(Action<CountlyConfig> configure = null, Func<string, string> customResponse = null)
        {
            MockHttpServer server = new MockHttpServer(customResponse);
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            configure?.Invoke(cc);
            Countly.Instance.Init(cc).Wait();
            return server;
        }

        /// <summary>
        /// Saves the user details, uploads the request it queues and returns the user details requests the server received
        /// </summary>
        private static List<MockHttpServer.RequestInfo> SaveAndUpload(MockHttpServer server)
        {
            Countly.UserDetails.Save();
            WaitUntil(() => Countly.Instance.StoredRequests.Count > 0);
            Countly.Instance.Upload().Wait();
            return UserDetailsRequests(server);
        }

        /// <summary>
        /// Returns the requests the server received that carry user details
        /// </summary>
        private static List<MockHttpServer.RequestInfo> UserDetailsRequests(MockHttpServer server)
        {
            return TestHelper.NonServerConfigRequests(server).Where(r => r.Params.ContainsKey("user_details")).ToList();
        }

        /// <summary>
        /// Polls the condition until it holds, failing after five seconds, since user details are saved by an async void handler
        /// </summary>
        private static void WaitUntil(Func<bool> condition)
        {
            DateTime deadline = DateTime.Now.AddSeconds(5);
            while (!condition()) {
                Assert.True(DateTime.Now < deadline, "Timed out waiting for the user details request");
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Records modifiers on a freshly initialized SDK, saves, and asserts the single user details request the server receives
        /// </summary>
        private static void ValidateSingleModifierRequest(Action modify, IDictionary<string, object> expectedCustom)
        {
            ValidateSingleModifierRequest(null, modify, expectedCustom);
        }

        /// <summary>
        /// Records modifiers on an SDK initialized with the adjusted configuration, saves, and asserts the single user details request the server receives
        /// </summary>
        private static void ValidateSingleModifierRequest(Action<CountlyConfig> configure, Action modify, IDictionary<string, object> expectedCustom)
        {
            MockHttpServer server = InitWithMockServer(configure);

            modify();
            List<MockHttpServer.RequestInfo> requests = SaveAndUpload(server);

            Assert.Single(requests);
            ValidateCustomRequest(requests[0], expectedCustom);
            server.Dispose();
        }

        /// <summary>
        /// Asserts that the request's user details hold exactly the given "custom" object and nothing else
        /// </summary>
        private static void ValidateCustomRequest(MockHttpServer.RequestInfo request, IDictionary<string, object> expectedCustom)
        {
            TestHelper.ValidateRequest(request.Params, TestHelper.Dict("user_details", TestHelper.Json("custom", expectedCustom)));
        }
    }
}
