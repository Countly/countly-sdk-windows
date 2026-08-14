using System.Collections.Generic;
using CountlySDK.CountlyCommon;
using CountlySDK.CountlyCommon.Helpers;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    public class ContentRequestBuilderTests
    {
        [Fact]
        public void BuildParams_IncludesResolutionMethodAndLocale()
        {
            var screen = new ContentScreen { Width = 1920, Height = 1080 };
            IDictionary<string, object> p = ContentRequestBuilder.BuildParams(screen, new[] { "cat1" }, "en", "desktop", null);
            Assert.Equal("queue", p["method"]);
            Assert.Equal("en", p["la"]);
            Assert.Equal("desktop", p["dt"]);
            Assert.Contains("1920", (string)p["resolution"]);
            Assert.Contains("\"w\"", (string)p["resolution"]);
            Assert.False(p.ContainsKey("content_id"));
        }

        [Fact]
        public void BuildParams_Preview_AddsContentIdAndPreview()
        {
            var screen = new ContentScreen { Width = 800, Height = 600 };
            IDictionary<string, object> p = ContentRequestBuilder.BuildParams(screen, null, "en", "desktop", "cid1");
            Assert.Equal("cid1", p["content_id"]);
            Assert.Equal("true", p["preview"]);
        }
    }
}
