using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if !RUNNING_ON_35
using Xunit.Abstractions;
#endif

namespace TestProject_common
{
    public class MockHttpServer : IDisposable
    {
        private readonly HttpListener _listener;
#if !RUNNING_ON_35
        private readonly ITestOutputHelper _output;
#endif

        private readonly List<RequestInfo> _requests = new List<RequestInfo>();

        public string Url { get; }
#if !RUNNING_ON_35
        public IReadOnlyList<RequestInfo> Requests => _requests;
#else
        public IList<RequestInfo> Requests => _requests;
#endif

        public MockHttpServer() : this(null)
        {
        }

        public MockHttpServer(object output)
        {
            int port = GetRandomUnusedPort();
            Url = $"http://localhost:{port}/";

#if !RUNNING_ON_35

            if (output is ITestOutputHelper) {
                _output = (ITestOutputHelper)output;
            }
#endif

            _listener = new HttpListener();
            _listener.Prefixes.Add(Url);
            _listener.Start();

#if !RUNNING_ON_35
            Task.Run(() => ListenLoop());
#else
            var thread = new Thread(() => ListenLoop());
            thread.IsBackground = true;
            thread.Start();
#endif
        }

        private async Task ListenLoop()
        {
            while (_listener.IsListening) {
                try {
#if !RUNNING_ON_35
                    var ctx = await _listener.GetContextAsync();
#else
                    var ctx = _listener.GetContext();
#endif

                    var reader = new StreamReader(ctx.Request.InputStream);
                    string body = reader.ReadToEnd();
#if !RUNNING_ON_35
                    _output.WriteLine($"[{DateTime.Now:HH:mm:ss}] {ctx.Request.HttpMethod} {ctx.Request.RawUrl} Body: {body}");
#endif
                    _requests.Add(new RequestInfo {
                        Path = ctx.Request.RawUrl,
                        Method = ctx.Request.HttpMethod,
                        Body = body,
                        Params = TestHelper.GetParams(body),
                    });

                    // Always respond 200 OK for now
                    string json = "{\"result\":\"success\"}";
                    byte[] resp = Encoding.UTF8.GetBytes(json);
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    ctx.Response.ContentLength64 = resp.Length;
#if !RUNNING_ON_35
                    await ctx.Response.OutputStream.WriteAsync(resp, 0, resp.Length);
#else
                    ctx.Response.OutputStream.Write(resp, 0, resp.Length);
#endif
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
