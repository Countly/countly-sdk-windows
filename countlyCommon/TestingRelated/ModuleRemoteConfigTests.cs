using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using CountlySDK;
using CountlySDK.CountlyCommon;
using CountlySDK.CountlyCommon.Entities;
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
        /// Verifies that remote config keys are downloaded automatically
        /// when automatic download triggers are enabled and consent is not required,
        /// and that a device ID change triggers an additional remote config download.
        /// </summary>
        public void DownloadKeys_AutomaticDownload_CNR_DeviceId()
        {
            RemoteConfigDownloadFlow((config) => {
                config.EnableRemoteConfigAutomaticTriggers();
            }, () => {
                Countly.Instance.SetId("device_id").Wait();
            }, calledTimesExpected: 2);
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
            }, () => { }, false, 0);
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

        [Fact]
        /// <summary>
        /// By default (auto opt-in enabled), a Remote Config download request includes oi=1.
        /// </summary>
        public void DownloadKeys_AutoEnrollDefault_SendsOptIn()
        {
            string body = null;
            MockHttpServer server = new MockHttpServer((b) => {
                if (b.Contains("method=rc")) { body = b; return "{}"; }
                return null;
            });
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.RemoteConfig().DownloadKeys().Wait();

            Assert.Contains("oi=1", body);
            server.Dispose();
        }

        [Fact]
        /// <summary>
        /// When auto opt-in is disabled via config, the Remote Config download request omits oi.
        /// </summary>
        public void DownloadKeys_AutoEnrollDisabled_OmitsOptIn()
        {
            string body = null;
            MockHttpServer server = new MockHttpServer((b) => {
                if (b.Contains("method=rc")) { body = b; return "{}"; }
                return null;
            });
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            cc.DisableAutoEnrollInABTesting();

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.RemoteConfig().DownloadKeys().Wait();

            Assert.DoesNotContain("oi=1", body);
            server.Dispose();
        }

        [Fact]
        /// <summary>
        /// Enrolling into A/B tests queues a method=ab request carrying the JSON-encoded keys.
        /// </summary>
        public void EnrollIntoABTestsForKeys_QueuesAbRequest()
        {
            MockHttpServer server = new MockHttpServer();
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.deferUpload = true;
            Countly.Instance.StoredRequests.Clear();

            Countly.Instance.RemoteConfig().EnrollIntoABTestsForKeys(new List<string> { "a", "b" }).Wait();

            Assert.Single(Countly.Instance.StoredRequests);
            StoredRequest sr = Countly.Instance.StoredRequests.Dequeue();
            NameValueCollection q = HttpUtility.ParseQueryString(sr.Request);
            Assert.Equal("ab", q.Get("method"));
            Assert.Equal("[\"a\",\"b\"]", q.Get("keys"));
            server.Dispose();
        }

        [Fact]
        /// <summary>
        /// Enrolling with null or empty keys is a no-op (nothing is queued).
        /// </summary>
        public void EnrollIntoABTestsForKeys_NoKeys_QueuesNothing()
        {
            MockHttpServer server = new MockHttpServer();
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.deferUpload = true;
            Countly.Instance.StoredRequests.Clear();

            Countly.Instance.RemoteConfig().EnrollIntoABTestsForKeys(null).Wait();
            Countly.Instance.RemoteConfig().EnrollIntoABTestsForKeys(new List<string>()).Wait();

            Assert.Empty(Countly.Instance.StoredRequests);
            server.Dispose();
        }

        [Fact]
        /// <summary>
        /// Exiting A/B tests with keys queues a method=ab_opt_out request carrying the JSON keys.
        /// </summary>
        public void ExitABTestsForKeys_WithKeys_QueuesOptOutRequest()
        {
            MockHttpServer server = new MockHttpServer();
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.deferUpload = true;
            Countly.Instance.StoredRequests.Clear();

            Countly.Instance.RemoteConfig().ExitABTestsForKeys(new List<string> { "a", "b" }).Wait();

            Assert.Single(Countly.Instance.StoredRequests);
            StoredRequest sr = Countly.Instance.StoredRequests.Dequeue();
            NameValueCollection q = HttpUtility.ParseQueryString(sr.Request);
            Assert.Equal("ab_opt_out", q.Get("method"));
            Assert.Equal("[\"a\",\"b\"]", q.Get("keys"));
            server.Dispose();
        }

        [Fact]
        /// <summary>
        /// Exiting with no keys (exit from ALL tests) queues method=ab_opt_out with no keys param.
        /// </summary>
        public void ExitABTestsForKeys_NoKeys_QueuesOptOutWithoutKeys()
        {
            MockHttpServer server = new MockHttpServer();
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.deferUpload = true;
            Countly.Instance.StoredRequests.Clear();

            Countly.Instance.RemoteConfig().ExitABTestsForKeys().Wait();

            Assert.Single(Countly.Instance.StoredRequests);
            StoredRequest sr = Countly.Instance.StoredRequests.Dequeue();
            NameValueCollection q = HttpUtility.ParseQueryString(sr.Request);
            Assert.Equal("ab_opt_out", q.Get("method"));
            Assert.Null(q.Get("keys"));
            server.Dispose();
        }

        [Fact]
        /// <summary>
        /// When consent is required but Remote Config consent is not granted, enroll/exit are
        /// gated by the RemoteConfig() accessor (returns a mock) and queue nothing.
        /// </summary>
        public void EnrollExit_ConsentRequiredNotGranted_QueuesNothing()
        {
            MockHttpServer server = new MockHttpServer();
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            cc.consentRequired = true;

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.deferUpload = true;
            Countly.Instance.StoredRequests.Clear();

            Countly.Instance.RemoteConfig().EnrollIntoABTestsForKeys(new List<string> { "a" }).Wait();
            Countly.Instance.RemoteConfig().ExitABTestsForKeys(new List<string> { "a" }).Wait();

            Assert.Empty(Countly.Instance.StoredRequests);
            server.Dispose();
        }

        private void RemoteConfigDownloadFlow(Action<CountlyConfig> configSetter, Action runnable = null, bool expectDownload = true, int calledTimesExpected = 1)
        {
            IDictionary<string, object> expectedRcValues = new Dictionary<string, object>() {
                {"rc_1", 1 },
                {"rc_2", "2" },
                {"rc_3", false },
                {"rc_4", 67.797 }
            };
            int calledTimes = 0;
            MockHttpServer server = new MockHttpServer((body) => {
                if (body.Contains("method=rc")) {
                    calledTimes++;
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
            Assert.Equal(calledTimesExpected, calledTimes);

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
