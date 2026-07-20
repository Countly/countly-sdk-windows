namespace CountlySDK.UI
{
    public static partial class CountlyWebView
    {
        private static WebView2ContentDisplay _contentDisplay;

        /// <summary>
        /// Enables the Countly content zone with a WebView2 overlay on the primary screen.
        /// Call on the UI thread. No-op if the WebView2 runtime is unavailable. (Experimental.)
        /// </summary>
        public static void EnableContentZone(string[] categories = null)
        {
            if (!WebView2Runtime.IsAvailable(out _)) {
                System.Diagnostics.Debug.WriteLine("[CountlyWebView] WebView2 runtime not available; content zone disabled");
                return;
            }
            _contentDisplay = new WebView2ContentDisplay();
            CountlySDK.Countly.Instance.SetContentDisplay(_contentDisplay);
            CountlySDK.Countly.Instance.Content().EnterContentZone(categories);
        }

        /// <summary>Disables the content zone (stops polling and prevents further overlays).</summary>
        public static void DisableContentZone()
        {
            CountlySDK.Countly.Instance.Content().ExitContentZone();
        }
    }
}
