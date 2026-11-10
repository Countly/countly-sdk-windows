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
            ModuleHealthCheck m = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.LogWarning();
            m.LogWarning();
            m.LogError();

            Assert.Equal(2, m.WarningCount);
            Assert.Equal(1, m.ErrorCount);
        }

        [Fact]
        /// <summary>Status code is guarded to (0,1000); a request without a valid status (no answer) changes neither value.</summary>
        public void FailedRequest_RecordsAndGuardsStatus()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.LogFailedNetworkRequest(503, "A");
            Assert.Equal(503, m.LastStatusCode);
            Assert.Equal("A", m.LastErrorMessage);

            m.LogFailedNetworkRequest(0, "B");
            m.LogFailedNetworkRequest(1000, "C");
            Assert.Equal(503, m.LastStatusCode);
            Assert.Equal("A", m.LastErrorMessage);
        }

        [Fact]
        /// <summary>Error message is truncated to 1000 chars.</summary>
        public void FailedRequest_TruncatesMessage()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.LogFailedNetworkRequest(500, new string('x', 1500));
            Assert.Equal(1000, m.LastErrorMessage.Length);
        }

        [Fact]
        /// <summary>SaveState then reload restores all counters.</summary>
        public void SaveAndLoad_RoundTrip()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.LogError();
            m.LogWarning();
            m.LogFailedNetworkRequest(500, "err");
            m.SaveState();

            ModuleHealthCheck m2 = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            Assert.Equal(1, m2.ErrorCount);
            Assert.Equal(1, m2.WarningCount);
            Assert.Equal(500, m2.LastStatusCode);
            Assert.Equal("err", m2.LastErrorMessage);
        }

        [Fact]
        /// <summary>No stored file → zero defaults (status -1, message empty).</summary>
        public void Load_Missing_ZeroDefaults()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
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

            ModuleHealthCheck m = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            Assert.Equal(0, m.ErrorCount);
            Assert.Equal(1, m.LogCodeCount("e503"));

            // storage was wiped, so a second load also yields zero defaults
            ModuleHealthCheck m2 = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            Assert.Equal(0, m2.ErrorCount);
            Assert.Equal(-1, m2.LastStatusCode);
        }

        [Fact]
        /// <summary>ClearAndSave zeroes counters and wipes storage.</summary>
        public void ClearAndSave_Resets()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.LogError();
            m.ClearAndSave();
            Assert.Equal(0, m.ErrorCount);

            ModuleHealthCheck m2 = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            Assert.Equal(0, m2.ErrorCount);
        }

        [Fact]
        /// <summary>Registered hook counts WARNING/ERROR logs but not DEBUG.</summary>
        public void LogHook_CountsWarningsAndErrors()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
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
            ModuleHealthCheck m = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.RegisterHooks();
            try {
                UtilityHelper.CountlyLogging("w", LogLevel.WARNING);
                Assert.Equal(1, m.WarningCount);
            } finally {
                m.UnregisterHooks();
            }
        }

        [Fact]
        /// <summary>After UnregisterHooks, further logs are not counted.</summary>
        public void UnregisterHooks_StopsCounting()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
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
            foreach (string k in new[] { "el", "wl", "sc", "em", "bom", "cbom", "fu", "lc" }) {
                Assert.True(hcObj.ContainsKey(k), "missing hc key: " + k);
            }
            Assert.Equal(JTokenType.Object, hcObj["fu"].Type);
            Assert.Equal(JTokenType.Object, hcObj["lc"].Type);
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
        /// <summary>On success the accumulated counters are sent, then the reported amounts are taken away and the rest is stored.</summary>
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
            JObject stored = JObject.Parse(e.Json);
            Assert.Equal(0, stored.Value<int>("LErr"));
            Assert.Equal(0, stored.Value<int>("LWar"));
            Assert.Equal(-1, stored.Value<int>("RStatC"));
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

        private static CountlyConfig ConfigFor(MockHttpServer server)
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            return cc;
        }

        [Fact]
        /// <summary>The session, event, view and crash calls of the app and of the SDK are counted once each, with the problems they run into.</summary>
        public void MethodUsage_CountsAppAndSdkCalls()
        {
            MockHttpServer server = new MockHttpServer();
            CountlyConfig cc = ConfigFor(server);
            cc.DisableHealthCheck();
            Countly.Instance.Init(cc).Wait();

            Countly.Instance.SessionBegin().Wait();
            Countly.Instance.SessionUpdate(5).Wait();
            Countly.RecordEvent("purchase").Wait();
            Countly.Instance.StartEvent("checkout");
            Countly.Instance.StartEvent("checkout");
            Countly.Instance.EndEvent("checkout").Wait();
            Countly.Instance.EndEvent("never_started").Wait();
            Countly.Instance.StartEvent("dialog");
            Countly.Instance.CancelEvent("dialog");
            Countly.Instance.CancelEvent("never_started");
            Countly.Instance.RecordView("home").Wait();
            Countly.Instance.RecordView("").Wait();
            Countly.RecordException("handled", "at line 1").Wait();
            Countly.Instance.AddCrashBreadCrumb("opened the cart");
#pragma warning disable CS0618
            Countly.AddBreadCrumb("old breadcrumb");
#pragma warning restore CS0618
            Countly.Instance.ChangeDeviceId("hc-usage-user-2", false).Wait();
            Countly.Instance.SessionEnd().Wait();

            ModuleHealthCheck m = Countly.Instance.moduleHealthCheck;
            Assert.Equal(1, m.UsageCount("sessions", "begin"));
            Assert.Equal(1, m.UsageCount("sessions", "update"));
            Assert.Equal(1, m.UsageCount("sessions", "end:a"));
            Assert.Equal(1, m.UsageCount("sessions", "begin:a"));
            Assert.Equal(1, m.UsageCount("sessions", "end"));
            Assert.Equal(1, m.UsageCount("events", "record"));
            Assert.Equal(3, m.UsageCount("events", "start"));
            Assert.Equal(2, m.UsageCount("events", "end"));
            Assert.Equal(2, m.UsageCount("events", "cancel"));
            Assert.Equal(2, m.UsageCount("views", "start"));
            Assert.Equal(1, m.UsageCount("crashes", "record"));
            Assert.Equal(1, m.UsageCount("crashes", "log"));
            Assert.Equal(1, m.UsageCount("crashes", "AddBreadCrumb:d"));
            Assert.Equal(1, m.LogCodeCount("w722"));
            Assert.Equal(1, m.LogCodeCount("w720"));
            Assert.Equal(1, m.LogCodeCount("w721"));
            Assert.Equal(1, m.LogCodeCount("w710"));
            Assert.Equal(1, m.LogCodeCount("w120"));
            server.Dispose();
        }

        [Fact]
        /// <summary>The calls of a feature and the problems they run into are counted only while the feature has consent.</summary>
        public void MethodUsage_FollowsConsent()
        {
            MockHttpServer server = new MockHttpServer();
            CountlyConfig cc = ConfigFor(server);
            cc.DisableHealthCheck();
            cc.consentRequired = true;
            Countly.Instance.Init(cc).Wait();

            Countly.RecordEvent("without_consent").Wait();
            Countly.Instance.RecordView("").Wait();
            Countly.Instance.SetConsent(new Dictionary<ConsentFeatures, bool> { { ConsentFeatures.Events, true }, { ConsentFeatures.Views, true } }).Wait();
            Countly.RecordEvent("with_consent").Wait();
            Countly.Instance.RecordView("").Wait();

            ModuleHealthCheck m = Countly.Instance.moduleHealthCheck;
            Assert.Equal(1, m.UsageCount("events", "record"));
            Assert.Equal(1, m.UsageCount("views", "start"));
            Assert.Equal(1, m.LogCodeCount("w710"));
            server.Dispose();
        }

        [Fact]
        /// <summary>Warnings and log codes of calls made before init are added to the health check once it exists.</summary>
        public void LogCodes_BeforeInit_AreKept()
        {
            Countly.RecordEvent("too_early").Wait();
            Countly.Instance.SessionBegin().Wait();

            MockHttpServer server = new MockHttpServer();
            CountlyConfig cc = ConfigFor(server);
            cc.DisableHealthCheck();
            Countly.Instance.Init(cc).Wait();

            ModuleHealthCheck m = Countly.Instance.moduleHealthCheck;
            Assert.Equal(2, m.LogCodeCount("w101"));
            Assert.True(m.WarningCount >= 2);
            server.Dispose();
        }

        [Fact]
        /// <summary>A method usage or log code count stops growing at 65535 until it is reported.</summary>
        public void Counts_StopAtTheCap()
        {
            Storage.Instance.SaveToFile<HealthCheckEntity>(ModuleHealthCheck.healthCheckFilename,
                new HealthCheckEntity { Json = "{\"MU\":{\"events\":{\"record\":65535}},\"LC\":{\"w722\":65535}}" }).Wait();

            ModuleHealthCheck m = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.RecordUsage("events", "record");
            m.RecordLogCode("w722", "events");

            Assert.Equal(65535, m.UsageCount("events", "record"));
            Assert.Equal(65535, m.LogCodeCount("w722"));
        }

        [Fact]
        /// <summary>A delivered health check takes away only what it reported; a request refused meanwhile is kept.</summary>
        public void ClearSentValues_KeepsWhatCameMeanwhile()
        {
            ModuleHealthCheck m = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            m.RecordUsage("events", "record");
            m.RecordUsage("views", "start");
            m.RecordLogCode("w720", "events");
            m.LogWarning();
            m.LogFailedNetworkRequest(503, "busy");
            HealthSnapshot sent = m.Snapshot();

            m.RecordUsage("events", "record");
            m.LogWarning();
            m.LogFailedNetworkRequest(500, "down");
            m.ClearSentValues(sent);

            Assert.Equal(1, m.UsageCount("events", "record"));
            Assert.Equal(0, m.UsageCount("views", "start"));
            Assert.Equal(0, m.LogCodeCount("w720"));
            Assert.Equal(1, m.WarningCount);
            Assert.Equal(500, m.LastStatusCode);
            Assert.Equal("down", m.LastErrorMessage);

            ModuleHealthCheck reloaded = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            Assert.Equal(1, reloaded.UsageCount("events", "record"));
            Assert.Equal(500, reloaded.LastStatusCode);
        }

        [Fact]
        /// <summary>Stored entries of the wrong type are skipped, the usable ones are read.</summary>
        public void Load_SkipsEntriesOfTheWrongType()
        {
            Storage.Instance.SaveToFile<HealthCheckEntity>(ModuleHealthCheck.healthCheckFilename,
                new HealthCheckEntity { Json = "{\"LErr\":\"many\",\"LWar\":2,\"RStatC\":null,\"MU\":{\"events\":{\"record\":\"x\",\"start\":2},\"payments\":{\"pay\":1}},\"LC\":{\"w701\":-1,\"w720\":3}}" }).Wait();

            ModuleHealthCheck m = new ModuleHealthCheck(null, () => TestHelper.SERVER_URL, false, TestHelper.APP_VERSION);
            Assert.Equal(0, m.ErrorCount);
            Assert.Equal(2, m.WarningCount);
            Assert.Equal(-1, m.LastStatusCode);
            Assert.Equal(0, m.UsageCount("events", "record"));
            Assert.Equal(2, m.UsageCount("events", "start"));
            Assert.Equal(0, m.UsageCount("payments", "pay"));
            Assert.Equal(0, m.LogCodeCount("w701"));
            Assert.Equal(3, m.LogCodeCount("w720"));
        }

        [Fact]
        /// <summary>Only a queued request the server refuses fills in the status code and message; a refused health check changes nothing.</summary>
        public void FailedRequests_OnlyQueuedOnesAreRecorded()
        {
            MockHttpServer server = new MockHttpServer(
                (body) => body.Contains("events=") ? "{\"result\":\"refused\"}" : null,
                (body) => body.Contains("events=") ? 500 : (body.Contains("hc=") ? 503 : 200));
            Countly.Instance.Init(ConfigFor(server)).Wait();

            ModuleHealthCheck m = Countly.Instance.moduleHealthCheck;
            Assert.Contains(Reqs(server), IsHc);
            Assert.Equal(-1, m.LastStatusCode);
            Assert.Equal(0, m.ErrorCount);

            Countly.RecordEvent("refused").Wait();
            Assert.Equal(500, m.LastStatusCode);
            Assert.Equal("{\"result\":\"refused\"}", m.LastErrorMessage);
            server.Dispose();
        }

        [Fact]
        /// <summary>The health check carries the method usage and log codes, and a delivered one takes them away.</summary>
        public void HealthCheck_SendsMethodUsageAndLogCodes()
        {
            Storage.Instance.SaveToFile<HealthCheckEntity>(ModuleHealthCheck.healthCheckFilename,
                new HealthCheckEntity { Json = "{\"MU\":{\"views\":{\"start\":4}},\"LC\":{\"w710\":2}}" }).Wait();

            MockHttpServer server = new MockHttpServer((body) =>
                body.Contains("method=sc") ? "{\"v\":1,\"t\":1,\"c\":{\"tracking\":true}}" : "{\"result\":\"Success\"}");
            Countly.Instance.Init(ConfigFor(server)).Wait();

            JObject hcObj = JObject.Parse(TestHelper.GetParams(Reqs(server).First(IsHc).Body)["hc"]);
            Assert.Equal(4, hcObj["fu"]["views"].Value<int>("start"));
            Assert.Equal(2, hcObj["lc"].Value<int>("w710"));
            Assert.Equal(0, Countly.Instance.moduleHealthCheck.UsageCount("views", "start"));
            Assert.Equal(0, Countly.Instance.moduleHealthCheck.LogCodeCount("w710"));
            server.Dispose();
        }
    }
}
