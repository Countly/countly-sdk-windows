using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CountlySDK.Server.Responses;
using Newtonsoft.Json.Linq;

namespace CountlySDK.CountlyCommon.Server.Responses
{
    internal class RequestResult
    {
        public ResultResponse parsedResponse = null;
        public string responseText = null;
        public int responseCode = -1;

        public bool IsSuccess()
        {
            return responseCode >= 200 && responseCode < 300 && responseText != null && JObject.Parse(responseText).ContainsKey("result");
        }

        public bool IsBadRequest()
        {
            if (responseCode == 400 || responseCode == 404) {
                return true;
            }
            return false;
        }
    }
}
