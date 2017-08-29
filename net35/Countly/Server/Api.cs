using CountlySDK.Entities;
using CountlySDK.Server.Responses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace CountlySDK
{
    internal class Api
    {
        public static async Task<ResultResponse> BeginSession(string serverUrl, string appKey, string deviceId, string sdkVersion, string metricsJson)
        {
            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&sdk_version={3}&begin_session=1&metrics={4}", serverUrl, appKey, deviceId, sdkVersion, System.Uri.EscapeUriString(metricsJson)));
        }

        public static async Task<ResultResponse> UpdateSession(string serverUrl, string appKey, string deviceId, int duration)
        {
            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&session_duration={3}", serverUrl, appKey, deviceId, duration));
        }

        public static async Task<ResultResponse> EndSession(string serverUrl, string appKey, string deviceId)
        {
            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&end_session=1", serverUrl, appKey, deviceId));
        }

        public static async Task<ResultResponse> SendSession(string serverUrl, SessionEvent sesisonEvent, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = String.Empty;

            if (userDetails != null)
            {
                userDetailsJson = "&user_details=" + System.Uri.EscapeUriString(JsonConvert.SerializeObject(userDetails, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
            }

            return await Call<ResultResponse>(serverUrl + sesisonEvent.Content + userDetailsJson);
        }

        public static async Task<ResultResponse> SendEvents(string serverUrl, string appKey, string deviceId, List<CountlyEvent> events, CountlyUserDetails userDetails = null)
        {
            string eventsJson = JsonConvert.SerializeObject(events, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            string userDetailsJson = String.Empty;

            if (userDetails != null)
            {
                userDetailsJson = "&user_details=" + System.Uri.EscapeUriString(JsonConvert.SerializeObject(userDetails, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
            }

            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&events={3}{4}", serverUrl, appKey, deviceId, System.Uri.EscapeUriString(eventsJson), userDetailsJson));
        }

        public static async Task<ResultResponse> SendException(string serverUrl, string appKey, string deviceId, ExceptionEvent exception)
        {
            string exceptionJson = JsonConvert.SerializeObject(exception, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&crash={3}", serverUrl, appKey, deviceId, System.Uri.EscapeUriString(exceptionJson)));
        }

        public static async Task<ResultResponse> UploadUserDetails(string serverUrl, string appKey, string deviceId, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = String.Empty;

            if (userDetails != null)
            {
                userDetailsJson = JsonConvert.SerializeObject(userDetails, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }

            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&user_details={3}", serverUrl, appKey, deviceId, userDetailsJson));
        }

        public static async Task<ResultResponse> UploadUserPicture(string serverUrl, string appKey, string deviceId, Stream imageStream, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = String.Empty;

            if (userDetails != null)
            {
                userDetailsJson = "=" + JsonConvert.SerializeObject(userDetails, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }

            return await Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&user_details{3}", serverUrl, appKey, deviceId, userDetailsJson), imageStream);
        }

        private static async Task<T> Call<T>(string address, Stream data = null)
        {
            return await TaskEx.Run(async () =>
            {
                TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

                try
                {
                    string responseJson = await RequestAsync(address, data);

                    if (responseJson != null)
                    {
                        if (Countly.IsLoggingEnabled)
                        {
                            Debug.WriteLine(responseJson);
                        }

                        T response = JsonConvert.DeserializeObject<T>(responseJson);

                        tcs.SetResult(response);
                    }
                    else
                    {
                        if (Countly.IsLoggingEnabled)
                        {
                            Debug.WriteLine("Received null response");
                        }
                        tcs.SetResult(default(T));
                    }
                }
                catch (Exception ex)
                {
                    if (Countly.IsLoggingEnabled)
                    {
                        Debug.WriteLine("Encountered an exeption while making a request, " + ex);
                    }
                    tcs.SetResult(default(T));
                }

                return await tcs.Task;
            }).ConfigureAwait(false);
        }

        private static async Task<string> RequestAsync(string address, Stream data)
        {
            TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

            await Task.Factory.StartNew(() =>
            {
                try
                {
                    if (Countly.IsLoggingEnabled)
                    {
                        Debug.WriteLine("POST " + address);
                    }

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
                    request.Method = "POST";
                    request.ContentType = "application/json";

                    if (data != null)
                    {
                        using (var stream = request.GetRequestStream())
                        {
                            CopyStream(data, stream);

                            stream.Flush();
                        }
                    }

                    var response = (HttpWebResponse)request.GetResponse();

                    tcs.SetResult(new StreamReader(response.GetResponseStream()).ReadToEnd());
                }
                catch (Exception ex)
                {
                    if (Countly.IsLoggingEnabled)
                    {
                        //Debug.WriteLine("Encountered a exception while making a POST request");
                        //Debug.WriteLine(ex);
                    }
                    tcs.SetResult(null);
                }
            });

            return await tcs.Task;
        }

        private static void CopyStream(Stream sourceStream, Stream targetStream)
        {
            byte[] buffer = new byte[0x10000];
            int n;
            while ((n = sourceStream.Read(buffer, 0, buffer.Length)) != 0)
                targetStream.Write(buffer, 0, n);
        }
    }
}
