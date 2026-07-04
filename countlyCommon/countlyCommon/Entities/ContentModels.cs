using Newtonsoft.Json.Linq;

namespace CountlySDK.CountlyCommon
{
    public class ContentPlacement { public int X, Y, W, H; }

    public class ContentScreen { public int Width, Height; }

    public class ContentData
    {
        public string Url;
        public ContentPlacement Portrait;
        public ContentPlacement Landscape;
    }

    public static class ContentParser
    {
        /// <summary>Parses a /o/sdk/content response; returns null if it has no usable content.</summary>
        public static ContentData ParseContentResponse(string json)
        {
            if (string.IsNullOrEmpty(json)) { return null; }
            try {
                JObject root = JObject.Parse(json);
                string url = (string)root["html"];
                JObject geo = root["geo"] as JObject;
                if (string.IsNullOrEmpty(url) || geo == null) { return null; }

                ContentPlacement p = ToPlacement(geo["p"]);
                ContentPlacement l = ToPlacement(geo["l"]);
                if (p == null && l == null) { return null; }

                return new ContentData { Url = url, Portrait = p, Landscape = l };
            } catch {
                return null;
            }
        }

        private static ContentPlacement ToPlacement(JToken t)
        {
            if (!(t is JObject o)) { return null; }
            return new ContentPlacement {
                X = (int?)o["x"] ?? 0, Y = (int?)o["y"] ?? 0,
                W = (int?)o["w"] ?? 0, H = (int?)o["h"] ?? 0
            };
        }
    }
}
