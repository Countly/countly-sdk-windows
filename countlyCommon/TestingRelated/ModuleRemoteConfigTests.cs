using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CountlySDK;
using CountlySDK.CountlyCommon;
using CountlySDK.Entities;
using Newtonsoft.Json;
using Xunit;

namespace TestProject_common
{
    public class ModuleRemoteConfigTests : IDisposable
    {
        /// <summary>
        /// Test setup
        /// </summary>
        public ModuleRemoteConfigTests()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Halt();
            TestHelper.CleanDataFiles();
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {

        }

        [Fact]
        /// <summary>
        /// Verifies that remote config keys are downloaded successfully
        /// when the download is triggered manually and consent is not required.
        /// </summary>
        public void DownloadKeys_ManualDownload_CNR()
        {
            RemoteConfigDownloadFlow((config) => { }, () => Countly.Instance.RemoteConfig().DownloadKeys().Wait());

        }

        [Fact]
        /// <summary>
        /// Verifies that remote config keys are downloaded automatically
        /// when automatic download triggers are enabled and consent is not required.
        /// </summary>
        public void DownloadKeys_AutomaticDownload_CNR()
        {
            RemoteConfigDownloadFlow((config) => config.EnableRemoteConfigAutomaticTriggers());
        }

        [Fact]
        /// <summary>
        /// Verifies that remote config keys are downloaded automatically
        /// when automatic triggers are enabled, consent is required,
        /// and remote config consent is explicitly granted.
        /// </summary>
        public void DownloadKeys_AutomaticDownload_CR_CG()
        {
            RemoteConfigDownloadFlow((config) => {
                config.EnableRemoteConfigAutomaticTriggers();
                config.consentRequired = true;
            }, () => {
                Dictionary<Countly.ConsentFeatures, bool> consents = new Dictionary<Countly.ConsentFeatures, bool> { { Countly.ConsentFeatures.RemoteConfig, true } };
                Countly.Instance.SetConsent(consents).Wait();
            });
        }

        [Fact]
        /// <summary>
        /// Verifies that remote config keys are NOT downloaded automatically
        /// when automatic triggers are enabled, consent is required,
        /// but remote config consent is NOT granted.
        /// </summary>
        public void DownloadKeys_AutomaticDownload_CR_CNG()
        {
            RemoteConfigDownloadFlow((config) => {
                config.EnableRemoteConfigAutomaticTriggers();
                config.consentRequired = true;
            }, () => { }, false);
        }

        [Fact]
        /// <summary>
        /// Verifies that invalid, malformed, or unsupported JSON responses
        /// do not populate remote config values and are handled gracefully
        /// without throwing exceptions or leaving residual state.
        /// </summary>
        public void DownloadKeys_Invalid()
        {
            List<string> results = new List<string> {
                "",
                "{}",
                "{",
                "{",
                "{\"key\":}",
                "[]",
                "\"just a string\"",
                "{\"k\": { \"v\": [}}"
            };
            int currentResponse = 0;
            MockHttpServer server = new MockHttpServer((body) => {
                if (body.Contains("method=rc")) {
                    return results[currentResponse];
                }
                return null;
            });
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;

            Countly.Instance.Init(cc).Wait();

            for (currentResponse = 0; currentResponse < results.Count; currentResponse++) {
                Countly.Instance.RemoteConfig().DownloadKeys().Wait();
                IDictionary<string, RCData> rcValues = Countly.Instance.RemoteConfig().GetValues();
                Assert.Empty(rcValues);
            }
        }

        private void RemoteConfigDownloadFlow(Action<CountlyConfig> configSetter, Action runnable = null, bool expectDownload = true)
        {
            IDictionary<string, object> expectedRcValues = new Dictionary<string, object>() {
                {"rc_1", 1 },
                {"rc_2", "2" },
                {"rc_3", false },
                {"rc_4", 67.797 }
            };
            MockHttpServer server = new MockHttpServer((body) => {
                if (body.Contains("method=rc")) {
                    return JsonConvert.SerializeObject(expectedRcValues);
                }
                return null;
            });
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            configSetter(cc);

            Countly.Instance.Init(cc).Wait();

            runnable?.Invoke();

            IDictionary<string, RCData> rcValues = Countly.Instance.RemoteConfig().GetValues();

            if (!expectDownload) {
                Assert.Empty(rcValues);
                server.Dispose();
                return;
            }

            Assert.Equal(4, rcValues.Count);
            Assert.Equal(1L, rcValues["rc_1"].Value);
            Assert.Equal("2", rcValues["rc_2"].Value);
            Assert.Equal(false, rcValues["rc_3"].Value);
            Assert.Equal(67.797, rcValues["rc_4"].Value);
            Assert.Equal(1L, Countly.Instance.RemoteConfig().GetValue("rc_1").Value);
            Assert.Equal("2", Countly.Instance.RemoteConfig().GetValue("rc_2").Value);
            Assert.Equal(false, Countly.Instance.RemoteConfig().GetValue("rc_3").Value);
            Assert.Equal(67.797, Countly.Instance.RemoteConfig().GetValue("rc_4").Value);
            server.Dispose();
        }
    }
}
