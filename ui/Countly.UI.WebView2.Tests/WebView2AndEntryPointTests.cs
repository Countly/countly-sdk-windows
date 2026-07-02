using System;
using CountlySDK.CountlyCommon;
using CountlySDK.UI;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    // T10-T12 verification that does NOT require a UI thread or the WebView2 runtime:
    // the runtime probe is tolerant, and the entry points are asserted to exist by reflection
    // (the real WebView2 display path is exercised via the sample apps).
    public class WebView2AndEntryPointTests
    {
        [Fact]
        public void WebView2Runtime_IsAvailable_DoesNotThrow()
        {
            bool available = WebView2Runtime.IsAvailable(out string version);
            // On CI without the runtime this is false + null; on a dev machine it may be true.
            Assert.True(available || version == null);
        }

        [Fact]
        public void PresentFeedbackWidget_Wpf_SignatureExists()
        {
            System.Reflection.MethodInfo m = typeof(CountlyWebView).GetMethod(
                "PresentFeedbackWidget",
                new[] { typeof(System.Windows.Window), typeof(CountlyFeedbackWidget), typeof(Action) });
            Assert.NotNull(m);
        }

        [Fact]
        public void PresentFeedbackWidget_WinForms_SignatureExists()
        {
            System.Reflection.MethodInfo m = typeof(CountlyWebView).GetMethod(
                "PresentFeedbackWidget",
                new[] { typeof(System.Windows.Forms.IWin32Window), typeof(CountlyFeedbackWidget), typeof(Action) });
            Assert.NotNull(m);
        }
    }
}
