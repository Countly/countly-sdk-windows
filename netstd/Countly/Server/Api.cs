using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon.Server;
using CountlySDK.CountlyCommon.Server.Responses;
using CountlySDK.Helpers;

namespace CountlySDK
{
    internal class Api : ApiBase
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly Api instance = new Api();
        // Explicit static constructor to tell C# compiler
        // not to mark type as
        // fieldinit    
        static Api() { }
        internal Api() { }
        public static Api Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        // Shared across all requests. Constructing an HttpClient per request exhausts sockets
        // under load, because each instance holds its own connection pool that lingers in
        // TIME_WAIT. Per-request data (custom headers) is applied to the HttpContent, so this
        // client carries no per-request state and is safe to reuse.
        private static readonly HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

        protected override async Task<RequestResult> Call(string address, string requestData, Stream imageData = null, string endpoint = sdkEndpoint)
        {
            return await Task.Run<RequestResult>(async () => {
                return await CallJob(address, requestData, endpoint, imageData);
            }).ConfigureAwait(false);
        }

        protected override async Task<RequestResult> RequestAsync(string address, string requestData = null, Stream imageData = null, IDictionary<string, string> customNetworkHeaders = null)
        {
            RequestResult requestResult = new RequestResult();
            try {
                //make sure stream is at start
                imageData?.Seek(0, SeekOrigin.Begin);
                HttpContent httpContent = (imageData != null) ? new StreamContent(imageData) : null;

                if (requestData != null) {
                    Stream requestStream = new MemoryStream(Encoding.UTF8.GetBytes(requestData));
                    httpContent = new StreamContent(requestStream);
                    httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                }

                if (httpContent != null && customNetworkHeaders != null && customNetworkHeaders.Count > 0) {
                    foreach (KeyValuePair<string, string> pair in customNetworkHeaders) {
                        httpContent.Headers.Add(pair.Key, pair.Value);
                    }
                }

                HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(address, httpContent);

                requestResult.responseText = await httpResponseMessage.Content.ReadAsStringAsync();
                requestResult.responseCode = (int)httpResponseMessage.StatusCode;

                return requestResult;
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("Encountered a exception while making a POST request, " + ex.ToString());
                return requestResult;
            }
        }
    }
}
