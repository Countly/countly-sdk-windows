using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Countly.UI.WebView2.Tests
{
    // Minimal self-contained HTTP mock (no dependency on the SDK's TestProject_common helpers,
    // which reach SDK internals not visible from this external assembly). Records requests and
    // returns JSON from an optional responder (rawUrl, body) => json.
    internal sealed class LocalMockServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Func<string, string, string> _responder;

        public string Url { get; }
        public List<Captured> Requests { get; } = new List<Captured>();

        public LocalMockServer(Func<string, string, string> responder = null)
        {
            _responder = responder;
            int port = FreePort();
            Url = $"http://localhost:{port}/";
            _listener = new HttpListener();
            _listener.Prefixes.Add(Url);
            _listener.Start();
            Thread t = new Thread(Loop) { IsBackground = true };
            t.Start();
        }

        private void Loop()
        {
            while (_listener.IsListening) {
                try {
                    HttpListenerContext ctx = _listener.GetContext();
                    string body;
                    using (StreamReader r = new StreamReader(ctx.Request.InputStream)) { body = r.ReadToEnd(); }
                    lock (Requests) { Requests.Add(new Captured { RawUrl = ctx.Request.RawUrl, Method = ctx.Request.HttpMethod, Body = body }); }

                    string json = _responder?.Invoke(ctx.Request.RawUrl, body) ?? "{\"result\":\"success\"}";
                    byte[] buf = Encoding.UTF8.GetBytes(json);
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    ctx.Response.ContentLength64 = buf.Length;
                    ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                    ctx.Response.Close();
                } catch { /* listener stopped */ }
            }
        }

        private static int FreePort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int p = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return p;
        }

        public void Dispose() { try { _listener.Stop(); } catch { /* listener already stopped/disposed; nothing to do */ } }

        public sealed class Captured
        {
            public string RawUrl;
            public string Method;
            public string Body;
        }
    }
}
