using System.Collections.Generic;

namespace CountlySDK.CountlyCommon.Helpers
{
    /// <summary>
    /// Builds the content-specific query params for a /o/sdk/content fetch (base params are added
    /// by RequestHelper.BuildRequest). Public so it can be unit-tested directly. (Experimental.)
    /// </summary>
    public static class ContentRequestBuilder
    {
        public static IDictionary<string, object> BuildParams(ContentScreen screen, string[] categories,
            string language, string deviceType, string contentId)
        {
            int w = screen != null ? screen.Width : 0;
            int h = screen != null ? screen.Height : 0;
            // Desktop reports the same rect for both orientations.
            string resolution = "{\"l\":{\"w\":" + w + ",\"h\":" + h + "},\"p\":{\"w\":" + w + ",\"h\":" + h + "}}";

            IDictionary<string, object> p = new Dictionary<string, object> {
                { "method", "queue" },
                { "resolution", resolution },
                { "category", CategoryList(categories) },
                { "la", language ?? "" },
                { "dt", deviceType ?? "desktop" }
            };
            if (!string.IsNullOrEmpty(contentId)) {
                p.Add("content_id", contentId);
                p.Add("preview", "true");
            }
            return p;
        }

        private static string CategoryList(string[] categories)
        {
            if (categories == null || categories.Length == 0) { return "[]"; }
            string[] escaped = new string[categories.Length];
            for (int i = 0; i < categories.Length; i++) {
                escaped[i] = categories[i] == null ? "" : System.Uri.EscapeDataString(categories[i]);
            }
            return "[" + string.Join(", ", escaped) + "]";
        }
    }
}
