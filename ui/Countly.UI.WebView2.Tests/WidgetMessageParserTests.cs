using CountlySDK.CountlyCommon;
using CountlySDK.UI;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    public class WidgetMessageParserTests
    {
        [Fact]
        public void ParsesResizeMe_BothOrientations()
        {
            string json = "{\"cly_widget_command\":1,\"action\":\"resize_me\",\"resize_me\":{\"p\":{\"x\":10,\"y\":20,\"w\":300,\"h\":400},\"l\":{\"x\":5,\"y\":6,\"w\":700,\"h\":350}}}";
            Assert.True(WidgetMessageParser.TryParse(json, out WidgetAction a));
            Assert.True(a.HasResize);
            Assert.Equal(300, a.Portrait.W);
            Assert.Equal(20, a.Portrait.Y);
            Assert.Equal(700, a.Landscape.W);
        }

        [Fact]
        public void ResizeMe_WithoutWidthHeight_IsNotAResize()
        {
            string json = "{\"cly_widget_command\":1,\"action\":\"resize_me\",\"resize_me\":{\"p\":{\"x\":0,\"y\":0},\"l\":{\"x\":0,\"y\":0}}}";
            Assert.True(WidgetMessageParser.TryParse(json, out WidgetAction a));
            Assert.False(a.HasResize);
        }

        [Fact]
        public void ParsesClose()
        {
            string json = "{\"cly_widget_command\":1,\"close\":1}";
            Assert.True(WidgetMessageParser.TryParse(json, out WidgetAction a));
            Assert.True(a.Close);
        }

        [Fact]
        public void NonWidgetJson_ReturnsFalse()
        {
            Assert.False(WidgetMessageParser.TryParse("{\"type\":\"resize\",\"width\":400}", out _));
            Assert.False(WidgetMessageParser.TryParse("not json", out _));
        }
    }
}
