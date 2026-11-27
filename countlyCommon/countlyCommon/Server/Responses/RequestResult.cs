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

        /// <summary>
        /// Whether the server answered with a 2xx status and a JSON object or array, the rule the
        /// answer to a direct request is accepted by.
        /// </summary>
        /// <returns>true for a 2xx answer that is a JSON object or array</returns>
        public bool IsJsonAnswer()
        {
            if (!(responseCode >= 200 && responseCode < 300) || responseText == null || responseText.Trim().Length == 0) {
                return false;
            }

            try {
                JToken answer = JToken.Parse(responseText);
                return answer.Type == JTokenType.Object || answer.Type == JTokenType.Array;
            } catch (Exception) {
                return false;
            }
        }
    }
}
