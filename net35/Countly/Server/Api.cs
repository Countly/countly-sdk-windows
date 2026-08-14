using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
        // not to mark type as beforefieldinit    
        static Api() { }
        internal Api() { }
        public static Api Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        /// <summary>
        /// Platform specific task wrapper
        /// </summary>
        /// <param name="address"></param>
        /// <param name="requestData"></param>
        /// <param name="imageData"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        protected override async Task<RequestResult> Call(string address, string requestData, Stream imageData = null, string endpoint = sdkEndpoint)
        {
            return await TaskEx.Run(async () => {
                return await CallJob(address, requestData, endpoint, imageData);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Platform specific networking code
        /// </summary>
        /// <param name="address"></param>
        /// <param name="requestData"></param>
        /// <param name="imageData"></param>
        /// <param name="customHeaders"></param>
        /// <returns></returns>
        protected override async Task<RequestResult> RequestAsync(string address, String requestData = null, Stream imageData = null, IDictionary<string, string> customHeaders = null)
        {
            Stream dataStream = null;
            RequestResult requestResult = new RequestResult();
            try {
                //make sure stream is at start
                imageData?.Seek(0, SeekOrigin.Begin);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
                request.Method = "POST";
                request.ContentType = "application/json";
                if (customHeaders != null && customHeaders.Count > 0) {
                    foreach (KeyValuePair<string, string> kv in customHeaders) {
                        request.Headers.Add(kv.Key, kv.Value);
                    }

                }

                if (imageData != null) {
                    dataStream = imageData;
                }

                if (requestData != null) {
                    request.ContentType = "application/x-www-form-urlencoded";
                    dataStream = UtilityHelper.GenerateStreamFromString(requestData);
                }

                if (dataStream != null) {
                    using (var stream = request.GetRequestStream()) {
                        CopyStream(dataStream, stream);
                        stream.Flush();
                    }
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var responseStream = response.GetResponseStream())
                using (var reader = new StreamReader(responseStream)) {
                    requestResult.responseCode = (int)response.StatusCode;
                    requestResult.responseText = reader.ReadToEnd();
                }

                return requestResult;
            } catch (WebException wex) {
                // GetResponse() throws on 4xx/5xx. Recover the real status code and body from
                // the exception's response instead of leaving responseCode at -1.
                UtilityHelper.CountlyLogging("Encountered a WebException while making a POST request, " + wex.ToString());
                HttpWebResponse errorResponse = wex.Response as HttpWebResponse;
                if (errorResponse != null) {
                    using (errorResponse)
                    using (var errorStream = errorResponse.GetResponseStream())
                    using (var errorReader = new StreamReader(errorStream)) {
                        requestResult.responseCode = (int)errorResponse.StatusCode;
                        requestResult.responseText = errorReader.ReadToEnd();
                    }
                }
                return requestResult;
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("Encountered a exception while making a POST request, " + ex.ToString());
                return requestResult;
            } finally {
                if (dataStream != null) {
                    dataStream.Close();
                    dataStream.Dispose();
                }
            }
        }

        private static void CopyStream(Stream sourceStream, Stream targetStream)
        {
            byte[] buffer = new byte[0x10000];
            int n;
            while ((n = sourceStream.Read(buffer, 0, buffer.Length)) != 0) {
                targetStream.Write(buffer, 0, n);
            }
        }
    }
}
