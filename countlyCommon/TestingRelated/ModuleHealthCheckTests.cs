using System;
using System.Collections.Generic;
using System.Linq;
using CountlySDK;
using CountlySDK.CountlyCommon;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.CountlyCommon.Server;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Newtonsoft.Json.Linq;
using Xunit;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace TestProject_common
{
    public class ModuleHealthCheckTests : IDisposable
    {
        public ModuleHealthCheckTests()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Halt();
            TestHelper.CleanDataFiles();
        }

        public void Dispose() { }

        [Fact]
        /// <summary>DisableHealthCheck chains fluently and sets the flag.</summary>
        public void ConfigApi_DisableHealthCheck_ChainsAndStores()
        {
            CountlyConfig cc = TestHelper.GetConfig();
            CountlyConfig returned = (CountlyConfig)cc.DisableHealthCheck();

            Assert.Same(cc, returned);
            Assert.True(cc.healthCheckDisabled);
        }

        [Fact]
        /// <summary>Warning/error counters accumulate.</summary>
        public void Counters_Accumulate()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.LogWarning();
            m.LogWarning();
            m.LogError();

            Assert.Equal(2, m.WarningCount);
            Assert.Equal(1, m.ErrorCount);
        }

        [Fact]
        /// <summary>Status code is guarded to (0,1000); the error message always overwrites.</summary>
        public void FailedRequest_RecordsAndGuardsStatus()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.LogFailedNetworkRequest(503, "A");
            Assert.Equal(503, m.LastStatusCode);
            Assert.Equal("A", m.LastErrorMessage);

            m.LogFailedNetworkRequest(0, "B");      // 0 rejected for sc, em overwrites
            m.LogFailedNetworkRequest(1000, "C");   // 1000 rejected for sc, em overwrites
            Assert.Equal(503, m.LastStatusCode);
            Assert.Equal("C", m.LastErrorMessage);
        }

        [Fact]
        /// <summary>Error message is truncated to 1000 chars.</summary>
        public void FailedRequest_TruncatesMessage()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.LogFailedNetworkRequest(500, new string('x', 1500));
            Assert.Equal(1000, m.LastErrorMessage.Length);
        }

        [Fact]
        /// <summary>SaveState then reload restores all counters.</summary>
        public void SaveAndLoad_RoundTrip()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.LogError();
            m.LogWarning();
            m.LogFailedNetworkRequest(500, "err");
            m.SaveState();

            ModuleHealthCheck m2 = new ModuleHealthCheck(null, TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            Assert.Equal(1, m2.ErrorCount);
            Assert.Equal(1, m2.WarningCount);
            Assert.Equal(500, m2.LastStatusCode);
            Assert.Equal("err", m2.LastErrorMessage);
        }

        [Fact]
        /// <summary>No stored file → zero defaults (status -1, message empty).</summary>
        public void Load_Missing_ZeroDefaults()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            Assert.Equal(0, m.ErrorCount);
            Assert.Equal(0, m.WarningCount);
            Assert.Equal(-1, m.LastStatusCode);
            Assert.Equal("", m.LastErrorMessage);
        }

        [Fact]
        /// <summary>Malformed stored JSON → reset to zero defaults and storage cleared.</summary>
        public void Load_Malformed_ResetsAndClears()
        {
            Storage.Instance.SaveToFile<HealthCheckEntity>(
                ModuleHealthCheck.healthCheckFilename, new HealthCheckEntity { Json = "not-json" }).Wait();

            ModuleHealthCheck m = new ModuleHealthCheck(null, TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            Assert.Equal(0, m.ErrorCount);

            // storage was wiped, so a second load also yields zero defaults
            ModuleHealthCheck m2 = new ModuleHealthCheck(null, TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            Assert.Equal(0, m2.ErrorCount);
            Assert.Equal(-1, m2.LastStatusCode);
        }

        [Fact]
        /// <summary>ClearAndSave zeroes counters and wipes storage.</summary>
        public void ClearAndSave_Resets()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.LogError();
            m.ClearAndSave();
            Assert.Equal(0, m.ErrorCount);

            ModuleHealthCheck m2 = new ModuleHealthCheck(null, TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            Assert.Equal(0, m2.ErrorCount);
        }

        [Fact]
        /// <summary>Registered hook counts WARNING/ERROR logs but not DEBUG.</summary>
        public void LogHook_CountsWarningsAndErrors()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.RegisterHooks();
            try {
                UtilityHelper.CountlyLogging("w", LogLevel.WARNING);
                UtilityHelper.CountlyLogging("e", LogLevel.ERROR);
                UtilityHelper.CountlyLogging("d", LogLevel.DEBUG);

                Assert.Equal(1, m.WarningCount);
                Assert.Equal(1, m.ErrorCount);
            } finally {
                m.UnregisterHooks();
            }
        }

        [Fact]
        /// <summary>Counting is independent of the console-logging flag.</summary>
        public void LogHook_CountsWhenLoggingDisabled()
        {
            Countly.IsLoggingEnabled = false;
            ModuleHealthCheck m = new ModuleHealthCheck(null, TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.RegisterHooks();
            try {
                UtilityHelper.CountlyLogging("w", LogLevel.WARNING);
                Assert.Equal(1, m.WarningCount);
            } finally {
                m.UnregisterHooks();
            }
        }

        [Fact]
        /// <summary>ApiBase failed-request hook records status/body; UnregisterHooks clears it.</summary>
        public void FailedRequestHook_RecordsViaApiBase()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.RegisterHooks();
            try {
                ApiBase.FailedRequestHook(504, "Gateway");
                Assert.Equal(504, m.LastStatusCode);
                Assert.Equal("Gateway", m.LastErrorMessage);
            } finally {
                m.UnregisterHooks();
            }
            Assert.Null(ApiBase.FailedRequestHook);
        }

        [Fact]
        /// <summary>After UnregisterHooks, further logs are not counted.</summary>
        public void UnregisterHooks_StopsCounting()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.RegisterHooks();
            m.UnregisterHooks();
            UtilityHelper.CountlyLogging("e", LogLevel.ERROR);
            Assert.Equal(0, m.ErrorCount);
        }

        private static List<MockHttpServer.RequestInfo> Reqs(MockHttpServer s)
        {
            return s.Requests.ToList();
        }

        private static bool IsHc(MockHttpServer.RequestInfo r)
        {
            return r.Body != null && r.Body.Contains("hc=");
        }

        [Fact]
        /// <summary>Health check is sent to /i, after the SBS (method=sc) request, off-queue (no rr).</summary>
        public void HealthCheck_SentToI_AfterSbs_OffQueue()
        {
            MockHttpServer server = new MockHttpServer((body) => "{\"result\":\"Success\"}");
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            Countly.Instance.Init(cc).Wait();

            List<MockHttpServer.RequestInfo> reqs = Reqs(server);
            int scIdx = reqs.FindIndex(r => r.Body != null && r.Body.Contains("method=sc"));
            int hcIdx = reqs.FindIndex(IsHc);

            Assert.True(scIdx >= 0, "SBS request not sent");
            Assert.True(hcIdx >= 0, "health check request not sent");
            Assert.True(hcIdx > scIdx, "health check must be after SBS");
            Assert.Contains("/i", reqs[hcIdx].Path);
            Assert.DoesNotContain("rr=", reqs[hcIdx].Body); // direct request has no remaining-request-count
            server.Dispose();
        }

        [Fact]
        /// <summary>Payload carries metrics (only _app_version) and an hc object with all six keys.</summary>
        public void HealthCheck_Payload_HasMetricsAndAllHcKeys()
        {
            MockHttpServer server = new MockHttpServer((body) => "{\"result\":\"Success\"}");
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            Countly.Instance.Init(cc).Wait();

            MockHttpServer.RequestInfo hc = Reqs(server).First(IsHc);
            Dictionary<string, string> p = TestHelper.GetParams(hc.Body);

            Assert.True(p.ContainsKey("metrics"));
            JObject metrics = JObject.Parse(p["metrics"]);
            Assert.Equal(TestHelper.APP_VERSION, metrics.Value<string>("_app_version"));
            Assert.Equal(1, metrics.Count);

            JObject hcObj = JObject.Parse(p["hc"]);
            foreach (string k in new[] { "el", "wl", "sc", "em", "bom", "cbom" }) {
                Assert.True(hcObj.ContainsKey(k), "missing hc key: " + k);
            }
            Assert.Equal(0, hcObj.Value<int>("bom"));
            Assert.Equal(0, hcObj.Value<int>("cbom"));
            server.Dispose();
        }

        [Fact]
        /// <summary>DisableHealthCheck suppresses the request.</summary>
        public void HealthCheck_Disabled_NoRequest()
        {
            MockHttpServer server = new MockHttpServer((body) => "{\"result\":\"Success\"}");
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            cc.DisableHealthCheck();
            Countly.Instance.Init(cc).Wait();

            Assert.DoesNotContain(Reqs(server), IsHc);
            server.Dispose();
        }

        [Fact]
        /// <summary>Backend mode suppresses the request.</summary>
        public void HealthCheck_BackendMode_NoRequest()
        {
            MockHttpServer server = new MockHttpServer((body) => "{\"result\":\"Success\"}");
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            cc.EnableBackendMode();
            Countly.Instance.Init(cc).Wait();

            Assert.DoesNotContain(Reqs(server), IsHc);
            server.Dispose();
        }

        [Fact]
        /// <summary>Server networking=false suppresses the request.</summary>
        public void HealthCheck_NetworkingDisabled_NoRequest()
        {
            MockHttpServer server = new MockHttpServer((body) =>
                body.Contains("method=sc") ? "{\"v\":1,\"t\":1,\"c\":{\"networking\":false}}" : "{\"result\":\"Success\"}");
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            Countly.Instance.Init(cc).Wait();

            Assert.DoesNotContain(Reqs(server), IsHc);
            server.Dispose();
        }

        [Fact]
        /// <summary>On success the accumulated counters are sent, then cleared and storage wiped.</summary>
        public void HealthCheck_Success_SendsThenClears()
        {
            Storage.Instance.SaveToFile<HealthCheckEntity>(ModuleHealthCheck.healthCheckFilename,
                new HealthCheckEntity { Json = "{\"LErr\":5,\"LWar\":3,\"RStatC\":503,\"REMsg\":\"x\",\"BReq\":0,\"CBReq\":0}" }).Wait();

            // Valid SBS envelope so init emits no internal WARNING (which would bump wl).
            MockHttpServer server = new MockHttpServer((body) =>
                body.Contains("method=sc") ? "{\"v\":1,\"t\":1,\"c\":{\"tracking\":true}}" : "{\"result\":\"Success\"}");
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            Countly.Instance.Init(cc).Wait();

            JObject hcObj = JObject.Parse(TestHelper.GetParams(Reqs(server).First(IsHc).Body)["hc"]);
            Assert.Equal(5, hcObj.Value<int>("el"));
            Assert.Equal(3, hcObj.Value<int>("wl"));
            Assert.Equal(503, hcObj.Value<int>("sc"));

            Assert.Equal(0, Countly.Instance.moduleHealthCheck.ErrorCount);
            Assert.Equal(0, Countly.Instance.moduleHealthCheck.WarningCount);
            HealthCheckEntity e = Storage.Instance.LoadFromFile<HealthCheckEntity>(ModuleHealthCheck.healthCheckFilename).Result;
            Assert.True(e == null || string.IsNullOrEmpty(e.Json));
            server.Dispose();
        }

        [Fact]
        /// <summary>On failure (no result key) counters and storage are retained.</summary>
        public void HealthCheck_Failure_RetainsCounters()
        {
            Storage.Instance.SaveToFile<HealthCheckEntity>(ModuleHealthCheck.healthCheckFilename,
                new HealthCheckEntity { Json = "{\"LErr\":5,\"LWar\":0,\"RStatC\":-1,\"REMsg\":\"\",\"BReq\":0,\"CBReq\":0}" }).Wait();

            MockHttpServer server = new MockHttpServer((body) =>
                body.Contains("method=sc") ? "{\"garbage\":1}" : "{\"noresult\":true}");
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            Countly.Instance.Init(cc).Wait();

            Assert.Equal(5, Countly.Instance.moduleHealthCheck.ErrorCount);
            HealthCheckEntity e = Storage.Instance.LoadFromFile<HealthCheckEntity>(ModuleHealthCheck.healthCheckFilename).Result;
            Assert.False(e == null || string.IsNullOrEmpty(e.Json));
            server.Dispose();
        }

        [Fact]
        /// <summary>Only one health check is sent per lifecycle (one-shot).</summary>
        public void HealthCheck_OneShot_NoSecondSend()
        {
            MockHttpServer server = new MockHttpServer((body) => "{\"result\":\"Success\"}");
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            Countly.Instance.Init(cc).Wait();

            int firstCount = Reqs(server).Count(IsHc);
            Countly.Instance.moduleHealthCheck.SendHealthCheck().Wait();
            int secondCount = Reqs(server).Count(IsHc);

            Assert.Equal(1, firstCount);
            Assert.Equal(firstCount, secondCount);
            server.Dispose();
        }
    }
}
