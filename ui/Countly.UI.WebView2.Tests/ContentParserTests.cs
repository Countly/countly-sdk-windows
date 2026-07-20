using CountlySDK.CountlyCommon;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    public class ContentParserTests
    {
        [Fact]
        public void ParsesGeoAndHtmlUrl()
        {
            string json = "{\"geo\":{\"p\":{\"x\":1,\"y\":2,\"w\":300,\"h\":400},\"l\":{\"x\":5,\"y\":6,\"w\":700,\"h\":500}},\"html\":\"https://s/content/abc\"}";
            ContentData c = ContentParser.ParseContentResponse(json);
            Assert.NotNull(c);
            Assert.Equal("https://s/content/abc", c.Url);
            Assert.Equal(300, c.Portrait.W);
            Assert.Equal(400, c.Portrait.H);
            Assert.Equal(700, c.Landscape.W);
            Assert.Equal(6, c.Landscape.Y);
        }

        [Fact]
        public void InvalidOrEmpty_ReturnsNull()
        {
            Assert.Null(ContentParser.ParseContentResponse(""));
            Assert.Null(ContentParser.ParseContentResponse("{}"));
            Assert.Null(ContentParser.ParseContentResponse("{\"geo\":{},\"html\":\"u\"}"));
            Assert.Null(ContentParser.ParseContentResponse("{\"jsonArray\":[]}"));
            Assert.Null(ContentParser.ParseContentResponse("not json"));
        }
    }
}
