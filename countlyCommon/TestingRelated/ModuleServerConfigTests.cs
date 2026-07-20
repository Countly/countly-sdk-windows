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
    }
}
