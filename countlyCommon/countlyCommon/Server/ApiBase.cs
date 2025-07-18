using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.CountlyCommon.Server.Responses;
using CountlySDK.Entities;
using CountlySDK.Helpers;

namespace CountlySDK.CountlyCommon.Server
{
    abstract class ApiBase
    {
        internal const int maxLengthForDataInUrl = 2000;
        internal const string sdkEndpoint = "/i";
        internal string tamperingProtectionSalt = null;
        internal IDictionary<string, string> customNetworkRequestHeaders = null;

        public async Task<RequestResult> SendSession(string serverUrl, int rr, SessionEvent sessionEvent, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = string.Empty;

            if (userDetails != null) {
                userDetailsJson = "&user_details=" + UtilityHelper.EncodeDataForURL(RequestHelper.Json(userDetails));
            }

            return await Call(serverUrl, string.Format("{0}{1}&rr={2}", sessionEvent.Content, userDetailsJson, rr));
        }

        public async Task<RequestResult> SendEvents(string serverUrl, RequestHelper requestHelper, int rr, List<CountlyEvent> events, CountlyUserDetails userDetails = null)
        {
            string eventsJson = RequestHelper.Json(events);

            string userDetailsJson = string.Empty;

            if (userDetails != null) {
                userDetailsJson = "&user_details=" + UtilityHelper.EncodeDataForURL(RequestHelper.Json(userDetails));
            }

            return await Call(serverUrl, string.Format("{0}&events={1}{2}&rr={3}", await requestHelper.BuildRequest(), UtilityHelper.EncodeDataForURL(eventsJson), userDetailsJson, rr));
        }

        public async Task<RequestResult> SendException(string serverUrl, RequestHelper requestHelper, int rr, ExceptionEvent exception)
        {
            string exceptionJson = UtilityHelper.EncodeDataForURL(RequestHelper.Json(exception));
            return await Call(serverUrl, string.Format("{0}&crash={1}&rr={2}", await requestHelper.BuildRequest(), exceptionJson, rr));
        }

        public async Task<RequestResult> SendUserDetails(string serverUrl, RequestHelper requestHelper, int rr, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = string.Empty;

            if (userDetails != null) {
                userDetailsJson = UtilityHelper.EncodeDataForURL(RequestHelper.Json(userDetails));
            }

            return await Call(serverUrl, string.Format("{0}&user_details={1}&rr={2}", await requestHelper.BuildRequest(), userDetailsJson, rr));
        }

        public async Task<RequestResult> SendUserPicture(string serverUrl, RequestHelper requestHelper, int rr, Stream imageStream, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = string.Empty;

            if (userDetails != null) {
                userDetailsJson = "=" + UtilityHelper.EncodeDataForURL(RequestHelper.Json(userDetails));
            }

            return await Call(serverUrl, string.Format("{0}&user_details={1}&rr={2}", await requestHelper.BuildRequest(), userDetailsJson, rr), imageStream);
        }

        public async Task<RequestResult> SendStoredRequest(string serverUrl, StoredRequest request, int rr)
        {
            Debug.Assert(serverUrl != null);
            Debug.Assert(request != null);

            return await Call(serverUrl, string.Format("{0}&rr={1}", request.Request, rr));
        }

        /// <summary>
        /// Platform specific task wrapper
        /// </summary>
        /// <param name="address"></param>
        /// <param name="requestData"></param>
        /// <param name="imageData"></param>
        /// <returns></returns>
        protected abstract Task<RequestResult> Call(string address, string requestData, Stream imageData = null);

        /// <summary>
        /// Common job handler
        /// </summary>
        /// <param name="address"></param>
        /// <param name="requestData"></param>
        /// <param name="imageData"></param>
        /// <param name="customHeaders"></param>
        /// <returns></returns>
        protected async Task<RequestResult> CallJob(string address, string requestData, Stream imageData = null)
        {
            Debug.Assert(address != null);
            TaskCompletionSource<RequestResult> tcs = new TaskCompletionSource<RequestResult>();
            if (requestData.StartsWith("/i?")) { // for migrating old requests
                requestData = requestData.Replace("/i?", "");
            }
            requestData = AddChekcsum(requestData);
            UtilityHelper.CountlyLogging(string.Format("[ApiBase] CallJob, address: [{0}], endpoint: [{1}] requestData: [{2}]", address, sdkEndpoint, requestData));

            try {
                RequestResult requestResult = await RequestAsync(address + sdkEndpoint, requestData, imageData, customNetworkRequestHeaders);
                tcs.SetResult(requestResult);

                if (requestResult.responseText != null) {
                    UtilityHelper.CountlyLogging(requestResult.responseText);
                } else {
                    UtilityHelper.CountlyLogging("Received null response");
                }
            } catch (Exception ex) {
                RequestResult requestResult = new RequestResult();
                requestResult.responseText = "Encountered an exception while making a request, " + ex;
                UtilityHelper.CountlyLogging(requestResult.responseText);
            }

            return await tcs.Task;
        }

        private string AddChekcsum(string data)
        {
            if (tamperingProtectionSalt == null || tamperingProtectionSalt.Length == 0) {
                return data;
            }
            string decodedData = data;
            UtilityHelper.CountlyLogging(decodedData);
            using (SHA256 sha256 = SHA256.Create()) {
                byte[] bytes = Encoding.UTF8.GetBytes(decodedData + tamperingProtectionSalt);
                byte[] hash = sha256.ComputeHash(bytes);

                var sb = new StringBuilder();
                foreach (byte b in hash) {
                    sb.Append(b.ToString("x2"));

                }
                data = data + "&checksum256=" + sb.ToString();
                return data;
            }
        }

        /// <summary>
        /// Platform specific networking code
        /// </summary>
        /// <param name="address"></param>
        /// <param name="requestData"></param>
        /// <param name="imageData"></param>
        /// <param name="customHeaders"></param>
        /// <returns></returns>
        protected abstract Task<RequestResult> RequestAsync(string address, string requestData, Stream imageData = null, IDictionary<string, string> customHeaders = null);
    }
}
