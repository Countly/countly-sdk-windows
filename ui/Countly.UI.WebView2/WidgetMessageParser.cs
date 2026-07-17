using CountlySDK.CountlyCommon;
using Newtonsoft.Json.Linq;

namespace CountlySDK.UI
{
    /// <summary>
    /// Parses the widget's postMessage payloads (bridged to the host) — the
    /// <c>{cly_widget_command, action:'resize_me', resize_me:{p,l}}</c> / <c>{close}</c> shape.
    /// Distinct from the URL-based <see cref="WidgetActionParser"/> (which handles
    /// countly_action_event navigations). A rect is only usable when it has positive w AND h.
    /// Uses Newtonsoft (the SDK's JSON library) so it builds on net462 as well as net8.0.
    /// </summary>
    public static class WidgetMessageParser
    {
        public static bool TryParse(string json, out WidgetAction action)
        {
            action = new WidgetAction();
            if (string.IsNullOrEmpty(json)) { return false; }

            JToken token;
            try { token = JToken.Parse(json); }
            catch { return false; }

            if (!(token is JObject root)) { return false; }
            if (root.Property("cly_widget_command") == null) { return false; }

            action.IsActionEvent = true;

            JToken close = root["close"];
            if (close != null && IsTruthy(close)) {
                action.Close = true;
            }

            if (root["resize_me"] is JObject rm) {
                WidgetRect p = ReadRect(rm, "p");
                WidgetRect l = ReadRect(rm, "l");
                if (p != null || l != null) {
                    action.HasResize = true;
                    action.Portrait = p;
                    action.Landscape = l;
                }
            }
            return true;
        }

        private static WidgetRect ReadRect(JObject resizeMe, string key)
        {
            if (!(resizeMe[key] is JObject e)) { return null; }
            int w = ReadInt(e, "w");
            int h = ReadInt(e, "h");
            if (w <= 0 || h <= 0) { return null; }
            return new WidgetRect { X = ReadInt(e, "x"), Y = ReadInt(e, "y"), W = w, H = h };
        }

        private static int ReadInt(JObject obj, string name)
        {
            JToken v = obj[name];
            if (v != null && (v.Type == JTokenType.Integer || v.Type == JTokenType.Float)) { return (int)v; }
            return 0;
        }

        private static bool IsTruthy(JToken e)
        {
            if (e.Type == JTokenType.Boolean) { return (bool)e; }
            if (e.Type == JTokenType.Integer || e.Type == JTokenType.Float) { return (double)e != 0; }
            return false;
        }
    }
}
