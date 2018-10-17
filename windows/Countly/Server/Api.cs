using CountlySDK.CountlyCommon.Server;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using HttpClient = Windows.Web.Http.HttpClient;
using HttpStreamContent = Windows.Web.Http.HttpStreamContent;

namespace CountlySDK
{
    internal class Api : ApiBase
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly Api instance = new Api();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static Api() { }
        internal Api() { }
        public static Api Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        private static HttpClient _httpClient;

        internal static HttpClient Client
        {
            get {
                if (_httpClient == null)
                {
                    _httpClient = new HttpClient();
                }
                return _httpClient;
            }
        }

        protected override Task<T> Call<T>(string address, Stream data = null)
        {
            return Task.Run<T>(async () =>
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
            });
        }

        private static async Task<string> RequestAsync(string address, Stream data = null)
        {
            try
            {
                if (Countly.IsLoggingEnabled)
                {
                    Debug.WriteLine("POST " + address);
                }

                var httpResponseMessage = await Client.PostAsync(new Uri(address), (data != null) ? new HttpStreamContent(data.AsInputStream()) : null);

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    return await httpResponseMessage.Content.ReadAsStringAsync();
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                if (Countly.IsLoggingEnabled)
                {
                    //Debug.WriteLine("Encountered a exception while making a POST request");
                    //Debug.WriteLine(ex);
                }
                return null;
            }
        }
    }
}
