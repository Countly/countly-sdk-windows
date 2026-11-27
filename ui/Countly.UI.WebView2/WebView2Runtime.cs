using Microsoft.Web.WebView2.Core;

namespace CountlySDK.UI
{
    /// <summary>Detects whether the WebView2 Evergreen Runtime is available on the machine.</summary>
    public static class WebView2Runtime
    {
        /// <summary>
        /// Returns true if the WebView2 runtime is installed. <paramref name="version"/> receives
        /// the installed version string, or null if unavailable. Never throws.
        /// </summary>
        public static bool IsAvailable(out string version)
        {
            version = null;
            try {
                version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                return !string.IsNullOrEmpty(version);
            } catch {
                return false;
            }
        }
    }
}
