using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CountlySDK;
using CountlySDK.CountlyCommon;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using Xunit;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace TestProject_common
{
    /// <summary>
    /// Changing the server URL and the app key after init, and the queue operations
    /// </summary>
    public class ServerUrlAndAppKeyTests : IDisposable
    {
        private const string NEW_APP_KEY = "NEW_APP_KEY";
        private const string VIEW_EVENT_KEY = "[CLY]_view";

        /// <summary>
        /// Test setup
        /// </summary>
        public ServerUrlAndAppKeyTests()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Instance.HaltInternal().Wait();
            TestHelper.CleanDataFiles();
            Countly.Instance.deferUpload = false;
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {
            Countly.Instance.HaltInternal().Wait();
        }

        [Fact]
        /// <summary>
        /// Requests queued while the first server is down and every later request go to the new server with the same
        /// app key. Trailing slashes of the new URL are removed, so the paths are not doubled.
        /// </summary>
        public async Task ChangeServerUrl_QueuedAndLaterRequestsGoToTheNewServer()
        {
            using (MockHttpServer serverA = new MockHttpServer(null, body => 503))
            using (MockHttpServer serverB = new MockHttpServer()) {
                await Countly.Instance.Init(ConfigFor(serverA));
                await Countly.Instance.SessionBegin();
                await Countly.RecordEvent("before_change");
                Assert.Single(Countly.Instance.StoredRequests);
                Assert.Single(Countly.Instance.Events);
                int requestsAtA = serverA.Requests.Count;

                await Countly.Instance.ChangeServerUrl(serverB.Url + "//");
                await Countly.RecordEvent("after_change");

                Assert.True(WaitFor(() => Countly.Instance.StoredRequests.Count == 0 && Countly.Instance.Events.Count == 0));
                List<MockHttpServer.RequestInfo> sentToB = TestHelper.NonServerConfigRequests(serverB);
                Assert.Single(sentToB, request => request.Params.ContainsKey("begin_session"));
                Assert.Equal(new[] { "before_change", "after_change" }, EventKeys(sentToB));
                Assert.All(sentToB, request => Assert.Equal("/i", request.Path));
                Assert.All(sentToB, request => Assert.Equal(TestHelper.APP_KEY, request.Params["app_key"]));
                Assert.Equal(requestsAtA, serverA.Requests.Count);
            }
        }

        [Fact]
        /// <summary>
        /// After a server URL change the consent state, the behavior settings fetch and the remote config download go to
        /// the new server, and so do the feedback widget list, the feedback widget URL and the content fetch
        /// </summary>
        public async Task ChangeServerUrl_CatchUpAndDirectRequestsUseTheNewServer()
        {
            using (MockHttpServer serverA = new MockHttpServer())
            using (MockHttpServer serverB = new MockHttpServer()) {
                CountlyConfig cc = ConfigFor(serverA);
                cc.consentRequired = true;
                cc.givenConsent = new Dictionary<ConsentFeatures, bool> {
                    { ConsentFeatures.RemoteConfig, true }, { ConsentFeatures.Feedback, true }, { ConsentFeatures.Content, true }
                };
                cc.EnableRemoteConfigAutomaticTriggers();
                await Countly.Instance.Init(cc);
                int requestsAtA = serverA.Requests.Count;

                await Countly.Instance.ChangeServerUrl(serverB.Url);

                Assert.True(WaitFor(() => serverB.Requests.Any(request => request.Params.ContainsKey("consent"))));
                Assert.Equal("/i", serverB.Requests.First(request => request.Params.ContainsKey("consent")).Path);
                Assert.Contains(serverB.Requests, request => request.Path == "/o/sdk" && Param(request, "method") == "sc");
                Assert.Contains(serverB.Requests, request => request.Path == "/o/sdk" && Param(request, "method") == "rc");

                await Countly.Instance.Feedback().GetAvailableFeedbackWidgets();
                Assert.Contains(serverB.Requests, request => request.Path == "/o/sdk" && Param(request, "method") == "feedback");
                CountlyFeedbackWidget widget = new CountlyFeedbackWidget { widgetId = "widget", type = FeedbackWidgetType.nps };
                string widgetUrl = await Countly.Instance.Feedback().ConstructFeedbackWidgetUrl(widget);
                Assert.StartsWith(serverB.Url + "feedback/nps?widget_id=widget", widgetUrl);
                Assert.Contains("app_key=" + TestHelper.APP_KEY, widgetUrl);

                Countly.Instance.SetContentDisplay(new TestContentDisplay());
                Countly.Instance.Content().PreviewContent("content");
                Assert.True(WaitFor(() => serverB.Requests.Any(request => request.Path == "/o/sdk/content")));

                Assert.All(serverB.Requests, request => Assert.Equal(TestHelper.APP_KEY, request.Params["app_key"]));
                Assert.Equal(requestsAtA, serverA.Requests.Count);
            }
        }

        [Fact]
        /// <summary>
        /// Empty, relative, non http and host-less URLs are ignored with the "e102" log code, and nothing follows them
        /// </summary>
        public async Task ChangeServerUrl_InvalidUrlsAreIgnored()
        {
            using (MockHttpServer server = new MockHttpServer()) {
                await Countly.Instance.Init(ConfigFor(server));
                int behaviorSettingsFetches = server.Requests.Count(request => Param(request, "method") == "sc");
                string[] invalidUrls = { null, "", " ", "not a url", "ftp://example.com", "http://", "https:///", "/relative/path" };

                foreach (string invalidUrl in invalidUrls) {
                    await Countly.Instance.ChangeServerUrl(invalidUrl);
                }
                await Countly.RecordEvent("after_invalid_urls");

                Assert.True(WaitFor(() => EventKeys(server.Requests).Contains("after_invalid_urls")));
                Assert.Equal(behaviorSettingsFetches, server.Requests.Count(request => Param(request, "method") == "sc"));
                Assert.Equal(invalidUrls.Length, LogCodeCount("e102"));
            }
        }

        [Fact]
        /// <summary>
        /// A new init uses the server URL and the app key of the config again, and the config object keeps them
        /// </summary>
        public async Task ChangeServerUrlAndAppKey_NextInitUsesTheConfigAgain()
        {
            using (MockHttpServer serverA = new MockHttpServer())
            using (MockHttpServer serverB = new MockHttpServer()) {
                CountlyConfig cc = ConfigFor(serverA);
                await Countly.Instance.Init(cc);
                string configUrl = cc.serverUrl;
                await Countly.Instance.ChangeServerUrl(serverB.Url);
                await Countly.Instance.ChangeAppKey(NEW_APP_KEY);
                await Countly.Instance.SessionBegin();
                Assert.True(WaitFor(() => serverB.Requests.Any(request => request.Params.ContainsKey("begin_session"))));
                Assert.Equal(NEW_APP_KEY, serverB.Requests.First(request => request.Params.ContainsKey("begin_session")).Params["app_key"]);
                Assert.Equal(configUrl, cc.serverUrl);
                Assert.Equal(TestHelper.APP_KEY, cc.appKey);

                await Countly.Instance.HaltInternal(false);
                int requestsAtB = serverB.Requests.Count;
                await Countly.Instance.Init(cc);
                await Countly.Instance.SessionBegin();

                Assert.True(WaitFor(() => serverA.Requests.Any(request => request.Params.ContainsKey("begin_session"))));
                Assert.Equal(TestHelper.APP_KEY, serverA.Requests.First(request => request.Params.ContainsKey("begin_session")).Params["app_key"]);
                Assert.Equal(requestsAtB, serverB.Requests.Count);
            }
        }

        [Fact]
        /// <summary>
        /// Before the switch, the running view is ended, events, crash reports and pending user details are queued and
        /// the session is ended, all with the old app key; then a session begins with the new app key. Timed events,
        /// the device ID and the config object stay as they are.
        /// </summary>
        public async Task ChangeAppKey_EndsTheOldAppAndBeginsTheNewOne()
        {
            using (MockHttpServer server = new MockHttpServer()) {
                CountlyConfig cc = ConfigFor(server);
                await Countly.Instance.Init(cc);
                Countly.Instance.deferUpload = true;
                await Countly.Instance.SessionBegin();
                Countly.Instance.StartEvent("timed");
                await Countly.Instance.RecordView("Home");
                await Countly.RecordEvent("pending");
                await Countly.RecordException("old app crash");
                Countly.UserDetails.Name = "Ann";
                Countly.Instance.StoredRequests.Clear();

                await Countly.Instance.ChangeAppKey(NEW_APP_KEY);

                List<Dictionary<string, string>> queue = QueuedParams();
                Assert.Equal(5, queue.Count);
                Assert.Contains("Ann", queue[0]["user_details"]);
                List<CountlyEvent> events = JsonConvert.DeserializeObject<List<CountlyEvent>>(queue[1]["events"]);
                Assert.Equal(new[] { VIEW_EVENT_KEY, "pending", VIEW_EVENT_KEY }, events.Select(e => e.Key));
                Assert.Equal("Home", events[2].segmentation["name"]);
                Assert.True(events[2].segmentation.ContainsKey("dur"));
                Assert.Contains("old app crash", queue[2]["crash"]);
                Assert.Equal("1", queue[3]["end_session"]);
                Assert.Equal("1", queue[4]["begin_session"]);
                Assert.All(queue.Take(4), request => Assert.Equal(TestHelper.APP_KEY, request["app_key"]));
                Assert.Equal(NEW_APP_KEY, queue[4]["app_key"]);
                Assert.All(queue, request => Assert.Equal(TestHelper.DEVICE_ID, request["device_id"]));

                Assert.Empty(Countly.Instance.Events);
                Assert.Empty(Countly.Instance.Exceptions);
                Assert.True(Countly.Instance.TimedEvents.ContainsKey("timed"));
                Assert.Equal(TestHelper.APP_KEY, cc.appKey);
                Assert.Contains(server.Requests, request => Param(request, "method") == "sc" && Param(request, "app_key") == NEW_APP_KEY);

                await Countly.Instance.SetLocation("1.0,2.0");
                Assert.Equal(NEW_APP_KEY, QueuedParams()[5]["app_key"]);
            }
        }

        [Fact]
        /// <summary>
        /// Invalid keys are ignored with the "e103" log code, the current key is ignored, and without a running session
        /// no session is ended or begun; the pending events still go with the old app key
        /// </summary>
        public async Task ChangeAppKey_InvalidOrSameKeysAreIgnoredAndNoSessionBeginsWithoutOne()
        {
            using (MockHttpServer server = new MockHttpServer()) {
                await Countly.Instance.Init(ConfigFor(server));
                Countly.Instance.deferUpload = true;
                await Countly.Instance.SessionBegin();
                Countly.Instance.StoredRequests.Clear();

                string[] invalidKeys = { null, "", "   ", "YOUR_APP_KEY" };
                foreach (string invalidKey in invalidKeys) {
                    await Countly.Instance.ChangeAppKey(invalidKey);
                }
                await Countly.Instance.ChangeAppKey(TestHelper.APP_KEY);
                Assert.Empty(Countly.Instance.StoredRequests);
                Assert.Equal(invalidKeys.Length, LogCodeCount("e103"));

                await Countly.Instance.SessionEnd();
                await Countly.RecordEvent("pending");
                Countly.Instance.StoredRequests.Clear();
                await Countly.Instance.ChangeAppKey(NEW_APP_KEY);

                List<Dictionary<string, string>> queue = QueuedParams();
                Assert.Single(queue);
                Assert.Equal(TestHelper.APP_KEY, queue[0]["app_key"]);
                Assert.Equal("pending", JsonConvert.DeserializeObject<List<CountlyEvent>>(queue[0]["events"]).Single().Key);

                await Countly.Instance.SetLocation("1.0,2.0");
                Assert.Equal(NEW_APP_KEY, QueuedParams()[1]["app_key"]);
                Assert.DoesNotContain(QueuedParams(), request => request.ContainsKey("begin_session") || request.ContainsKey("end_session"));
            }
        }

        [Fact]
        /// <summary>
        /// The remote config values of the old app are removed and the new app's values are downloaded, and the
        /// consent state is sent with the new app key
        /// </summary>
        public async Task ChangeAppKey_ReplacesRemoteConfigValuesAndSendsConsent()
        {
            string failingKey = "FAILING_APP_KEY";
            using (MockHttpServer server = new MockHttpServer(body => {
                if (!body.Contains("method=rc")) {
                    return null;
                }
                return body.Contains("app_key=" + NEW_APP_KEY) ? "{\"size\":\"big\"}" : "{\"color\":\"red\"}";
            }, body => body.Contains("method=rc") && body.Contains("app_key=" + failingKey) ? 500 : 200)) {
                CountlyConfig cc = ConfigFor(server);
                cc.consentRequired = true;
                cc.givenConsent = new Dictionary<ConsentFeatures, bool> { { ConsentFeatures.RemoteConfig, true }, { ConsentFeatures.Sessions, true } };
                cc.EnableRemoteConfigAutomaticTriggers();
                await Countly.Instance.Init(cc);
                Assert.Equal("red", Countly.Instance.RemoteConfig().GetValue("color").Value);

                await Countly.Instance.ChangeAppKey(NEW_APP_KEY);

                IDictionary<string, RCData> values = Countly.Instance.RemoteConfig().GetValues();
                Assert.Equal(new[] { "size" }, values.Keys);
                Assert.Equal("big", values["size"].Value);
                Assert.True(WaitFor(() => server.Requests.Any(request => request.Params.ContainsKey("consent") && Param(request, "app_key") == NEW_APP_KEY)));

                await Countly.Instance.ChangeAppKey(failingKey);
                Assert.Empty(Countly.Instance.RemoteConfig().GetValues());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        /// <summary>
        /// Without a view or a session, pending user details and a pending custom property modifier are queued with the old
        /// app key, whether the user details are sent automatically or not
        /// </summary>
        public async Task ChangeAppKey_QueuesPendingUserDetailsWithTheOldKey(bool autoSendUserDetails)
        {
            using (MockHttpServer server = new MockHttpServer()) {
                CountlyConfig cc = ConfigFor(server);
                if (!autoSendUserDetails) {
                    cc.DisableAutoSendUserDetails();
                }
                await Countly.Instance.Init(cc);
                Countly.Instance.deferUpload = true;
                Countly.UserDetails.Name = "Ann";
                Countly.UserDetails.Increment("visits");
                Assert.Empty(Countly.Instance.StoredRequests);

                await Countly.Instance.ChangeAppKey(NEW_APP_KEY);

                List<Dictionary<string, string>> queue = QueuedParams();
                Assert.Contains(queue, request => request.ContainsKey("user_details") && request["user_details"].Contains("\"name\":\"Ann\""));
                Assert.Contains(queue, request => request.ContainsKey("user_details") && request["user_details"].Contains("\"visits\":{\"$inc\":1}"));
                Assert.All(queue, request => Assert.Equal(TestHelper.APP_KEY, request["app_key"]));
                Assert.DoesNotContain(queue, request => request.ContainsKey("begin_session") || request.ContainsKey("end_session"));
                Assert.False(Countly.UserDetails.isChanged);

                await Countly.Instance.SetLocation("1.0,2.0");
                Assert.Equal(NEW_APP_KEY, QueuedParams().Last()["app_key"]);
            }
        }

        [Fact]
        /// <summary>
        /// Events that are being sent while the app key changes keep the old app key when their send fails and they are sent again
        /// </summary>
        public async Task ChangeAppKey_EventsBeingSentKeepTheOldKeyWhenTheirSendFails()
        {
            int attempts = 0;
            using (ManualResetEvent release = new ManualResetEvent(false))
            using (MockHttpServer server = new MockHttpServer(body => {
                if (body.Contains("held_event") && Interlocked.Increment(ref attempts) == 1) {
                    release.WaitOne(10000);
                }
                return null;
            }, body => body.Contains("held_event") && attempts == 1 ? 503 : 200)) {
                CountlyConfig cc = ConfigFor(server);
                cc.DisableSDKBehaviorSettingsUpdates();
                await Countly.Instance.Init(cc);
                Task recording = Countly.RecordEvent("held_event");
                Assert.True(WaitFor(() => server.Requests.Any(request => request.Body.Contains("held_event"))));

                await Countly.Instance.ChangeAppKey(NEW_APP_KEY);
                Assert.Empty(Countly.Instance.StoredRequests);
                release.Set();
                await recording;
                await Countly.Instance.AttemptToSendStoredRequests();

                List<MockHttpServer.RequestInfo> sent = server.Requests.Where(request => request.Body.Contains("held_event")).ToList();
                Assert.Equal(2, sent.Count);
                Assert.All(sent, request => Assert.Equal(TestHelper.APP_KEY, request.Params["app_key"]));
                Assert.Empty(Countly.Instance.Events);
                Assert.Empty(Countly.Instance.StoredRequests);
            }
        }

        [Fact]
        /// <summary>
        /// A crash report that is being sent while the app key changes is sent once, with the old app key
        /// </summary>
        public async Task ChangeAppKey_CrashReportBeingSentIsSentOnce()
        {
            using (ManualResetEvent release = new ManualResetEvent(false))
            using (MockHttpServer server = new MockHttpServer(body => {
                if (body.Contains("held_crash")) {
                    release.WaitOne(10000);
                }
                return null;
            })) {
                CountlyConfig cc = ConfigFor(server);
                cc.DisableSDKBehaviorSettingsUpdates();
                await Countly.Instance.Init(cc);
                Task recording = Countly.RecordException("held_crash");
                Assert.True(WaitFor(() => server.Requests.Any(request => request.Body.Contains("held_crash"))));

                await Countly.Instance.ChangeAppKey(NEW_APP_KEY);
                release.Set();
                await recording;
                await Countly.Instance.AttemptToSendStoredRequests();

                MockHttpServer.RequestInfo sent = Assert.Single(server.Requests.Where(request => request.Body.Contains("held_crash")));
                Assert.Equal(TestHelper.APP_KEY, sent.Params["app_key"]);
                Assert.Empty(Countly.Instance.Exceptions);
                Assert.Empty(Countly.Instance.StoredRequests);
            }
        }

        [Fact]
        /// <summary>
        /// Every queued request gets the current app key, also one with a leading "/i?", in memory and in storage, and the
        /// checksum computed at send time matches the changed request
        /// </summary>
        public async Task ReplaceAllAppKeysInQueueWithCurrentAppKey_MovesQueuedRequestsToTheCurrentApp()
        {
            using (MockHttpServer server = new MockHttpServer()) {
                CountlyConfig cc = ConfigFor(server);
                cc.SetParamaterTamperingProtectionSalt("salt");
                await Countly.Instance.Init(cc);
                Countly.Instance.deferUpload = true;
                await Countly.Instance.SessionBegin();
                await Countly.Instance.AddRequest("/i?app_key=" + TestHelper.APP_KEY + "&device_id=" + TestHelper.DEVICE_ID + "&legacy=1");
                await Countly.Instance.ChangeAppKey(NEW_APP_KEY);
                Assert.Equal(new[] { TestHelper.APP_KEY, TestHelper.APP_KEY, TestHelper.APP_KEY, NEW_APP_KEY }, QueuedParams().Select(request => request["app_key"]));

                Countly.Instance.ReplaceAllAppKeysInQueueWithCurrentAppKey();

                Assert.All(QueuedParams(), request => Assert.Equal(NEW_APP_KEY, request["app_key"]));
                Assert.Equal("/i?app_key=" + NEW_APP_KEY + "&device_id=" + TestHelper.DEVICE_ID + "&legacy=1", Countly.Instance.StoredRequests.ElementAt(1).Request);
                Queue<StoredRequest> stored = await Storage.Instance.LoadFromFile<Queue<StoredRequest>>(Countly.storedRequestsFilename);
                Assert.Equal(Countly.Instance.StoredRequests.Select(request => request.Request), stored.Select(request => request.Request));

                Countly.Instance.deferUpload = false;
                await Countly.Instance.AttemptToSendStoredRequests();

                Assert.Empty(Countly.Instance.StoredRequests);
                List<MockHttpServer.RequestInfo> sent = TestHelper.NonServerConfigRequests(server);
                Assert.Equal(4, sent.Count);
                foreach (MockHttpServer.RequestInfo request in sent) {
                    Assert.Equal(NEW_APP_KEY, request.Params["app_key"]);
                    int checksumIndex = request.Body.IndexOf("&checksum256=", StringComparison.Ordinal);
                    Assert.Equal(Sha256Hex(request.Body.Substring(0, checksumIndex) + "salt"), request.Params["checksum256"]);
                }
            }
        }

        [Fact]
        /// <summary>
        /// Only the queued requests with exactly the current app key stay, in their order, in memory and in storage
        /// </summary>
        public async Task RemoveDifferentAppKeysFromQueue_DropsTheRequestsOfOtherApps()
        {
            using (MockHttpServer server = new MockHttpServer()) {
                await Countly.Instance.Init(ConfigFor(server));
                Countly.Instance.deferUpload = true;
                await Countly.Instance.SessionBegin();
                await Countly.Instance.AddRequest("/i?app_key=" + NEW_APP_KEY + "&device_id=" + TestHelper.DEVICE_ID + "&legacy=1");
                await Countly.Instance.AddRequest("app_key=" + NEW_APP_KEY + "_2&device_id=" + TestHelper.DEVICE_ID + "&longer_key=1");
                await Countly.Instance.AddRequest("device_id=" + TestHelper.DEVICE_ID + "&no_app_key=1");
                await Countly.Instance.ChangeAppKey(NEW_APP_KEY);

                Countly.Instance.RemoveDifferentAppKeysFromQueue();

                List<Dictionary<string, string>> queue = QueuedParams();
                Assert.Equal(2, queue.Count);
                Assert.Equal("1", queue[0]["legacy"]);
                Assert.Equal("1", queue[1]["begin_session"]);
                Assert.All(queue, request => Assert.Equal(NEW_APP_KEY, request["app_key"]));
                Queue<StoredRequest> stored = await Storage.Instance.LoadFromFile<Queue<StoredRequest>>(Countly.storedRequestsFilename);
                Assert.Equal(Countly.Instance.StoredRequests.Select(request => request.Request), stored.Select(request => request.Request));
            }
        }

        [Fact]
        /// <summary>
        /// The request, event and crash queues are emptied in memory and in storage, while timed events, the running view
        /// and pending user details stay, and recording goes on as before
        /// </summary>
        public async Task FlushQueues_RemovesUnsentDataAndKeepsTheRest()
        {
            using (MockHttpServer server = new MockHttpServer()) {
                await Countly.Instance.Init(ConfigFor(server));
                Countly.Instance.deferUpload = true;
                await Countly.Instance.SessionBegin();
                Countly.Instance.StartEvent("timed");
                await Countly.Instance.RecordView("Home");
                await Countly.RecordEvent("flushed");
                await Countly.RecordException("flushed crash");
                Countly.UserDetails.Name = "Ann";
                Assert.NotEmpty(Countly.Instance.StoredRequests);

                Countly.Instance.FlushQueues();

                Assert.Empty(Countly.Instance.StoredRequests);
                Assert.Empty(Countly.Instance.Events);
                Assert.Empty(Countly.Instance.Exceptions);
                Assert.Empty(await Storage.Instance.LoadFromFile<Queue<StoredRequest>>(Countly.storedRequestsFilename));
                Assert.Empty(await Storage.Instance.LoadFromFile<List<CountlyEvent>>(Countly.eventsFilename));
                Assert.Empty(await Storage.Instance.LoadFromFile<List<ExceptionEvent>>(Countly.exceptionsFilename));
                Assert.True(Countly.Instance.TimedEvents.ContainsKey("timed"));

                Countly.UserDetails.Save();
                Assert.Contains("Ann", QueuedParams().Single()["user_details"]);

                Countly.Instance.deferUpload = false;
                await Countly.Instance.RecordView("Next");
                Assert.True(WaitFor(() => Countly.Instance.Events.Count == 0 && Countly.Instance.StoredRequests.Count == 0));
                List<CountlyEvent> sentEvents = TestHelper.NonServerConfigRequests(server).Where(request => request.Params.ContainsKey("events"))
                    .SelectMany(request => JsonConvert.DeserializeObject<List<CountlyEvent>>(request.Params["events"])).ToList();
                Assert.Equal(new[] { "Home", "Next" }, sentEvents.Select(e => e.segmentation["name"]));
                Assert.True(sentEvents[0].segmentation.ContainsKey("dur"));
                Assert.DoesNotContain(EventKeys(server.Requests), key => key == "flushed");
            }
        }

        [Fact]
        /// <summary>
        /// The stored requests and events go out as soon as the call is made, once the server answers again
        /// </summary>
        public async Task AttemptToSendStoredRequests_SendsTheQueuesRightAway()
        {
            bool serverDown = true;
            using (MockHttpServer server = new MockHttpServer(null, body => serverDown ? 503 : 200)) {
                await Countly.Instance.Init(ConfigFor(server));
                await Countly.Instance.SessionBegin();
                await Countly.RecordEvent("queued");
                Assert.Single(Countly.Instance.StoredRequests);
                Assert.Single(Countly.Instance.Events);

                serverDown = false;
                await Countly.Instance.AttemptToSendStoredRequests();

                Assert.Empty(Countly.Instance.StoredRequests);
                Assert.Empty(Countly.Instance.Events);
                List<MockHttpServer.RequestInfo> sent = TestHelper.NonServerConfigRequests(server);
                Assert.Contains(sent, request => request.Params.ContainsKey("begin_session") && request.Params.ContainsKey("rr"));
                Assert.Contains("queued", EventKeys(sent));
            }
        }

        [Fact]
        /// <summary>
        /// Flushing the queues while a request is on its way neither stops it nor drops the request queued after the flush
        /// </summary>
        public async Task FlushQueues_WhileARequestIsOnItsWay_KeepsTheRequestQueuedAfterIt()
        {
            await AssertQueueOperationKeepsUnsentRequests(async () => Countly.Instance.FlushQueues(), TestHelper.APP_KEY);
        }

        [Fact]
        /// <summary>
        /// Removing the requests of the old app, the one on its way included, does not drop the request queued after it
        /// </summary>
        public async Task RemoveDifferentAppKeysFromQueue_WhileARequestIsOnItsWay_KeepsTheRequestQueuedAfterIt()
        {
            await AssertQueueOperationKeepsUnsentRequests(async () => {
                await Countly.Instance.ChangeAppKey(NEW_APP_KEY);
                Countly.Instance.RemoveDifferentAppKeysFromQueue();
            }, NEW_APP_KEY);
        }

        /// <summary>
        /// Holds a request on its way at the server, runs the queue operation and queues another request, then lets the
        /// server answer: the first request is sent once and the other one is sent after it with the expected app key
        /// </summary>
        private async Task AssertQueueOperationKeepsUnsentRequests(Func<Task> queueOperation, string appKeyAfter)
        {
            using (ManualResetEvent release = new ManualResetEvent(false))
            using (MockHttpServer server = new MockHttpServer(body => {
                if (body.Contains("city=on_its_way")) {
                    release.WaitOne(10000);
                }
                return null;
            })) {
                CountlyConfig cc = ConfigFor(server);
                cc.DisableSDKBehaviorSettingsUpdates();
                await Countly.Instance.Init(cc);
                Task onItsWay = Countly.Instance.SetLocation(null, null, null, "on_its_way");
                Assert.True(WaitFor(() => server.Requests.Any(request => request.Body.Contains("city=on_its_way"))));

                await queueOperation();
                await Countly.Instance.SetLocation(null, null, null, "after_operation");
                Assert.Single(Countly.Instance.StoredRequests);

                release.Set();
                await onItsWay;

                Assert.True(WaitFor(() => Countly.Instance.StoredRequests.Count == 0));
                Assert.Single(server.Requests, request => request.Body.Contains("city=on_its_way"));
                MockHttpServer.RequestInfo after = server.Requests.Single(request => request.Body.Contains("city=after_operation"));
                Assert.Equal(appKeyAfter, after.Params["app_key"]);
            }
        }

        /// <summary>
        /// Returns the default test configuration pointed at the given mock server
        /// </summary>
        private static CountlyConfig ConfigFor(MockHttpServer server)
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            return cc;
        }

        /// <summary>
        /// Polls, up to a deadline, until the condition holds, and tells whether it does
        /// </summary>
        private static bool WaitFor(Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(10);
            while (!condition() && DateTime.UtcNow < deadline) {
                Thread.Sleep(20);
            }
            return condition();
        }

        /// <summary>
        /// Returns the parameters of every queued request, in queue order
        /// </summary>
        private static List<Dictionary<string, string>> QueuedParams()
        {
            return Countly.Instance.StoredRequests.Select(request => TestHelper.GetParams(request.Request)).ToList();
        }

        /// <summary>
        /// Returns the value of a parameter of a request the mock server received, or null when the request has none
        /// </summary>
        private static string Param(MockHttpServer.RequestInfo request, string key)
        {
            return request.Params.TryGetValue(key, out string value) ? value : null;
        }

        /// <summary>
        /// Returns the keys of the events the given requests carry, in the order they were sent
        /// </summary>
        private static List<string> EventKeys(IEnumerable<MockHttpServer.RequestInfo> requests)
        {
            return requests.Where(request => request.Params.ContainsKey("events"))
                .SelectMany(request => JsonConvert.DeserializeObject<List<CountlyEvent>>(request.Params["events"]))
                .Select(sentEvent => sentEvent.Key)
                .ToList();
        }

        /// <summary>
        /// Returns how often the health check counted the log code since the last health check
        /// </summary>
        private static int LogCodeCount(string code)
        {
            Dictionary<string, int> codes = Countly.Instance.moduleHealthCheck.Snapshot().lc;
            return codes.ContainsKey(code) ? codes[code] : 0;
        }

        /// <summary>
        /// Returns the SHA-256 hash of the text as lowercase hex, as the SDK computes "checksum256"
        /// </summary>
        private static string Sha256Hex(string text)
        {
            using (SHA256 sha256 = SHA256.Create()) {
                return BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Content display that reports a fixed screen and shows nothing
        /// </summary>
        private class TestContentDisplay : IContentDisplay
        {
            public ContentScreen GetScreen()
            {
                return new ContentScreen { Width = 800, Height = 600 };
            }

            public void Present(ContentPlacement portrait, ContentPlacement landscape, string url, Action onClosed)
            {
            }
        }
    }
}
