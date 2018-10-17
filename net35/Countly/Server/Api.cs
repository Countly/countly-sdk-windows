using CountlySDK.CountlyCommon.Server;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
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
