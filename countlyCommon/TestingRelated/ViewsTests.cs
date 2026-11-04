using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CountlySDK;
using CountlySDK.Entities;
using Newtonsoft.Json;
using Xunit;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace TestProject_common
{
    public class ViewsTests : IDisposable
    {
        private const string VIEW_EVENT_KEY = "[CLY]_view";

        /// <summary>
        /// Test setup
        /// </summary>
        public ViewsTests()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Instance.HaltInternal().Wait(); // synchronous teardown: avoid async-void Halt racing with the next Init
            TestHelper.CleanDataFiles();
            Countly.Instance.deferUpload = false;
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {

        }

        [Fact]
        /// <summary>
        /// It validates the limit of the view's name size.
        ///
        /// todo potentially flakey test
        /// </summary>
        public async void TestEventLimits()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            cc.MaxKeyLength = 5;


            Countly.Instance.Init(cc).Wait();
            Countly.Instance.SessionBegin().Wait();

            Countly.Instance.deferUpload = true;

            bool res = await Countly.Instance.RecordView("open_view");
            Assert.True(res);

            CountlyEvent model = Countly.Instance.Events[0];

            SegmentationItem item = model.Segmentation.segmentation[0];
            Assert.Equal("name", item.Key);
            Assert.Equal("open_", item.Value);

            item = model.Segmentation.segmentation[1];
            Assert.Equal("visit", item.Key);
            Assert.Equal("1", item.Value);

            item = model.Segmentation.segmentation[2];
            Assert.Equal("segment", item.Key);
            Assert.Equal("Windows", item.Value);

            item = model.Segmentation.segmentation[3];
            Assert.Equal("start", item.Key);
            Assert.Equal("1", item.Value);

            Countly.Instance.SessionEnd().Wait();

        }

        [Fact]
        /// <summary>The given segmentation is sent with the view's start event, next to the SDK's own view keys.</summary>
        public async Task RecordViewWithSegmentation_SendsSegmentationWithViewStart()
        {
            using (MockHttpServer server = new MockHttpServer()) {
                await Countly.Instance.Init(ConfigWithServer(server));

                bool result = await Countly.Instance.RecordView("Home", TestHelper.Segm("color", "red", "level", "3"));

                Assert.True(result);
                List<CountlyEvent> views = await WaitForSentViews(server, 1);
                AssertViewStart(views[0], "Home", "start", "1", "color", "red", "level", "3");
            }
        }

        [Fact]
        /// <summary>The segmentation is only sent with the view's start event, the event that ends the view does not carry it.</summary>
        public async Task RecordViewWithSegmentation_IsNotAddedToViewEnd()
        {
            using (MockHttpServer server = new MockHttpServer()) {
                await Countly.Instance.Init(ConfigWithServer(server));

                await Countly.Instance.RecordView("Home", TestHelper.Segm("color", "red"));
                await Countly.Instance.RecordView("Cart");

                List<CountlyEvent> views = await WaitForSentViews(server, 3);
                AssertViewStart(views[0], "Home", "start", "1", "color", "red");
                AssertViewEnd(views[1], "Home");
                AssertViewStart(views[2], "Cart");
            }
        }

        [Fact]
        /// <summary>The reserved keys "name", "visit", "start" and "segment" in the given segmentation are ignored.</summary>
        public async Task RecordViewWithSegmentation_ReservedKeys_AreIgnored()
        {
            using (MockHttpServer server = new MockHttpServer()) {
                await Countly.Instance.Init(ConfigWithServer(server));
                Segmentation segmentation = TestHelper.Segm("name", "x", "visit", "x", "start", "x", "segment", "x", "color", "red");

                await Countly.Instance.RecordView("Home", segmentation);
                await Countly.Instance.RecordView("Cart", segmentation);

                List<CountlyEvent> views = await WaitForSentViews(server, 3);
                AssertViewStart(views[0], "Home", "start", "1", "color", "red");
                AssertViewStart(views[2], "Cart", "color", "red");
            }
        }

        [Fact]
        /// <summary>Segmentation keys and values are truncated and extra entries dropped, reserved keys do not count toward the limit.</summary>
        public async Task RecordViewWithSegmentation_AppliesLimits()
        {
            using (MockHttpServer server = new MockHttpServer()) {
                CountlyConfig cc = ConfigWithServer(server);
                cc.MaxKeyLength = 5;
                cc.MaxValueSize = 3;
                cc.MaxSegmentationValues = 2;
                await Countly.Instance.Init(cc);

                await Countly.Instance.RecordView("Home", TestHelper.Segm("name", "x", "colour", "purple", "level", "high", "extra", "y"));

                List<CountlyEvent> views = await WaitForSentViews(server, 1);
                AssertViewStart(views[0], "Home", "start", "1", "colou", "pur", "level", "hig");
            }
        }

        [Fact]
        /// <summary>A key that is truncated into a reserved key is ignored, also on a later view that has no "start" key.</summary>
        public async Task RecordViewWithSegmentation_KeyTruncatedToReservedKey_IsIgnored()
        {
            using (MockHttpServer server = new MockHttpServer()) {
                CountlyConfig cc = ConfigWithServer(server);
                cc.MaxKeyLength = 5;
                await Countly.Instance.Init(cc);
                Segmentation segmentation = TestHelper.Segm("visits", "x", "startpage", "x", "colour", "red");

                await Countly.Instance.RecordView("Home", segmentation);
                await Countly.Instance.RecordView("Cart", segmentation);

                List<CountlyEvent> views = await WaitForSentViews(server, 3);
                AssertViewStart(views[0], "Home", "start", "1", "colou", "red");
                AssertViewStart(views[2], "Cart", "colou", "red");
            }
        }

        [Fact]
        /// <summary>A null or empty segmentation records the same view as the overload without segmentation.</summary>
        public async Task RecordViewWithSegmentation_NullOrEmpty_RecordsPlainView()
        {
            using (MockHttpServer server = new MockHttpServer()) {
                await Countly.Instance.Init(ConfigWithServer(server));

                Assert.True(await Countly.Instance.RecordView("Home", null));
                Assert.True(await Countly.Instance.RecordView("Cart", new Segmentation()));

                List<CountlyEvent> views = await WaitForSentViews(server, 3);
                AssertViewStart(views[0], "Home", "start", "1");
                AssertViewStart(views[2], "Cart");
            }
        }

        [Fact]
        /// <summary>Segmentation entries with a null value are skipped and do not count toward the segmentation limit.</summary>
        public async Task RecordViewWithSegmentation_NullValue_IsSkippedAndNotCounted()
        {
            using (MockHttpServer server = new MockHttpServer()) {
                CountlyConfig cc = ConfigWithServer(server);
                cc.MaxSegmentationValues = 1;
                await Countly.Instance.Init(cc);

                await Countly.Instance.RecordView("Home", TestHelper.Segm("color", null, "size", "big"));

                List<CountlyEvent> views = await WaitForSentViews(server, 1);
                AssertViewStart(views[0], "Home", "start", "1", "size", "big");
            }
        }

        [Fact]
        /// <summary>The caller's segmentation object is left as it was, so it can be used again for the next view.</summary>
        public async Task RecordViewWithSegmentation_CallersSegmentation_IsNotChanged()
        {
            using (MockHttpServer server = new MockHttpServer()) {
                CountlyConfig cc = ConfigWithServer(server);
                cc.MaxSegmentationValues = 1;
                await Countly.Instance.Init(cc);
                Segmentation segmentation = TestHelper.Segm("name", "x", "color", "red", "size", "big");

                await Countly.Instance.RecordView("Home", segmentation);
                await Countly.Instance.RecordView("Cart", segmentation);

                Assert.Equal(new[] { "name", "color", "size" }, segmentation.segmentation.Select(item => item.Key));
                List<CountlyEvent> views = await WaitForSentViews(server, 3);
                AssertViewStart(views[0], "Home", "start", "1", "color", "red");
                AssertViewStart(views[2], "Cart", "color", "red");
            }
        }

        [Fact]
        /// <summary>A null or empty view name is rejected even when segmentation is given.</summary>
        public async Task RecordViewWithSegmentation_NullOrEmptyName_RecordsNothing()
        {
            using (MockHttpServer server = new MockHttpServer()) {
                await Countly.Instance.Init(ConfigWithServer(server));

                Assert.False(await Countly.Instance.RecordView(null, TestHelper.Segm("color", "red")));
                Assert.False(await Countly.Instance.RecordView("", TestHelper.Segm("color", "red")));

                Assert.Empty(Countly.Instance.Events);
                Assert.Empty(SentViews(server));
            }
        }

        [Fact]
        /// <summary>Without views consent, a view with segmentation is neither queued nor sent.</summary>
        public async Task RecordViewWithSegmentation_NoViewsConsent_RecordsNothing()
        {
            using (MockHttpServer server = new MockHttpServer()) {
                CountlyConfig cc = ConfigWithServer(server);
                Dictionary<ConsentFeatures, bool> consent = TestHelper.AllConsentValues(true);
                consent[ConsentFeatures.Views] = false;
                cc.givenConsent = consent;
                cc.consentRequired = true;
                await Countly.Instance.Init(cc);

                bool result = await Countly.Instance.RecordView("Home", TestHelper.Segm("color", "red"));

                Assert.False(result);
                Assert.Empty(Countly.Instance.Events);
                Assert.Empty(SentViews(server));
            }
        }

        [Fact]
        /// <summary>When the server answers with an error, the view stays queued with its segmentation and is sent again with it.</summary>
        public async Task RecordViewWithSegmentation_ServerError_KeepsSegmentationForRetry()
        {
            bool serverFails = true;
            using (MockHttpServer server = new MockHttpServer(body => Volatile.Read(ref serverFails) && body.Contains("events=") ? "{\"error\":\"unavailable\"}" : null)) {
                await Countly.Instance.Init(ConfigWithServer(server));

                bool result = await Countly.Instance.RecordView("Home", TestHelper.Segm("color", "red"));

                Assert.False(result);
                Assert.Single(Countly.Instance.Events);
                AssertViewStart(Countly.Instance.Events[0], "Home", "start", "1", "color", "red");

                Volatile.Write(ref serverFails, false);
                Assert.True(await Countly.Instance.Upload());

                List<CountlyEvent> views = await WaitForSentViews(server, 2);
                AssertViewStart(views[0], "Home", "start", "1", "color", "red");
                AssertViewStart(views[1], "Home", "start", "1", "color", "red");
            }
        }

        [Fact]
        /// <summary>A view recorded while the server cannot be reached is stored with its segmentation and sent after a restart.</summary>
        public async Task RecordViewWithSegmentation_NoNetwork_SendsSegmentationAfterRestart()
        {
            CountlyConfig offline = TestHelper.GetConfig();
            offline.serverUrl = "http://127.0.0.1:1/";
            await Countly.Instance.Init(offline);

            Assert.False(await Countly.Instance.RecordView("Home", TestHelper.Segm("color", "red")));
            await Countly.Instance.HaltInternal(false);

            using (MockHttpServer server = new MockHttpServer()) {
                await Countly.Instance.Init(ConfigWithServer(server));
                await Countly.Instance.Upload();

                List<CountlyEvent> views = await WaitForSentViews(server, 1);
                AssertViewStart(views[0], "Home", "start", "1", "color", "red");
            }
        }

        /// <summary>
        /// Returns the default test configuration pointed at the given mock server.
        /// </summary>
        private CountlyConfig ConfigWithServer(MockHttpServer server)
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            return cc;
        }

        /// <summary>
        /// Returns the view events the mock server has received so far, in the order they were sent.
        /// </summary>
        private List<CountlyEvent> SentViews(MockHttpServer server)
        {
            return server.Requests
                .Where(request => request.Params.ContainsKey("events"))
                .SelectMany(request => JsonConvert.DeserializeObject<List<CountlyEvent>>(request.Params["events"]))
                .Where(sentEvent => sentEvent.Key == VIEW_EVENT_KEY)
                .ToList();
        }

        /// <summary>
        /// Polls, up to a deadline, until the mock server has received the expected number of view events and the
        /// event queue is empty, then returns the received view events in the order they were sent.
        /// </summary>
        private async Task<List<CountlyEvent>> WaitForSentViews(MockHttpServer server, int expectedCount)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(10);
            List<CountlyEvent> views = SentViews(server);
            while ((views.Count < expectedCount || Countly.Instance.Events.Count > 0) && DateTime.UtcNow < deadline) {
                await Task.Delay(20);
                views = SentViews(server);
            }

            Assert.Equal(expectedCount, views.Count);
            return views;
        }

        /// <summary>
        /// Asserts that the event starts the named view: its segmentation is exactly the SDK's "name", "visit" and
        /// "segment" keys plus the given other key and value pairs.
        /// </summary>
        private void AssertViewStart(CountlyEvent viewEvent, string viewName, params string[] otherSegmentation)
        {
            Dictionary<string, string> expected = new Dictionary<string, string>(TestHelper.DictS(otherSegmentation));
            expected["name"] = viewName;
            expected["visit"] = "1";
            expected["segment"] = "Windows";

            Assert.Equal(VIEW_EVENT_KEY, viewEvent.Key);
            Assert.Equal(1, viewEvent.Count);
            Assert.Equal(expected, viewEvent.segmentation);
        }

        /// <summary>
        /// Asserts that the event ends the named view: it carries only the view name, the platform and the duration in seconds.
        /// </summary>
        private void AssertViewEnd(CountlyEvent viewEvent, string viewName)
        {
            Assert.Equal(VIEW_EVENT_KEY, viewEvent.Key);
            Assert.Equal(new[] { "dur", "name", "segment" }, viewEvent.segmentation.Keys.OrderBy(key => key, StringComparer.Ordinal));
            Assert.Equal(viewName, viewEvent.segmentation["name"]);
            Assert.Equal("Windows", viewEvent.segmentation["segment"]);
            Assert.True(long.Parse(viewEvent.segmentation["dur"]) >= 0);
        }
    }
}
