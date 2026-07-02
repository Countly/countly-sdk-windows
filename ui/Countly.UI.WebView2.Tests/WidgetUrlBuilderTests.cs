using System.Collections.Generic;
using CountlySDK.CountlyCommon;
using CountlySDK.CountlyCommon.Helpers;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    public class WidgetUrlBuilderTests
    {
        [Fact]
        public void BuildFeedbackWidgetUrl_ContainsRequiredParamsAndCustom()
        {
            var baseParams = new Dictionary<string, object> {
                { "app_key", "APPKEY" }, { "device_id", "DEV1" },
                { "sdk_name", "csharp" }, { "sdk_version", "9.9.9" }, { "av", "1.2.3" }
            };
            var widget = new CountlyFeedbackWidget { widgetId = "w1", type = FeedbackWidgetType.survey };

            string url = WidgetUrlBuilder.BuildFeedbackWidgetUrl("https://s.count.ly", widget, baseParams);

            Assert.StartsWith("https://s.count.ly/feedback/survey?", url);
            Assert.Contains("widget_id=w1", url);
            Assert.Contains("device_id=DEV1", url);
            Assert.Contains("app_key=APPKEY", url);
            Assert.Contains("platform=windows", url);
            Assert.Contains("sdk_version=9.9.9", url);
            Assert.Contains("app_version=1.2.3", url);
            Assert.Contains("%22rw%22%3A1", url); // "rw":1 (resizable opt-in), url-encoded
            Assert.Contains("%22tc%22%3A1", url);
        }
    }
}
