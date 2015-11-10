using CountlySDK.Entities;
using CountlySDK.Server.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace CountlySDK
{
    internal class Api
    {
        public static async Task<ResultResponse> BeginSession(string serverUrl, string appKey, string deviceId, string sdkVersion, string metricsJson)
        {
            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&sdk_version={3}&begin_session=1&metrics={4}", serverUrl, appKey, deviceId, sdkVersion, HttpUtility.UrlEncode(metricsJson)));
        }

        public static async Task<ResultResponse> UpdateSession(string serverUrl, string appKey, string deviceId, int duration)
        {
            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&session_duration={3}", serverUrl, appKey, deviceId, duration));
        }

        public static async Task<ResultResponse> EndSession(string serverUrl, string appKey, string deviceId)
        {
            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&end_session=1", serverUrl, appKey, deviceId));
        }

        public static async Task<ResultResponse> SendSession(string serverUrl, SessionEvent sesisonEvent)
        {
            return await Call<ResultResponse>(serverUrl + sesisonEvent.Content);
        }

        public static async Task<ResultResponse> SendEvents(string serverUrl, string appKey, string deviceId, List<CountlyEvent> events)
        {
            string json = JsonConvert.SerializeObject(events, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&events={3}", serverUrl, appKey, deviceId, HttpUtility.UrlEncode(json)));
        }

        private static Task<T> Call<T>(string address)
        {
            return Task.Run<T>(async () =>
            {
                TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

                try
                {
                    string responseJson = await RequestAsync(address);

                    if (Countly.IsLoggingEnabled)
                    {
                        Debug.WriteLine(responseJson);
                    }

                    T response = JsonConvert.DeserializeObject<T>(responseJson);

                    tcs.SetResult(response);
                }
                catch (Exception ex)
                {
                    tcs.SetResult(default(T));
                }

                return await tcs.Task;
            });
        }

        private static async Task<string> RequestAsync(string address)
        {
            if (Countly.IsLoggingEnabled)
            {
                Debug.WriteLine(address);
            }

            TaskCompletionSource<string> taskCompletionSource = new TaskCompletionSource<string>();

            HttpWebRequest request = HttpWebRequest.CreateHttp(address);

            var taskGetResponse = Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null);

            taskGetResponse.Wait();

            using (var response = taskGetResponse.Result)
            {
                using (var stream = response.GetResponseStream())
                {
                    var reader = new StreamReader(stream);
                    taskCompletionSource.SetResult(reader.ReadToEnd());
                }
            }

            return await taskCompletionSource.Task;
        }
    }
}
