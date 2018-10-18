using CountlySDK.CountlyCommon.Server;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
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

        protected override Task<T> Call<T>(string address, Stream data = null)
        {
            return Task.Run<T>(async () =>
            {
                TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

                try
                {
                    string responseJson = await RequestAsync(address, data);

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

        private static async Task<string> RequestAsync(string address, Stream data = null)
        {
            try
            {
                UtilityHelper.CountlyLogging("POST " + address);                

                System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();

                System.Net.Http.HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(address, (data != null) ? new StreamContent(data) : null);

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
                UtilityHelper.CountlyLogging(ex.ToString());
                return null;
            }
        }
    }
}
