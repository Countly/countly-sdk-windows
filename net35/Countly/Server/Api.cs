using CountlySDK.CountlyCommon.Server;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CountlySDK.CountlyCommon.Server.Responses;

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
        /// <param name="data"></param>
        /// <returns></returns>
        protected override async Task<RequestResult> Call(string address, Stream data = null)
        {
            return await TaskEx.Run(async () => {
                return await CallJob(address, data);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Platform specific networking code
        /// </summary>
        /// <param name="address"></param>
        /// <param name="requestData"></param>
        /// <param name="imageData"></param>
        /// <returns></returns>
        protected override async Task<RequestResult> RequestAsync(string address, String requestData = null, Stream imageData = null)
        {
            Stream dataStream = null;
            RequestResult requestResult = new RequestResult();
            try {
                UtilityHelper.CountlyLogging("POST " + address);

                //make sure stream is at start
                imageData?.Seek(0, SeekOrigin.Begin);

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
                request.Method = "POST";
                request.ContentType = "application/json";

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

                var response = (HttpWebResponse)request.GetResponse();
                requestResult.responseCode = (int)response.StatusCode;
                requestResult.responseText = new StreamReader(response.GetResponseStream()).ReadToEnd();

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

        protected override async Task DoSleep(int sleepTime)
        {
            Thread.Sleep(sleepTime);
        }
    }
}
