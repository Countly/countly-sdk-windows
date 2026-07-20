using System;
using System.Collections.Generic;
using CountlySDK;
using CountlySDK.CountlyCommon;
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
    }
}
