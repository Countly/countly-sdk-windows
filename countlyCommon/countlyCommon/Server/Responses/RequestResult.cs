using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CountlySDK.Server.Responses;

namespace CountlySDK.CountlyCommon.Server.Responses
{
    internal class RequestResult
    {
        public ResultResponse parsedResponse = null;
        public String responseText = null;
        public int responseCode = -1;

        public bool IsSuccess()
        {
            if (parsedResponse != null) {
                return parsedResponse.IsSuccess;
            }
            return false;
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
