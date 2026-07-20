using System.Collections.Generic;
using System.Linq;
using CountlySDK.CountlyCommon;
using CountlySDK.Entities;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    // Local integration tests for the feedback network layer (T3/T4/T5). Uses only PUBLIC SDK
    // API + a local mock server, so they run from this external harness. (The event-queue
    // assertion for T5 that inspects Countly.Instance.Events directly lives in the source-linked
    // TestProject_common test for the VS build; here we verify T5 via the observable /i upload.)
    public class ModuleFeedbackNetworkTests
    {
        [Fact]
        public void GetAvailableFeedbackWidgets_FetchesAndParses()
        {
            using LocalMockServer server = new LocalMockServer((rawUrl, body) =>
                body != null && body.Contains("method=feedback")
                    ? "{\"result\":[{\"_id\":\"w1\",\"type\":\"nps\",\"name\":\"N\",\"tg\":[]}]}"
                    : null);

            CountlySDK.Countly.Halt();
            CountlySDK.Countly.Instance.Init(new CountlyConfig { serverUrl = server.Url, appKey = "APP_KEY", appVersion = "1.0" }).Wait();

            CountlyFeedbackWidget[] widgets = CountlySDK.Countly.Instance.Feedback().GetAvailableFeedbackWidgets().Result;

            Assert.Single(widgets);
            Assert.Equal("w1", widgets[0].widgetId);
            Assert.Equal(FeedbackWidgetType.nps, widgets[0].type);
            CountlySDK.Countly.Halt();
        }

        [Fact]
        public void GetFeedbackWidgetData_HitsSurveyEndpointAndParses()
        {
            using LocalMockServer server = new LocalMockServer((rawUrl, body) =>
                rawUrl.Contains("/o/surveys/") ? "{\"name\":\"My NPS\"}" : null);

            CountlySDK.Countly.Halt();
            CountlySDK.Countly.Instance.Init(new CountlyConfig { serverUrl = server.Url, appKey = "APP_KEY", appVersion = "1.0" }).Wait();

            CountlyFeedbackWidget widget = new CountlyFeedbackWidget { widgetId = "w1", type = FeedbackWidgetType.nps };
            JObject data = CountlySDK.Countly.Instance.Feedback().GetFeedbackWidgetData(widget).Result;

            Assert.NotNull(data);
            Assert.Equal("My NPS", (string)data["name"]);

            LocalMockServer.Captured req = server.Requests.Last(r => r.RawUrl.Contains("/o/surveys/"));
            Assert.Contains("/o/surveys/nps/widget", req.RawUrl);
            Assert.Contains("widget_id=w1", req.RawUrl + req.Body);
            Assert.Contains("shown=1", req.RawUrl + req.Body);
            CountlySDK.Countly.Halt();
        }

        [Fact]
        public void ReportFeedbackWidgetManually_UploadsNpsEventWithWidgetSegmentation()
        {
            using LocalMockServer server = new LocalMockServer((rawUrl, body) => "{\"result\":\"success\"}");

            CountlySDK.Countly.Halt();
            CountlySDK.Countly.Instance.Init(new CountlyConfig { serverUrl = server.Url, appKey = "APP_KEY", appVersion = "1.0" }).Wait();

            CountlyFeedbackWidget widget = new CountlyFeedbackWidget { widgetId = "w1", type = FeedbackWidgetType.nps };
            Dictionary<string, object> result = new Dictionary<string, object> { { "rating", 9 } };
            CountlySDK.Countly.Instance.Feedback().ReportFeedbackWidgetManually(widget, null, result).Wait();

            // The [CLY]_nps event is uploaded to /i; its (url-encoded) body must carry the widget segmentation.
            bool found = server.Requests.Any(r => {
                string decoded = System.Uri.UnescapeDataString(r.Body ?? "");
                return decoded.Contains("[CLY]_nps") && decoded.Contains("widget_id") && decoded.Contains("w1");
            });
            Assert.True(found, "No uploaded request carried the NPS feedback event. Captured bodies: " +
                string.Join(" || ", server.Requests.Select(r => r.Body)));
            CountlySDK.Countly.Halt();
        }
    }
}
