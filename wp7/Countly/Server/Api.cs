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
using System.Threading;

namespace CountlySDK
{
    internal class Api
    {
        public static void BeginSession(string serverUrl, string appKey, string deviceId, string sdkVersion, string metricsJson, Action<ResultResponse> callback)
        {
            Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&sdk_version={3}&begin_session=1&metrics={4}", serverUrl, appKey, deviceId, sdkVersion, HttpUtility.UrlEncode(metricsJson)), null, callback);
        }

        public static void UpdateSession(string serverUrl, string appKey, string deviceId, int duration, Action<ResultResponse> callback)
        {
            Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&session_duration={3}", serverUrl, appKey, deviceId, duration), null, callback);
        }

        public static void EndSession(string serverUrl, string appKey, string deviceId, Action<ResultResponse> callback)
        {
            Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&end_session=1", serverUrl, appKey, deviceId), null, callback);
        }

        public static void SendSession(string serverUrl, SessionEvent sesisonEvent, CountlyUserDetails userDetails, Action<ResultResponse> callback)
        {
            string userDetailsJson = String.Empty;

            if (userDetails != null)
            {
                userDetailsJson = "&user_details=" + HttpUtility.UrlEncode(JsonConvert.SerializeObject(userDetails, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
            }

            Call<ResultResponse>(serverUrl + sesisonEvent.Content + userDetailsJson, null, callback);
        }

        public static void SendEvents(string serverUrl, string appKey, string deviceId, List<CountlyEvent> events, CountlyUserDetails userDetails, Action<ResultResponse> callback)
        {
            string eventsJson = JsonConvert.SerializeObject(events, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            string userDetailsJson = String.Empty;

            if (userDetails != null)
            {
                userDetailsJson = "&user_details=" + HttpUtility.UrlEncode(JsonConvert.SerializeObject(userDetails, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
            }

            Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&events={3}{4}", serverUrl, appKey, deviceId, HttpUtility.UrlEncode(eventsJson), userDetailsJson), null, callback);
        }

        public static void SendException(string serverUrl, string appKey, string deviceId, ExceptionEvent exception, Action<ResultResponse> callback)
        {
            string exceptionJson = JsonConvert.SerializeObject(exception, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&crash={3}", serverUrl, appKey, deviceId, HttpUtility.UrlEncode(exceptionJson)), null, callback);
        }

        public static void UploadUserDetails(string serverUrl, string appKey, string deviceId, CountlyUserDetails userDetails, Action<ResultResponse> callback)
        {
            string userDetailsJson = String.Empty;

            if (userDetails != null)
            {
                userDetailsJson = JsonConvert.SerializeObject(userDetails, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }

            Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&user_details={3}", serverUrl, appKey, deviceId, userDetailsJson), null, callback);
        }

        public static void UploadUserPicture(string serverUrl, string appKey, string deviceId, Stream imageStream, CountlyUserDetails userDetails, Action<ResultResponse> callback)
        {
            string userDetailsJson = String.Empty;

            if (userDetails != null)
            {
                userDetailsJson = "=" + JsonConvert.SerializeObject(userDetails, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }

            Call<ResultResponse>(String.Format("{0}/i?app_key={1}&device_id={2}&user_details{3}", serverUrl, appKey, deviceId, userDetailsJson), imageStream, callback);
        }

        private static void Call<T>(string address, Stream data, Action<T> callback)
        {
            ThreadPool.QueueUserWorkItem((a) =>
            {
                try
                {
                    RequestAsync(address, data, (responseJson) =>
                    {
                        try
                        {
                            if (Countly.IsLoggingEnabled)
                            {
                                Debug.WriteLine(responseJson);
                            }

                            T response = JsonConvert.DeserializeObject<T>(responseJson);

                            callback(response);
                        }
                        catch
                        {
                            callback(default(T));
                        }
                    });
                }
                catch (Exception ex)
                {
                    callback(default(T));
                }
            });
        }

        private static void RequestAsync(string address, Stream data, Action<string> callback)
        {
            if (Countly.IsLoggingEnabled)
            {
                Debug.WriteLine("POST " + address);
            }

            HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp(address);

            httpWebRequest.Method = "POST";

            httpWebRequest.BeginGetRequestStream((a) =>
            {
                try
                {
                    Stream stream = httpWebRequest.EndGetRequestStream(a);

                    if (data != null)
                    {
                        data.CopyTo(stream);
                    }

                    stream.Flush();

                    httpWebRequest.BeginGetResponse((r) =>
                    {
                        try
                        {
                            WebResponse webResponse = httpWebRequest.EndGetResponse(r);

                            Stream responseStream = webResponse.GetResponseStream();

                            StreamReader streamReader = new StreamReader(responseStream);

                            string response = streamReader.ReadToEnd();

                            callback(response);
                        }
                        catch
                        {
                            callback(null);
                        }
                    }, httpWebRequest);
                }
                catch
                {
                    callback(null);
                }
            }, httpWebRequest);
        }
    }
}
