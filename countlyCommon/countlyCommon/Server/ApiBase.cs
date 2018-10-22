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
using System.Threading;
using System.Threading.Tasks;

namespace CountlySDK.CountlyCommon.Server
{
    abstract class ApiBase
    {
        internal int DeviceMergeWaitTime = 10000;

        public async Task<ResultResponse> BeginSession(string serverUrl, string appKey, string deviceId, string sdkVersion, string metricsJson)
        {
            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&sdk_version={3}&begin_session=1&metrics={4}", serverUrl, appKey, deviceId, sdkVersion, UtilityHelper.EncodeDataForURL(metricsJson)));
        }

        public async Task<ResultResponse> UpdateSession(string serverUrl, string appKey, string deviceId, int duration)
        {
            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&session_duration={3}", serverUrl, appKey, deviceId, duration));
        }

        public async Task<ResultResponse> EndSession(string serverUrl, string appKey, string deviceId)
        {
            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&end_session=1", serverUrl, appKey, deviceId));
        }

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
#if RUNNING_ON_35
                Thread.Sleep(DeviceMergeWaitTime);
#elif RUNNING_ON_40
                Thread.Sleep(DeviceMergeWaitTime);
#else
                System.Threading.Tasks.Task.Delay(DeviceMergeWaitTime).Wait();
#endif
            }

            return await Call<ResultResponse>(String.Format("{0}{1}", serverUrl, request.Request));
        }

        protected abstract Task<T> Call<T>(string address, Stream data = null);

        protected async Task<T> CallJob<T>(string address, Stream data = null)
        {
            Debug.Assert(address != null);
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            
            try
            {
                string responseJson = await RequestAsync(address, data);

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
                UtilityHelper.CountlyLogging("Encountered an exeption while making a request, " + ex);
                tcs.SetResult(default(T));
            }

            return await tcs.Task;
        }

        protected abstract Task<string> RequestAsync(string address, Stream data = null);
    }
}
