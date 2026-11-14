using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CountlySDK;
using CountlySDK.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace TestProject_common
{
    /// <summary>
    /// In each request scenario the SDK sends the requests api/requests/[scenario].json records, with the values that
    /// change from run to run masked. The scenarios make the calls the other Countly SDKs make in their own request
    /// tests. A deliberate change: run these tests with the environment variable COUNTLY_UPDATE_REQUESTS=1, which writes
    /// the records, check the difference and add a line to CHANGELOG.md.
    /// </summary>
    public class RequestScenarioTests : IDisposable
    {
        private const string MASK = "<masked>";
        private static readonly string[] MaskedParams = { "timestamp", "hour", "dow", "tz", "sdk_version", "av", "rr", "checksum", "checksum256", "session_duration" };
        private static readonly string[] JsonParams = { "events", "metrics", "user_details", "crash", "consent", "hc" };
        private static readonly string[] MaskedEventFields = { "timestamp", "hour", "dow", "id", "pvid", "cvid", "peid" };
        // a view's duration, which this SDK sends in the segmentation
        private static readonly string[] MaskedViewSegments = { "dur" };
        private static readonly string[] MaskedCrashFields = { "_error", "_run", "_os", "_os_version", "_resolution", "_density", "_locale", "_device", "_device_type", "_manufacture", "_manufacturer", "_cpu", "_opengl", "_ram_current", "_ram_total", "_disk_current", "_disk_total", "_bat", "_orientation", "_root", "_online", "_muted", "_background" };

        /// <summary>
        /// Starts every scenario from an SDK that is not initialized and has no stored data.
        /// </summary>
        public RequestScenarioTests()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Instance.HaltInternal().Wait();
            TestHelper.CleanDataFiles();
            Countly.Instance.deferUpload = false;
        }

        /// <summary>
        /// Stops the SDK and removes its stored data.
        /// </summary>
        public void Dispose()
        {
            Countly.Instance.HaltInternal().Wait();
            TestHelper.CleanDataFiles();
        }

        [Fact]
        /// <summary>A session with an event of every segmentation value type the SDK takes, the user details and a session update.</summary>
        public async Task Parity()
        {
            await AssertRequests("parity", Config(), async server => {
                await Countly.Instance.SessionBegin();
                Segmentation segmentation = new Segmentation();
                segmentation.Add("nar", "uto");
                segmentation.Add("sasuke", "2147483647");
                segmentation.Add("sakura", "false");
                segmentation.Add("itachi", "1.7976931348623157e+308");
                segmentation.Add("kakaski", "5e-324");
                segmentation.Add("hashirama", "-2147483647");
                await Countly.RecordEvent("purchase", 2, 9.99, 5, segmentation);
                Countly.UserDetails.Name = "John Doe";
                Countly.UserDetails.Username = "john_d";
                Countly.UserDetails.Email = "john@example.com";
                Countly.UserDetails.Organization = "Countly";
                Countly.UserDetails.Phone = "1234567890";
                Countly.UserDetails.Picture = "https://example.com/picture.jpg";
                Countly.UserDetails.Gender = "M";
                Countly.UserDetails.BirthYear = 1980;
                Countly.UserDetails.Custom.Add("tier", "gold");
                Countly.UserDetails.Custom.Add("age", "30");
                Countly.UserDetails.Save();
                await QueuedUserDetails();
                await Countly.Instance.SessionUpdate(60);
                await Countly.Instance.SessionEnd();
            });
        }

        [Fact]
        /// <summary>Two views in a session, the first with segmentation.</summary>
        public async Task Views()
        {
            await AssertRequests("views", Config(), async server => {
                await Countly.Instance.SessionBegin();
                await Countly.Instance.RecordView("Home", TestHelper.Segm("source", "scenario"));
                await HoldingUploads(() => Countly.Instance.RecordView("Settings"));
                await HoldingUploads(() => Countly.Instance.SessionEnd());
            });
        }

        [Fact]
        /// <summary>A handled exception with a breadcrumb and custom details.</summary>
        public async Task Crash()
        {
            await AssertRequests("crash", Config(), async server => {
                Countly.AddBreadCrumb("breadcrumb one");
                await Countly.RecordException("parity crash", "parity crash\n  at the scenario", new Dictionary<string, string> { { "tier", "gold" } }, false);
            });
        }

        [Fact]
        /// <summary>Consent given for sessions and events, then taken back for events.</summary>
        public async Task Consent()
        {
            CountlyConfig config = Config();
            config.consentRequired = true;
            await AssertRequests("consent", config, async server => {
                await Countly.Instance.SetConsent(new Dictionary<ConsentFeatures, bool> { { ConsentFeatures.Sessions, true }, { ConsentFeatures.Events, true } });
                await Countly.Instance.SessionBegin();
                await Countly.RecordEvent("consented");
                await Settle(server);
                await Countly.Instance.SetConsent(new Dictionary<ConsentFeatures, bool> { { ConsentFeatures.Events, false } });
                await Countly.RecordEvent("not_consented");
                await Countly.Instance.SessionEnd();
            });
        }

        [Fact]
        /// <summary>A device ID change with merge, then one without.</summary>
        public async Task DeviceId()
        {
            await AssertRequests("device-id", Config(), async server => {
                await Countly.Instance.SessionBegin();
                await Countly.Instance.ChangeDeviceId("parity-device-002", true);
                await Countly.RecordEvent("after_merge");
                await Settle(server);
                await Countly.Instance.ChangeDeviceId("parity-device-003", false);
                await Countly.RecordEvent("after_new_id");
                await Countly.Instance.SessionEnd();
            });
        }

        /// <summary>
        /// The init config every scenario starts from: the parity app key and device ID.
        /// </summary>
        /// <returns>the config</returns>
        private static CountlyConfig Config()
        {
            return new CountlyConfig() { appKey = "parity-app-key", appVersion = "1.0", developerProvidedDeviceId = "parity-device-001" };
        }

        /// <summary>
        /// Makes a call that also records the end of the open view, which the SDK does from a callback that is not
        /// awaited, so that its upload would race the call's own. The uploads wait until the call returns and then go
        /// out together, in the SDK's own order.
        /// </summary>
        /// <param name="call">the SDK call</param>
        /// <returns>a task that completes when the upload is done</returns>
        private static async Task HoldingUploads(Func<Task> call)
        {
            Countly.Instance.deferUpload = true;
            try {
                await call();
            } finally {
                Countly.Instance.deferUpload = false;
            }
            await Countly.Instance.Upload();
        }

        /// <summary>
        /// Waits until the user details a save records are in the request queue, which a callback that is not awaited
        /// puts there.
        /// </summary>
        /// <returns>a task that completes when the request is queued</returns>
        private static async Task QueuedUserDetails()
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(5);
            while (!Countly.Instance.StoredRequests.ToArray().Any(request => request.Request.Contains("user_details=")) && DateTime.UtcNow < deadline) {
                await Task.Delay(10);
            }
        }

        /// <summary>
        /// Waits until the SDK has sent everything it queued: every queue is empty and the server got no request for half
        /// a second.
        /// </summary>
        /// <param name="server">the server the SDK sends to</param>
        /// <returns>a task that completes when the SDK is idle</returns>
        private static async Task Settle(MockHttpServer server)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(15);
            int count = -1;
            DateTime quietSince = DateTime.UtcNow;
            while (DateTime.UtcNow < deadline) {
                await Task.Delay(50);
                int queued = Countly.Instance.StoredRequests.Count + Countly.Instance.Events.Count + Countly.Instance.Sessions.Count + Countly.Instance.Exceptions.Count;
                int received = server.Requests.Count;
                if (queued > 0 || received != count) {
                    count = received;
                    quietSince = DateTime.UtcNow;
                } else if (DateTime.UtcNow - quietSince > TimeSpan.FromMilliseconds(500)) {
                    return;
                }
            }
            Assert.True(false, "the SDK still had requests to send after 15 seconds");
        }

        /// <summary>
        /// Runs a scenario on a new SDK instance and compares the requests it sent with the record of the scenario, or
        /// writes the record when COUNTLY_UPDATE_REQUESTS is set.
        /// </summary>
        /// <param name="name">scenario name, the record file name without .json</param>
        /// <param name="config">init config, which gets the server's URL</param>
        /// <param name="scenario">the calls, given the server</param>
        /// <returns>a task that completes when the requests are compared</returns>
        private static async Task AssertRequests(string name, CountlyConfig config, Func<MockHttpServer, Task> scenario)
        {
            string current;
            using (MockHttpServer server = new MockHttpServer()) {
                config.serverUrl = server.Url;
                await Countly.Instance.Init(config);
                await scenario(server);
                await Settle(server);
                current = Report(server.Requests);
            }

            string file = Path.Combine(RecordsDirectory(), name + ".json");
            if (Environment.GetEnvironmentVariable("COUNTLY_UPDATE_REQUESTS") != null) {
                Directory.CreateDirectory(RecordsDirectory());
                File.WriteAllText(file, current, new UTF8Encoding(false));
                return;
            }
            string committed = File.Exists(file) ? File.ReadAllText(file).Replace("\r\n", "\n") : "(no api/requests/" + name + ".json)\n";
            Assert.True(committed == current, "the requests of the " + name + " scenario differ from api/requests/" + name + ".json. If that is deliberate, run these tests with COUNTLY_UPDATE_REQUESTS=1, check the difference and add a line to CHANGELOG.md.\n" + FirstDifference(committed, current));
        }

        /// <summary>
        /// The api/requests folder of the repository, found from the location of this source file.
        /// </summary>
        /// <param name="sourceFile">filled in by the compiler</param>
        /// <returns>the folder's path</returns>
        private static string RecordsDirectory([CallerFilePath] string sourceFile = "")
        {
            return Path.Combine(Path.GetDirectoryName(sourceFile), "..", "..", "api", "requests");
        }

        /// <summary>
        /// Describes where a record and the requests the SDK sent part: the first line that differs.
        /// </summary>
        /// <param name="record">content of the record</param>
        /// <param name="current">the record of the requests the SDK sent</param>
        /// <returns>the line number with both versions of the line</returns>
        private static string FirstDifference(string record, string current)
        {
            string[] recorded = record.Split('\n');
            string[] sent = current.Split('\n');
            for (int i = 0; i < Math.Max(recorded.Length, sent.Length); i++) {
                string recordedLine = i < recorded.Length ? recorded[i] : "(end)";
                string sentLine = i < sent.Length ? sent[i] : "(end)";
                if (recordedLine != sentLine) {
                    return "line " + (i + 1) + ": the record has [" + recordedLine + "], the SDK sent [" + sentLine + "]";
                }
            }
            return "";
        }

        /// <summary>
        /// The record of the requests the server received: each with its path and its parameters, the values that change
        /// from run to run masked, the JSON parameters parsed and the keys of every object sorted.
        /// </summary>
        /// <param name="requests">the requests, in the order they arrived</param>
        /// <returns>file content</returns>
        private static string Report(IList<MockHttpServer.RequestInfo> requests)
        {
            JArray report = new JArray();
            foreach (MockHttpServer.RequestInfo request in requests) {
                Dictionary<string, string> sentParams = new Dictionary<string, string>(request.Params);
                if (request.Path.Contains("?")) {
                    foreach (KeyValuePair<string, string> param in TestHelper.GetParams(request.Path)) {
                        sentParams[param.Key] = param.Value;
                    }
                }
                JObject parameters = new JObject();
                foreach (KeyValuePair<string, string> param in sentParams.OrderBy(p => p.Key, StringComparer.Ordinal)) {
                    if (MaskedParams.Contains(param.Key)) {
                        parameters[param.Key] = MASK;
                    } else if (JsonParams.Contains(param.Key) && !string.IsNullOrEmpty(param.Value)) {
                        parameters[param.Key] = MaskedJson(param.Key, JToken.Parse(param.Value));
                    } else {
                        parameters[param.Key] = param.Value;
                    }
                }
                report.Add(new JObject { ["path"] = request.Path.Split('?')[0], ["params"] = parameters });
            }
            return report.ToString(Formatting.Indented).Replace("\r\n", "\n") + "\n";
        }

        /// <summary>
        /// The value of a JSON parameter with the values that change from run to run masked and the keys of every object
        /// sorted.
        /// </summary>
        /// <param name="name">parameter name</param>
        /// <param name="value">parsed parameter value</param>
        /// <returns>the masked value</returns>
        private static JToken MaskedJson(string name, JToken value)
        {
            if ((name == "metrics" || name == "hc") && value is JObject metrics) {
                return Masked(metrics, metrics.Properties().Select(p => p.Name).ToArray());
            }
            if (name == "events" && value is JArray events) {
                return new JArray(events.Select(MaskedEvent));
            }
            if (name == "crash" && value is JObject crash) {
                return Masked(crash, MaskedCrashFields);
            }
            return Sorted(value);
        }

        /// <summary>
        /// An event with the values that change from run to run masked, for a view also in its segmentation.
        /// </summary>
        /// <param name="value">the event</param>
        /// <returns>the masked event</returns>
        private static JToken MaskedEvent(JToken value)
        {
            if (!(value is JObject sentEvent)) {
                return Sorted(value);
            }
            if ((string)sentEvent["key"] != "[CLY]_view") {
                return Masked(sentEvent, MaskedEventFields);
            }
            JObject view = Masked(sentEvent, MaskedEventFields.Concat(new[] { "dur" }).ToArray());
            if (view["segmentation"] is JObject segmentation) {
                view["segmentation"] = Masked(segmentation, MaskedViewSegments);
            }
            return view;
        }

        /// <summary>
        /// A copy of an object with the values of the given keys masked and the keys of every object sorted.
        /// </summary>
        /// <param name="value">object</param>
        /// <param name="keys">keys to mask</param>
        /// <returns>the masked copy</returns>
        private static JObject Masked(JObject value, string[] keys)
        {
            JObject copy = new JObject();
            foreach (JProperty property in value.Properties().OrderBy(p => p.Name, StringComparer.Ordinal)) {
                copy[property.Name] = keys.Contains(property.Name) ? new JValue(MASK) : Sorted(property.Value);
            }
            return copy;
        }

        /// <summary>
        /// A copy of a JSON value with the keys of every object sorted.
        /// </summary>
        /// <param name="value">JSON value</param>
        /// <returns>the sorted copy</returns>
        private static JToken Sorted(JToken value)
        {
            if (value is JObject obj) {
                return Masked(obj, new string[0]);
            }
            if (value is JArray array) {
                return new JArray(array.Select(Sorted));
            }
            return value.DeepClone();
        }
    }
}
