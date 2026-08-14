using System;
using Newtonsoft.Json.Linq;

namespace CountlySDK.CountlyCommon.Server.Responses
{
    internal class RequestResult
    {
        public string responseText = null;
        public int responseCode = -1;

        public bool IsSuccess()
        {
            if (!(responseCode >= 200 && responseCode < 300)) {
                return false;
            }

            if (responseText == null || responseText.Length == 0) {
                return false;
            }

            try {
                return JObject.Parse(responseText).ContainsKey("result");
            } catch (Exception ex) {
                CountlySDK.Helpers.UtilityHelper.CountlyLogging("[RequestResult] IsSuccess, could not parse response as JSON: " + ex.Message);
            }

            return false;
        }
    }
}
