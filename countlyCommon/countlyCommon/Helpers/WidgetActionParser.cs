using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace CountlySDK.CountlyCommon
{
    public class WidgetRect { public int X, Y, W, H; }

    public class WidgetAction
    {
        public bool IsActionEvent;
        public bool Close;
        public bool HasResize;
        public WidgetRect Portrait;
        public WidgetRect Landscape;
    }

    public static class WidgetActionParser
    {
        private const string ActionHost = "countly_action_event";

        public static WidgetAction Parse(string url)
        {
            WidgetAction action = new WidgetAction();
            if (string.IsNullOrEmpty(url) || url.IndexOf(ActionHost, StringComparison.Ordinal) < 0) {
                return action;
            }
            action.IsActionEvent = true;

            Dictionary<string, string> q = ParseQuery(url);
            if (q.TryGetValue("close", out string close) && close == "1") {
                action.Close = true;
            }
            if (q.TryGetValue("resize_me", out string resize) && !string.IsNullOrEmpty(resize)) {
                try {
                    JObject obj = JObject.Parse(resize);
                    action.Portrait = ToRect(obj["p"]);
                    action.Landscape = ToRect(obj["l"]);
                    action.HasResize = action.Portrait != null || action.Landscape != null;
                } catch { /* ignore malformed resize payload */ }
            }
            return action;
        }

        private static WidgetRect ToRect(JToken t)
        {
            if (t == null) { return null; }
            return new WidgetRect {
                X = (int?)t["x"] ?? 0, Y = (int?)t["y"] ?? 0,
                W = (int?)t["w"] ?? 0, H = (int?)t["h"] ?? 0
            };
        }

        private static Dictionary<string, string> ParseQuery(string url)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            int qi = url.IndexOf('?');
            if (qi < 0 || qi == url.Length - 1) { return result; }
            foreach (string pair in url.Substring(qi + 1).Split('&')) {
                int eq = pair.IndexOf('=');
                if (eq <= 0) { continue; }
                string key = pair.Substring(0, eq);
                string val = Uri.UnescapeDataString(pair.Substring(eq + 1));
                result[key] = val;
            }
            return result;
        }
    }
}
