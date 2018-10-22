using CountlySDK.CountlyCommon.Server;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
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
                return await CallJob<T>(address, data);
            });
        }

        protected override async Task<string> RequestAsync(string address, Stream data = null)
        {
            try
            {
                UtilityHelper.CountlyLogging("POST " + address);

                //make sure stream is at start
                data?.Seek(0, SeekOrigin.Begin);

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
                UtilityHelper.CountlyLogging("Encountered a exception while making a POST request, " + ex.ToString());
                return null;
            }
        }

        protected override async Task DoSleep(int sleepTime)
        {
            System.Threading.Tasks.Task.Delay(DeviceMergeWaitTime).Wait();
        }
    }
}
