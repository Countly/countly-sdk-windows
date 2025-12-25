using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestProject_common
{
    public class MockHttpServer : IDisposable
    {
        private readonly HttpListener _listener;

        private readonly List<RequestInfo> _requests = new List<RequestInfo>();

        public string Url { get; }
        public IList<RequestInfo> Requests => _requests;

        public MockHttpServer(Func<string, string> customResponse = null)
        {
            int port = GetRandomUnusedPort();
            Url = $"http://localhost:{port}/";

            _listener = new HttpListener();
            _listener.Prefixes.Add(Url);
            _listener.Start();

            var thread = new Thread(() => ListenLoop(customResponse));
            thread.IsBackground = true;
            thread.Start();
        }

        private async Task ListenLoop(Func<string, string> customResponse)
        {
            while (_listener.IsListening) {
                try {
                    var ctx = _listener.GetContext();
                    var reader = new StreamReader(ctx.Request.InputStream);
                    string body = reader.ReadToEnd();
                    _requests.Add(new RequestInfo {
                        Path = ctx.Request.RawUrl,
                        Method = ctx.Request.HttpMethod,
                        Body = body,
                        Params = TestHelper.GetParams(body),
                    });

                    // Always respond 200 OK for now
                    string json = customResponse?.Invoke(body);
                    if (json == null) {
                        json = "{\"result\":\"success\"}";
                    }
                    byte[] resp = Encoding.UTF8.GetBytes(json);
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    ctx.Response.ContentLength64 = resp.Length;
                    ctx.Response.OutputStream.Write(resp, 0, resp.Length);
                    ctx.Response.Close();
                    reader.Close();
                } catch { /* ignoring listener shutdown */ }
            }
        }

        private int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public void Dispose()
        {
            _listener?.Stop();
        }

        public class RequestInfo
        {
            public string Path;
            public string Method;
            public string Body;
            // parsed JSON as a dictionary
            public Dictionary<string, string> Params;
        }
    }
}
