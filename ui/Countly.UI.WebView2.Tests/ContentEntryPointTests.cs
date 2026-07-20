using System;
using CountlySDK.CountlyCommon;
using CountlySDK.UI;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    // The live overlay needs a UI thread + WebView2 runtime, so it's exercised via the demo.
    // Here we assert the entry points exist and the bridge type implements IContentDisplay.
    public class ContentEntryPointTests
    {
        [Fact]
        public void EnableDisableContentZone_SignaturesExist()
        {
            Assert.NotNull(typeof(CountlyWebView).GetMethod("EnableContentZone", new[] { typeof(string[]) }));
            Assert.NotNull(typeof(CountlyWebView).GetMethod("DisableContentZone", Type.EmptyTypes));
        }

        [Fact]
        public void WebView2ContentDisplay_ImplementsBridge()
        {
            Type t = typeof(CountlyWebView).Assembly.GetType("CountlySDK.UI.WebView2ContentDisplay");
            Assert.NotNull(t);
            Assert.True(typeof(IContentDisplay).IsAssignableFrom(t));
        }
    }
}
