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
    }
}
