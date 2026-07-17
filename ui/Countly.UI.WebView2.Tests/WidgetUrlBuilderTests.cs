using System.Collections.Generic;
using CountlySDK.CountlyCommon;
using CountlySDK.CountlyCommon.Helpers;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    public class WidgetUrlBuilderTests
    {
        private static Dictionary<string, object> Base() => new Dictionary<string, object> {
            { "app_key", "APPKEY" }, { "device_id", "DEV1" },
            { "sdk_name", "csharp" }, { "sdk_version", "9.9.9" }, { "av", "1.2.3" }
        };

        [Fact]
        public void BuildFeedbackWidgetUrl_ContainsRequiredParamsAndCustom()
        {
            var widget = new CountlyFeedbackWidget { widgetId = "w1", type = FeedbackWidgetType.survey };

            string url = WidgetUrlBuilder.BuildFeedbackWidgetUrl("https://s.count.ly", widget, Base());

            Assert.StartsWith("https://s.count.ly/feedback/survey?", url);
            Assert.Contains("widget_id=w1", url);
            Assert.Contains("device_id=DEV1", url);
            Assert.Contains("app_key=APPKEY", url);
            Assert.Contains("platform=windows", url);
            Assert.Contains("sdk_version=9.9.9", url);
            Assert.Contains("app_version=1.2.3", url);
            Assert.Contains("%22tc%22%3A1", url); // "tc":1
            Assert.Contains("%22xb%22%3A1", url); // "xb":1
        }

        [Fact]
        public void Custom_DropsRw()
        {
            var widget = new CountlyFeedbackWidget { widgetId = "w1", type = FeedbackWidgetType.survey };
            string url = WidgetUrlBuilder.BuildFeedbackWidgetUrl("https://s.count.ly/", widget, Base());
            Assert.Contains("custom=" + System.Uri.EscapeDataString("{\"tc\":1,\"xb\":1}"), url);
            Assert.DoesNotContain("%22rw%22", url); // no "rw" key
        }

        [Fact]
        public void AppendsUnencodedOrigin_SchemeAndAuthorityOnly()
        {
            var widget = new CountlyFeedbackWidget { widgetId = "w1", type = FeedbackWidgetType.nps };
            string url = WidgetUrlBuilder.BuildFeedbackWidgetUrl("https://s.count.ly/path/", widget, Base());
            Assert.EndsWith("&origin=https://s.count.ly", url);
        }

        [Fact]
        public void BuildFeedbackWidgetUrl_NullWidgetId_DoesNotThrow()
        {
            var widget = new CountlyFeedbackWidget { widgetId = null, type = FeedbackWidgetType.nps };

            // Must not throw ArgumentNullException from Uri.EscapeDataString(null).
            string url = WidgetUrlBuilder.BuildFeedbackWidgetUrl("https://s.count.ly", widget, Base());

            Assert.StartsWith("https://s.count.ly/feedback/nps?", url);
            Assert.Contains("widget_id=", url);
        }
    }
}
