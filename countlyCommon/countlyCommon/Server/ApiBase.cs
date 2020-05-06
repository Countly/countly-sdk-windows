using CountlySDK.CountlyCommon.Entities;
using CountlySDK.CountlyCommon.Server;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using CountlySDK.Server.Responses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CountlySDK.CountlyCommon.Server
{
    abstract class ApiBase
    {
        internal int DeviceMergeWaitTime = 10000;

        internal const int maxLengthForDataInUrl = 2000;

        protected abstract Task DoSleep(int sleepTime);

        public async Task<ResultResponse> SendSession(string serverUrl, SessionEvent sesisonEvent, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = String.Empty;

            if (userDetails != null)
            {
                userDetailsJson = "&user_details=" + UtilityHelper.EncodeDataForURL(JsonConvert.SerializeObject(userDetails, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
            }

            return await Call<ResultResponse>(serverUrl + sesisonEvent.Content + userDetailsJson);
        }

        public async Task<ResultResponse> SendEvents(string serverUrl, string appKey, string deviceId, List<CountlyEvent> events, CountlyUserDetails userDetails = null)
        {
            string eventsJson = JsonConvert.SerializeObject(events, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            string userDetailsJson = String.Empty;

            if (userDetails != null)
            {
                userDetailsJson = "&user_details=" + UtilityHelper.EncodeDataForURL(JsonConvert.SerializeObject(userDetails, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
            }

            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&events={3}{4}", serverUrl, appKey, deviceId, UtilityHelper.EncodeDataForURL(eventsJson), userDetailsJson));
        }

        public async Task<ResultResponse> SendException(string serverUrl, string appKey, string deviceId, ExceptionEvent exception)
        {
            string exceptionJson = JsonConvert.SerializeObject(exception, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&crash={3}", serverUrl, appKey, deviceId, UtilityHelper.EncodeDataForURL(exceptionJson)));
        }

        public async Task<ResultResponse> UploadUserDetails(string serverUrl, string appKey, string deviceId, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = String.Empty;

            if (userDetails != null)
            {
                userDetailsJson = JsonConvert.SerializeObject(userDetails, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }

            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&user_details={3}", serverUrl, appKey, deviceId, userDetailsJson));
        }

        public async Task<ResultResponse> UploadUserPicture(string serverUrl, string appKey, string deviceId, Stream imageStream, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = String.Empty;

            if (userDetails != null)
            {
                userDetailsJson = "=" + JsonConvert.SerializeObject(userDetails, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }

            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&user_details{3}", serverUrl, appKey, deviceId, userDetailsJson), imageStream);
        }

        public async Task<ResultResponse> SendStoredRequest(string serverUrl, StoredRequest request)
        {
            Debug.Assert(serverUrl != null);
            Debug.Assert(request != null);

            if (request.IdMerge)
            {
                await DoSleep(DeviceMergeWaitTime);
            }

            return await Call<ResultResponse>(String.Format("{0}{1}", serverUrl, request.Request));
        }

        protected abstract Task<T> Call<T>(string address, Stream imageData = null);

        protected async Task<T> CallJob<T>(string address, Stream imageData = null)
        {
            Debug.Assert(address != null);
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            
            try
            {
                String rData = null;
                if (address.Length > maxLengthForDataInUrl)
                {
                    //request url was too long, split off the data and pass it as form data
                    String[] splitData = address.Split('?');
                    address = splitData[0];
                    rData = splitData[1];
                }

                string responseJson = await RequestAsync(address, rData, imageData);

                if (responseJson != null)
                {
                    UtilityHelper.CountlyLogging(responseJson);

                    T response = JsonConvert.DeserializeObject<T>(responseJson);
                    tcs.SetResult(response);
                }
                else
                {
                    UtilityHelper.CountlyLogging("Received null response");
                    tcs.SetResult(default(T));
                }
            }
            catch (Exception ex)
            {
                UtilityHelper.CountlyLogging("Encountered an exception while making a request, " + ex);
                tcs.SetResult(default(T));
            }

            return await tcs.Task;
        }

        protected abstract Task<string> RequestAsync(string address, String requestData, Stream imageData = null);
    }
}
