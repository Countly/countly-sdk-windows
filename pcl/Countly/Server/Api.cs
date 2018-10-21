using CountlySDK.CountlyCommon.Server;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

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

        protected override async Task<T> Call<T>(string address, Stream data = null)
        {
            return await Task.Run<T>(async () =>
            {
                return await CallJob<T>(address, data);
            }).ConfigureAwait(false);
        }

        protected override async Task<string> RequestAsync(string address, Stream data = null)
        {
            try
            {
                UtilityHelper.CountlyLogging("POST " + address);
                
                //make sure stream is at start
                data?.Seek(0, SeekOrigin.Begin);

                HttpContent httpContent = (data != null) ? new StreamContent(data) : null;

                System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();

                System.Net.Http.HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(address, httpContent);

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
    }
}
