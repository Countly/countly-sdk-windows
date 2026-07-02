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
                    if (!System.Enum.TryParse(typeStr, out FeedbackWidgetType type)) { continue; }

                    CountlyFeedbackWidget w = new CountlyFeedbackWidget {
                        widgetId = (string)item["_id"],
                        type = type,
                        name = (string)item["name"]
                    };
                    if (item["tg"] is JArray tags) {
                        foreach (JToken t in tags) { w.tags.Add((string)t); }
                    }
                    if (!string.IsNullOrEmpty(w.widgetId)) { result.Add(w); }
                }
            } catch { /* malformed response -> empty list */ }

            return result.ToArray();
        }
    }
}
