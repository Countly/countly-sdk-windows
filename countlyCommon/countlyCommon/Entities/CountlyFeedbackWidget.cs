using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace CountlySDK.CountlyCommon
{
    public enum FeedbackWidgetType { survey, nps, rating }

    public class CountlyFeedbackWidget
    {
        public string widgetId;
        public FeedbackWidgetType type;
        public string name;
        public List<string> tags = new List<string>();
        // Server "wv" (widget version). Null/empty => legacy widget (no resize_me protocol).
        public string widgetVersion;
    }

    public static class FeedbackWidgetParser
    {
        public static CountlyFeedbackWidget[] ParseAvailableWidgets(string responseText)
        {
            List<CountlyFeedbackWidget> result = new List<CountlyFeedbackWidget>();
            if (string.IsNullOrEmpty(responseText)) { return result.ToArray(); }

            try {
                JObject root = JObject.Parse(responseText);
                if (!(root["result"] is JArray arr)) { return result.ToArray(); }

                foreach (JToken item in arr) {
                    string typeStr = (string)item["type"];
                    if (typeStr == null) { continue; }
                    if (!TryParseWidgetType(typeStr, out FeedbackWidgetType type)) { continue; }

                    CountlyFeedbackWidget w = new CountlyFeedbackWidget {
                        widgetId = (string)item["_id"],
                        type = type,
                        name = (string)item["name"],
                        widgetVersion = (string)item["wv"]
                    };
                    if (item["tg"] is JArray tags) {
                        foreach (JToken t in tags) { w.tags.Add((string)t); }
                    }
                    if (!string.IsNullOrEmpty(w.widgetId)) { result.Add(w); }
                }
            } catch { /* malformed response -> empty list */ }

            return result.ToArray();
        }

        // net35-safe replacement for Enum.TryParse<T> (which is .NET 4.0+). The enum member names
        // are the exact server type strings.
        private static bool TryParseWidgetType(string value, out FeedbackWidgetType type)
        {
            switch (value) {
                case "survey": type = FeedbackWidgetType.survey; return true;
                case "nps": type = FeedbackWidgetType.nps; return true;
                case "rating": type = FeedbackWidgetType.rating; return true;
                default: type = default(FeedbackWidgetType); return false;
            }
        }
    }
}
