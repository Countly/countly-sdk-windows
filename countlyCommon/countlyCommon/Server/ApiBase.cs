using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.CountlyCommon.Server.Responses;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using static CountlySDK.Helpers.TimeHelper;

namespace CountlySDK.CountlyCommon.Server
{
    abstract class ApiBase
    {
        internal const int maxLengthForDataInUrl = 2000;

        public async Task<RequestResult> SendSession(string serverUrl, int rr, SessionEvent sessionEvent, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = string.Empty;

            if (userDetails != null) {
                userDetailsJson = "&user_details=" + UtilityHelper.EncodeDataForURL(RequestHelper.Json(userDetails));
            }

            return await Call(serverUrl, string.Format("{0}{1}&rr={2}", sessionEvent.Content + userDetailsJson, rr));
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

        public async Task<RequestResult> UploadUserDetails(string serverUrl, RequestHelper requestHelper, int rr, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = string.Empty;

            if (userDetails != null) {
                userDetailsJson = UtilityHelper.EncodeDataForURL(RequestHelper.Json(userDetails));
            }

            return await Call(serverUrl, string.Format("{0}&user_details={1}&rr={2}", await requestHelper.BuildRequest(), userDetailsJson, rr));
        }

        public async Task<RequestResult> UploadUserPicture(string serverUrl, RequestHelper requestHelper, int rr, Stream imageStream, CountlyUserDetails userDetails = null)
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
        /// <returns></returns>
        protected async Task<RequestResult> CallJob(string address, string requestData, Stream imageData = null)
        {
            Debug.Assert(address != null);
            TaskCompletionSource<RequestResult> tcs = new TaskCompletionSource<RequestResult>();

            try {
                RequestResult requestResult = await RequestAsync(address, requestData, imageData);
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

        /// <summary>
        /// Platform specific networking code
        /// </summary>
        /// <param name="address"></param>
        /// <param name="requestData"></param>
        /// <param name="imageData"></param>
        /// <returns></returns>
        protected abstract Task<RequestResult> RequestAsync(string address, string requestData, Stream imageData = null);
    }
}
