using System;
using System.Collections.Generic;
using System.Text;

namespace CountlySDK.CountlyCommon.Helpers
{
    /// <summary>
    /// Builds the display URL for a feedback widget. Pure/UI-free so it can be unit-tested and
    /// reused by the WebView2 UI package. The <c>custom={"tc":1,"rw":1,"xb":1}</c> object opts
    /// into versioned/resizable widgets (so the widget emits resize/close action events).
    /// </summary>
    public static class WidgetUrlBuilder
    {
        public static string BuildFeedbackWidgetUrl(string serverUrl, CountlyFeedbackWidget widget, IDictionary<string, object> baseParams)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(serverUrl.TrimEnd('/'));
            sb.Append("/feedback/").Append(widget.type).Append("?");
            sb.Append("widget_id=").Append(Uri.EscapeDataString(widget.widgetId));
            Append(sb, "device_id", baseParams, "device_id");
            Append(sb, "app_key", baseParams, "app_key");
            Append(sb, "sdk_name", baseParams, "sdk_name");
            Append(sb, "sdk_version", baseParams, "sdk_version");
            Append(sb, "app_version", baseParams, "av");
            sb.Append("&platform=windows");
            sb.Append("&custom=").Append(Uri.EscapeDataString("{\"tc\":1,\"rw\":1,\"xb\":1}"));
            return sb.ToString();
        }

        private static void Append(StringBuilder sb, string outKey, IDictionary<string, object> src, string srcKey)
        {
            if (src != null && src.TryGetValue(srcKey, out object v) && v != null) {
                sb.Append("&").Append(outKey).Append("=").Append(Uri.EscapeDataString(Convert.ToString(v)));
            }
        }
    }
}
