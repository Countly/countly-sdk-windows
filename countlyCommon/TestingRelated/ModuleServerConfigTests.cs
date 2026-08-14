using System;
using System.Collections.Generic;
using CountlySDK;
using CountlySDK.CountlyCommon;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Xunit;

namespace TestProject_common
{
    public class ModuleServerConfigTests : IDisposable
    {
        public ModuleServerConfigTests()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Halt();
            TestHelper.CleanDataFiles();
        }

        public void Dispose()
        {
        }

        [Fact]
        /// <summary>Config setters chain fluently and store their values.</summary>
        public void ConfigApi_SettersChainAndStore()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            CountlyConfig returned = (CountlyConfig)cc
                .SetSDKBehaviorSettings("{\"c\":{\"tracking\":false}}")
                .DisableSDKBehaviorSettingsUpdates();

            Assert.Same(cc, returned);
            Assert.Equal("{\"c\":{\"tracking\":false}}", cc.providedSdkBehaviorSettings);
            Assert.True(cc.sdkBehaviorSettingsUpdatesDisabled);
        }

        [Fact]
        /// <summary>Developer-provided settings are applied to Configuration at init.</summary>
        public void ProvidedSettings_AppliedAtInit()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"lkl\":40,\"rqs\":50,\"log\":true,\"tracking\":false}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            Assert.Equal(40, Countly.Instance.Configuration.MaxKeyLength);
            Assert.Equal(50, Countly.Instance.Configuration.RequestQueueMaxSize);
            Assert.True(Countly.IsLoggingEnabled);
            Assert.False(Countly.Instance.moduleServerConfig.GetTrackingEnabled());
        }

        [Fact]
        /// <summary>Stored (server) settings take precedence over developer-provided settings.</summary>
        public void StoredSettings_OverrideProvided()
        {
            Storage.Instance.SaveToFile<ServerConfigEntity>(
                ModuleServerConfig.serverConfigFilename,
                new ServerConfigEntity { Json = "{\"v\":1,\"t\":1,\"c\":{\"lkl\":77}}" }).Wait();

            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"lkl\":40}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            Assert.Equal(77, Countly.Instance.Configuration.MaxKeyLength);
        }

        [Fact]
        /// <summary>Server 'cr' can turn consent enforcement ON.</summary>
        public void ConsentRequired_ServerCanEnable()
        {
            Storage.Instance.SaveToFile<ServerConfigEntity>(
                ModuleServerConfig.serverConfigFilename,
                new ServerConfigEntity { Json = "{\"v\":1,\"t\":1,\"c\":{\"cr\":true}}" }).Wait();

            CountlyConfig cc = TestHelper.GetConfig();
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            Assert.True(Countly.Instance.Configuration.consentRequired);
        }

        [Fact]
        /// <summary>Server 'cr=false' can NOT turn OFF developer-required consent (enable-only).</summary>
        public void ConsentRequired_ServerCannotDisable()
        {
            Storage.Instance.SaveToFile<ServerConfigEntity>(
                ModuleServerConfig.serverConfigFilename,
                new ServerConfigEntity { Json = "{\"v\":1,\"t\":1,\"c\":{\"cr\":false}}" }).Wait();

            CountlyConfig cc = TestHelper.GetConfig();
            cc.consentRequired = true;
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            Assert.True(Countly.Instance.Configuration.consentRequired);
        }

        [Fact]
        /// <summary>A valid SBS response is fetched via method=sc on /o/sdk, applied and persisted.</summary>
        public void FetchServerConfig_ValidResponse_AppliedAndRequestShaped()
        {
            MockHttpServer server = new MockHttpServer((body) =>
                body.Contains("method=sc") ? "{\"v\":1,\"t\":1742459739383,\"c\":{\"lkl\":33,\"networking\":false}}" : null);
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            Countly.Instance.Init(cc).Wait();

            bool scRequestSent = false;
            foreach (MockHttpServer.RequestInfo r in server.Requests) {
                if (r.Body.Contains("method=sc") && r.Path.Contains("/o/sdk")) { scRequestSent = true; }
            }
            Assert.True(scRequestSent);
            Assert.Equal(33, Countly.Instance.Configuration.MaxKeyLength);
            Assert.False(Countly.Instance.moduleServerConfig.GetNetworkingEnabled());
            server.Dispose();
        }

        [Fact]
        /// <summary>An invalid envelope (missing v/t/c) is rejected; defaults are kept.</summary>
        public void FetchServerConfig_InvalidResponse_KeepsDefaults()
        {
            MockHttpServer server = new MockHttpServer((body) =>
                body.Contains("method=sc") ? "{\"garbage\":true}" : null);
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            Countly.Instance.Init(cc).Wait();

            Assert.Equal(128, Countly.Instance.Configuration.MaxKeyLength);
            Assert.True(Countly.Instance.moduleServerConfig.GetNetworkingEnabled());
            server.Dispose();
        }

        [Fact]
        /// <summary>DisableSDKBehaviorSettingsUpdates skips the fetch yet still applies stored settings.</summary>
        public void DisableUpdates_SkipsFetch_ButAppliesStored()
        {
            Storage.Instance.SaveToFile<ServerConfigEntity>(
                ModuleServerConfig.serverConfigFilename,
                new ServerConfigEntity { Json = "{\"v\":1,\"t\":1,\"c\":{\"lkl\":55}}" }).Wait();
            int scCalls = 0;
            MockHttpServer server = new MockHttpServer((body) => {
                if (body.Contains("method=sc")) { scCalls++; return "{\"v\":1,\"t\":2,\"c\":{\"lkl\":99}}"; }
                return null;
            });
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            Assert.Equal(0, scCalls);
            Assert.Equal(55, Countly.Instance.Configuration.MaxKeyLength);
            server.Dispose();
        }

        [Fact]
        /// <summary>Successive fetches merge per-key rather than replacing the stored config.</summary>
        public void FetchServerConfig_MergesAcrossFetches()
        {
            string response = "{\"v\":1,\"t\":1,\"c\":{\"lkl\":33}}";
            MockHttpServer server = new MockHttpServer((body) => body.Contains("method=sc") ? response : null);
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            Countly.Instance.Init(cc).Wait();
            Assert.Equal(33, Countly.Instance.Configuration.MaxKeyLength);

            response = "{\"v\":1,\"t\":2,\"c\":{\"lvs\":44}}";
            Countly.Instance.moduleServerConfig.FetchServerConfig().Wait();
            Assert.Equal(33, Countly.Instance.Configuration.MaxKeyLength); // retained via merge
            Assert.Equal(44, Countly.Instance.Configuration.MaxValueSize); // newly added
            server.Dispose();
        }

        [Fact]
        /// <summary>tracking=false blocks event capture (event never enters the queue).</summary>
        public void Tracking_Disabled_BlocksEventCapture()
        {
            MockHttpServer server = new MockHttpServer((body) =>
                body.Contains("method=sc") ? "{\"v\":1,\"t\":1,\"c\":{\"tracking\":false}}" : null);
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            Countly.Instance.Init(cc).Wait();
            Assert.False(Countly.Instance.moduleServerConfig.GetTrackingEnabled());

            Countly.RecordEvent("test_event", 1, null, null).Wait();
            Assert.Empty(Countly.Instance.Events);
            server.Dispose();
        }

        [Fact]
        /// <summary>networking=false stops sending but data stays queued locally.</summary>
        public void Networking_Disabled_BlocksUpload_ButQueues()
        {
            MockHttpServer server = new MockHttpServer((body) =>
                body.Contains("method=sc") ? "{\"v\":1,\"t\":1,\"c\":{\"networking\":false}}" : null);
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            Countly.Instance.Init(cc).Wait();
            Assert.False(Countly.Instance.moduleServerConfig.GetNetworkingEnabled());

            Countly.RecordEvent("e", 1, null, null).Wait();
            Countly.Instance.Upload().Wait();
            Assert.NotEmpty(Countly.Instance.Events); // captured but not uploaded
            server.Dispose();
        }

        [Fact]
        /// <summary>Feature gates default to enabled (only 'ecz' defaults to disabled).</summary>
        public void FeatureGates_Defaults()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            ModuleServerConfig sc = Countly.Instance.moduleServerConfig;
            Assert.True(sc.GetSessionTrackingEnabled());
            Assert.True(sc.GetViewTrackingEnabled());
            Assert.True(sc.GetCustomEventTrackingEnabled());
            Assert.True(sc.GetCrashReportingEnabled());
            Assert.True(sc.GetLocationTrackingEnabled());
            Assert.True(sc.GetRefreshContentZoneEnabled());
            Assert.False(sc.GetEnterContentZoneEnabled());
        }

        [Fact]
        /// <summary>st=false blocks session begin/update/end requests.</summary>
        public void SessionTracking_Disabled_BlocksSessionRequests()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"st\":false}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();
            Countly.Instance.StoredRequests.Clear();

            Countly.Instance.SessionBegin().Wait();
            Countly.Instance.SessionUpdate(30).Wait();
            Countly.Instance.SessionEnd().Wait();

            Assert.Empty(Countly.Instance.StoredRequests);
        }

        [Fact]
        /// <summary>cet=false blocks custom events but reserved events (views) still record.</summary>
        public void CustomEventTracking_Disabled_BlocksCustomEvents_AllowsViews()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"cet\":false}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            Countly.RecordEvent("custom_event", 1, null, null).Wait();
            Assert.Empty(Countly.Instance.Events);

            Countly.Instance.RecordView("home").Wait();
            Assert.Single(Countly.Instance.Events);
            Assert.Equal("[CLY]_view", Countly.Instance.Events[0].Key);
        }

        [Fact]
        /// <summary>vt=false blocks view recording; custom events still record.</summary>
        public void ViewTracking_Disabled_BlocksViews_AllowsCustomEvents()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"vt\":false}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            bool viewResult = Countly.Instance.RecordView("home").Result;
            Assert.False(viewResult);
            Assert.Empty(Countly.Instance.Events);

            Countly.RecordEvent("custom_event", 1, null, null).Wait();
            Assert.Single(Countly.Instance.Events);
            Assert.Equal("custom_event", Countly.Instance.Events[0].Key);
        }

        [Fact]
        /// <summary>crt=false blocks crash recording.</summary>
        public void CrashReporting_Disabled_BlocksExceptions()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"crt\":false}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            Countly.RecordException("some error").Wait();
            Assert.Empty(Countly.Instance.Exceptions);
        }

        [Fact]
        /// <summary>lt=false ignores location calls and blanks the session location.</summary>
        public void LocationTracking_Disabled_IgnoresLocationCalls()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"lt\":false}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();
            Countly.Instance.StoredRequests.Clear();

            Countly.Instance.SetLocation("1.1,2.2", "1.1.1.1", "US", "Ankara").Wait();
            Countly.Instance.DisableLocation().Wait();
            Assert.Empty(Countly.Instance.StoredRequests);

            // with location tracking off, begin_session must carry an empty location param
            Countly.Instance.SessionBegin().Wait();
            Assert.Single(Countly.Instance.StoredRequests);
            StoredRequest sr = Countly.Instance.StoredRequests.Peek();
            Assert.Contains("begin_session", sr.Request);
            Assert.Contains("location=", sr.Request);
            Assert.DoesNotContain("1.1%2C2.2", sr.Request);
        }

        [Fact]
        /// <summary>Content keys apply: ecz/rcz to the provider, czi onto the configuration.</summary>
        public void ContentKeys_Applied()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"ecz\":true,\"rcz\":false,\"czi\":20}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            Assert.True(Countly.Instance.moduleServerConfig.GetEnterContentZoneEnabled());
            Assert.False(Countly.Instance.moduleServerConfig.GetRefreshContentZoneEnabled());
            Assert.Equal(20, Countly.Instance.Configuration.ContentZoneTimerInterval);
        }

        [Fact]
        /// <summary>Invalid feature-gate values are dropped: wrong types, and czi below 16.</summary>
        public void Sanitize_DropsInvalidFeatureGateValues()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"st\":\"nope\",\"vt\":1,\"czi\":10}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            Assert.True(Countly.Instance.moduleServerConfig.GetSessionTrackingEnabled());
            Assert.True(Countly.Instance.moduleServerConfig.GetViewTrackingEnabled());
            Assert.Equal(30, Countly.Instance.Configuration.ContentZoneTimerInterval);
        }

        private static List<string> SegKeys(CountlyEvent e)
        {
            List<string> keys = new List<string>();
            foreach (SegmentationItem si in e.Segmentation.segmentation) { keys.Add(si.Key); }
            return keys;
        }

        [Fact]
        /// <summary>eb blocks the listed custom events, others still record.</summary>
        public void EventFilter_Blacklist_BlocksListedEvents()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"eb\":[\"bad_event\"]}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            Countly.RecordEvent("bad_event", 1, null, null).Wait();
            Assert.Empty(Countly.Instance.Events);

            Countly.RecordEvent("good_event", 1, null, null).Wait();
            Assert.Single(Countly.Instance.Events);
            Assert.Equal("good_event", Countly.Instance.Events[0].Key);
        }

        [Fact]
        /// <summary>ew allows only the listed custom events; reserved events are unaffected.</summary>
        public void EventFilter_Whitelist_AllowsOnlyListed_ReservedUnaffected()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"ew\":[\"good_event\"]}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            Countly.RecordEvent("other_event", 1, null, null).Wait();
            Assert.Empty(Countly.Instance.Events);

            Countly.RecordEvent("good_event", 1, null, null).Wait();
            Countly.Instance.RecordView("home").Wait();
            Assert.Equal(2, Countly.Instance.Events.Count); // whitelisted custom event + reserved view event
        }

        [Fact]
        /// <summary>sb strips the listed segmentation keys from custom events.</summary>
        public void SegmentationBlacklist_StripsKeys()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"sb\":[\"secret\"]}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            Segmentation seg = new Segmentation();
            seg.Add("secret", "1");
            seg.Add("keep", "2");
            Countly.RecordEvent("e", 1, seg).Wait();

            Assert.Single(Countly.Instance.Events);
            List<string> keys = SegKeys(Countly.Instance.Events[0]);
            Assert.DoesNotContain("secret", keys);
            Assert.Contains("keep", keys);
        }

        [Fact]
        /// <summary>sw keeps only the listed segmentation keys on custom events.</summary>
        public void SegmentationWhitelist_KeepsOnlyListed()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"sw\":[\"keep\"]}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            Segmentation seg = new Segmentation();
            seg.Add("keep", "1");
            seg.Add("other", "2");
            Countly.RecordEvent("e", 1, seg).Wait();

            List<string> keys = SegKeys(Countly.Instance.Events[0]);
            Assert.Contains("keep", keys);
            Assert.DoesNotContain("other", keys);
        }

        [Fact]
        /// <summary>esb applies per event: only the named event loses the listed keys.</summary>
        public void EventSegmentationFilter_AppliesPerEvent()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"esb\":{\"e1\":[\"a\"]}}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            Segmentation seg1 = new Segmentation();
            seg1.Add("a", "1");
            seg1.Add("b", "2");
            Countly.RecordEvent("e1", 1, seg1).Wait();

            Segmentation seg2 = new Segmentation();
            seg2.Add("a", "1");
            Countly.RecordEvent("e2", 1, seg2).Wait();

            List<string> e1Keys = SegKeys(Countly.Instance.Events[0]);
            Assert.DoesNotContain("a", e1Keys);
            Assert.Contains("b", e1Keys);
            Assert.Contains("a", SegKeys(Countly.Instance.Events[1])); // no rule for e2, all allowed
        }

        [Fact]
        /// <summary>upb removes the listed keys from custom user properties.</summary>
        public void UserPropertyFilter_Blacklist_RemovesFilteredKeys()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"upb\":[\"secret\"]}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            cc.DisableManualUserDetailsSave().DisableAutoSendUserDetails();
            Countly.Instance.Init(cc).Wait();

            Countly.UserDetails.Custom.Add("secret", "1");
            Countly.UserDetails.Custom.Add("ok", "2");

            Dictionary<string, string> custom = Countly.UserDetails.Custom.ToDictionary();
            Assert.False(custom.ContainsKey("secret"));
            Assert.Equal("2", custom["ok"]);
        }

        [Fact]
        /// <summary>upcl caps the custom user property count (oldest kept).</summary>
        public void UserPropertyCacheLimit_TrimsOverflow()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"upcl\":2}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            cc.DisableManualUserDetailsSave().DisableAutoSendUserDetails();
            Countly.Instance.Init(cc).Wait();

            Countly.UserDetails.Custom.Add("k1", "1");
            Countly.UserDetails.Custom.Add("k2", "2");
            Countly.UserDetails.Custom.Add("k3", "3");

            Dictionary<string, string> custom = Countly.UserDetails.Custom.ToDictionary();
            Assert.Equal(2, custom.Count);
            Assert.True(custom.ContainsKey("k1"));
            Assert.True(custom.ContainsKey("k2"));
            Assert.False(custom.ContainsKey("k3"));
        }

        [Fact]
        /// <summary>jte is parsed into the journey trigger set.</summary>
        public void JourneyTriggerEvents_Parsed()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"jte\":[\"e1\"]}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            Assert.True(Countly.Instance.moduleServerConfig.IsJourneyTriggerEvent("e1"));
            Assert.False(Countly.Instance.moduleServerConfig.IsJourneyTriggerEvent("e2"));
        }

        [Fact]
        /// <summary>dort drops queued requests older than the limit on upload; fresh ones stay.</summary>
        public void DropOldRequests_PrunedOnUpload()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"dort\":1}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();
            Countly.Instance.StoredRequests.Clear();

            long nowMs = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            long oldMs = nowMs - (2L * 60L * 60L * 1000L); // 2h old, limit is 1h
            Countly.Instance.StoredRequests.Enqueue(new StoredRequest("/i?app_key=APP_KEY&timestamp=" + oldMs + "&end_session=1"));
            Countly.Instance.StoredRequests.Enqueue(new StoredRequest("/i?app_key=APP_KEY&timestamp=" + nowMs + "&end_session=1"));

            Countly.Instance.Upload().Wait();

            Assert.Single(Countly.Instance.StoredRequests);
            Assert.Contains("timestamp=" + nowMs, Countly.Instance.StoredRequests.Peek().Request);
        }

        [Fact]
        /// <summary>An incoming blacklist evicts the stored whitelist of the same category (and applies).</summary>
        public void FilterLists_IncomingBlacklistEvictsStoredWhitelist()
        {
            Storage.Instance.SaveToFile<ServerConfigEntity>(
                ModuleServerConfig.serverConfigFilename,
                new ServerConfigEntity { Json = "{\"v\":1,\"t\":1,\"c\":{\"ew\":[\"a\"]}}" }).Wait();
            MockHttpServer server = new MockHttpServer((body) =>
                body.Contains("method=sc") ? "{\"v\":1,\"t\":2,\"c\":{\"eb\":[\"c\"]}}" : null);
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            Countly.Instance.Init(cc).Wait();

            Assert.True(Countly.Instance.moduleServerConfig.IsEventKeyAllowed("b"));  // blacklist mode now
            Assert.False(Countly.Instance.moduleServerConfig.IsEventKeyAllowed("c"));

            ServerConfigEntity stored = Storage.Instance.LoadFromFile<ServerConfigEntity>(ModuleServerConfig.serverConfigFilename).Result;
            Assert.Contains("\"eb\"", stored.Json);
            Assert.DoesNotContain("\"ew\"", stored.Json);
            server.Dispose();
        }

        [Fact]
        /// <summary>Invalid filter values are dropped: non-array lists, negative dort, non-positive upcl.</summary>
        public void Sanitize_DropsInvalidFilterValues()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.SetSDKBehaviorSettings("{\"c\":{\"eb\":\"notarray\",\"esb\":[\"notobject\"],\"dort\":-1,\"upcl\":0}}");
            cc.DisableSDKBehaviorSettingsUpdates();
            Countly.Instance.Init(cc).Wait();

            Assert.True(Countly.Instance.moduleServerConfig.IsEventKeyAllowed("anything"));
            Assert.Equal(0, Countly.Instance.moduleServerConfig.GetDropOldRequestTimeHours());
            Assert.Equal(100, Countly.Instance.moduleServerConfig.GetUserPropertyCacheLimit());
        }

        [Fact]
        /// <summary>A device-ID change triggers an additional SBS fetch.</summary>
        public void ChangeDeviceId_TriggersRefetch()
        {
            int scCalls = 0;
            MockHttpServer server = new MockHttpServer((body) => {
                if (body.Contains("method=sc")) { scCalls++; return "{\"v\":1,\"t\":1,\"c\":{\"lkl\":50}}"; }
                return null;
            });
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            Countly.Instance.Init(cc).Wait();
            Assert.Equal(1, scCalls);

            Countly.Instance.ChangeDeviceId("new_device_id").Wait();
            Assert.Equal(2, scCalls);
            server.Dispose();
        }
    }
}
