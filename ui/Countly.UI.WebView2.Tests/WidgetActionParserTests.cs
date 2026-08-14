using System;
using CountlySDK.CountlyCommon;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    public class WidgetActionParserTests
    {
        [Fact]
        public void ParsesResizeAndClose()
        {
            string resizeUrl = "https://countly_action_event?cly_widget_command=1&action=resize_me&resize_me=" +
                Uri.EscapeDataString("{\"p\":{\"x\":1,\"y\":2,\"w\":300,\"h\":400},\"l\":{\"x\":5,\"y\":6,\"w\":700,\"h\":500}}");
            WidgetAction a = WidgetActionParser.Parse(resizeUrl);
            Assert.True(a.IsActionEvent);
            Assert.True(a.HasResize);
            Assert.Equal(300, a.Portrait.W);
            Assert.Equal(400, a.Portrait.H);
            Assert.Equal(700, a.Landscape.W);
            Assert.False(a.Close);

            WidgetAction c = WidgetActionParser.Parse("https://countly_action_event?cly_widget_command=1&close=1");
            Assert.True(c.IsActionEvent);
            Assert.True(c.Close);

            WidgetAction n = WidgetActionParser.Parse("https://example.com/feedback/nps?widget_id=w1");
            Assert.False(n.IsActionEvent);
        }
    }
}
