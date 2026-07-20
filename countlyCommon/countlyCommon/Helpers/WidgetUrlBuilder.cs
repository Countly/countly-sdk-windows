using System;
using System.Collections.Generic;
using System.Text;

namespace CountlySDK.CountlyCommon.Helpers
{
    /// <summary>
    /// Builds the display URL for a feedback widget. Pure/UI-free so it can be unit-tested and
    /// reused by the WebView2 UI package. Desktop follows the web-SDK model: <c>custom={"tc":1,"xb":1}</c>
    /// (no <c>rw</c> fullscreen flag) plus <c>origin</c>, so the widget renders a positioned card and
    /// reports its rect via resize_me; the SDK reports the surface size and places the window there.
    /// </summary>
    public static class WidgetUrlBuilder
    {
        public static string BuildFeedbackWidgetUrl(string serverUrl, CountlyFeedbackWidget widget, IDictionary<string, object> baseParams)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(serverUrl.TrimEnd('/'));
            sb.Append("/feedback/").Append(widget.type).Append("?");
            // Uri.EscapeDataString throws on null; a null widgetId is a caller error, not a crash.
            sb.Append("widget_id=").Append(Uri.EscapeDataString(widget.widgetId ?? ""));
            Append(sb, "device_id", baseParams, "device_id");
            Append(sb, "app_key", baseParams, "app_key");
            Append(sb, "sdk_name", baseParams, "sdk_name");
            Append(sb, "sdk_version", baseParams, "sdk_version");
            Append(sb, "app_version", baseParams, "av");
            sb.Append("&platform=windows");
            // Desktop = web-SDK model: no rw (fullscreen); xb = widget draws its own close button.
            sb.Append("&custom=").Append(Uri.EscapeDataString("{\"tc\":1,\"xb\":1}"));
            // The widget only accepts our post-load {type:'resize'} message if its `origin` matches
            // the page's own origin (the widget page validates event.origin === ?origin= param).
            // Without this the resize message is dropped and a fullscreen widget has no dimensions
            // to fill to (renders as a fixed content-sized card). Sent unencoded, like the web SDK.
            string origin = OriginOf(serverUrl);
            if (origin != null) { sb.Append("&origin=").Append(origin); }
            return sb.ToString();
        }

        /// <summary>Scheme+authority of the server URL (e.g. "https://host:port"), or null if unparseable.</summary>
        private static string OriginOf(string serverUrl)
        {
            try {
                return new Uri(serverUrl, UriKind.Absolute).GetLeftPart(UriPartial.Authority);
            } catch {
                return null;
            }
        }

        private static void Append(StringBuilder sb, string outKey, IDictionary<string, object> src, string srcKey)
        {
            if (src != null && src.TryGetValue(srcKey, out object v) && v != null) {
                sb.Append("&").Append(outKey).Append("=").Append(Uri.EscapeDataString(Convert.ToString(v)));
            }
        }
    }
}
