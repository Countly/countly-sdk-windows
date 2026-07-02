using System.Collections.Generic;
using CountlySDK.CountlyCommon;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    public class FeedbackWidgetParserTests
    {
        [Fact]
        public void ParseAvailableWidgets_ParsesAllTypes()
        {
            string json = "{\"result\":[" +
                "{\"_id\":\"id_s\",\"type\":\"survey\",\"name\":\"S\",\"tg\":[\"/a\"]}," +
                "{\"_id\":\"id_n\",\"type\":\"nps\",\"name\":\"N\",\"tg\":[]}," +
                "{\"_id\":\"id_r\",\"type\":\"rating\",\"name\":\"R\",\"tg\":[\"/b\",\"/c\"]}]}";

            CountlyFeedbackWidget[] widgets = FeedbackWidgetParser.ParseAvailableWidgets(json);

            Assert.Equal(3, widgets.Length);
            Assert.Equal("id_s", widgets[0].widgetId);
            Assert.Equal(FeedbackWidgetType.survey, widgets[0].type);
            Assert.Equal("N", widgets[1].name);
            Assert.Equal(FeedbackWidgetType.nps, widgets[1].type);
            Assert.Equal(new List<string> { "/b", "/c" }, widgets[2].tags);
        }

        [Fact]
        public void ParseAvailableWidgets_InvalidOrEmpty_ReturnsEmpty()
        {
            Assert.Empty(FeedbackWidgetParser.ParseAvailableWidgets(""));
            Assert.Empty(FeedbackWidgetParser.ParseAvailableWidgets("{}"));
            Assert.Empty(FeedbackWidgetParser.ParseAvailableWidgets("{\"result\":\"nope\"}"));
            Assert.Empty(FeedbackWidgetParser.ParseAvailableWidgets("not json"));
        }
    }
}
