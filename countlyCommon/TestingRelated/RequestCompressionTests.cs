using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CountlySDK;
using CountlySDK.CountlyCommon.Server;
using CountlySDK.CountlyCommon.Server.Responses;
using CountlySDK.Entities;
using Xunit;

namespace TestProject_common
{
    /// <summary>
    /// Request compression, against a server on loopback that reads a body as gzip the way the Countly server does:
    /// only when the compression marker is the only query of the URL. The assertions are on what the server received.
    /// </summary>
    public class RequestCompressionTests : IDisposable
    {
        private const string Salt = "gzip-salt";
        private const int Big = 5000;
        private readonly List<GzipServer> servers = new List<GzipServer>();

        public RequestCompressionTests()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Halt();
            TestHelper.CleanDataFiles();
        }

        public void Dispose()
        {
            Countly.Halt();
            foreach (GzipServer server in servers) {
                server.Dispose();
            }
        }

        [Fact]
        /// <summary>Without EnableRequestCompression the settings request does not ask for gzip, and a big request goes plain.</summary>
        public void CompressionOff_SettingsRequestDoesNotAsk_RequestsArePlain()
        {
            GzipServer server = Start(true);
            Init(server, false);

            Received big = Send(server, "big", Big);

            Assert.DoesNotContain("gzip", server.Settings().Params.Keys);
            Assert.False(big.Compressed);
            Assert.Equal(Big, big.Params["payload"].Length);
        }

        [Fact]
        /// <summary>
        /// Once the server says it reads gzip, a body of at least 1 KB goes compressed to /i with the marker as the only
        /// query, as a form body without a Content-Encoding header, and its checksum is the one of the plain body.
        /// </summary>
        public void CompressionOn_ServerReadsGzip_BigRequestIsCompressed()
        {
            GzipServer server = Start(true);
            CountlyConfig cc = Config(server, true);
            cc.SetParamaterTamperingProtectionSalt(Salt);
            Countly.Instance.Init(cc).Wait();

            Received big = Send(server, "big", Big);

            Assert.Equal("1", server.Settings().Params["gzip"]);
            Assert.True(big.Compressed);
            Assert.Equal("/i", big.Path);
            Assert.True(big.BodyBytes < 1000, "the compressed body has " + big.BodyBytes + " bytes");
            Assert.Equal("application/x-www-form-urlencoded", big.ContentType);
            Assert.Null(big.ContentEncoding);
            Assert.Equal(new string('a', Big), big.Params["payload"]);
            int at = big.Body.IndexOf("&checksum256=", StringComparison.Ordinal);
            Assert.True(at > 0);
            Assert.Equal(Sha256(big.Body.Substring(0, at) + Salt), big.Body.Substring(at + "&checksum256=".Length));
        }

        [Fact]
        /// <summary>A body under 1 KB goes plain, and a bigger one after it is compressed.</summary>
        public void CompressionOn_SmallRequestStaysPlain()
        {
            GzipServer server = Start(true);
            Init(server, true);

            Received small = Send(server, "small", 300);
            Received big = Send(server, "big", Big);

            Assert.False(small.Compressed);
            Assert.True(small.BodyBytes < 1024);
            Assert.True(big.Compressed);
        }

        [Fact]
        /// <summary>A settings answer without the gzip key keeps every body plain.</summary>
        public void CompressionOn_ServerDoesNotSayGzip_RequestsArePlain()
        {
            GzipServer server = Start(false);
            Init(server, true);

            Received big = Send(server, "big", Big);

            Assert.Equal("1", server.Settings().Params["gzip"]);
            Assert.False(big.Compressed);
        }

        [Fact]
        /// <summary>A settings answer with empty settings and "gzip": true still turns compression on.</summary>
        public void CompressionOn_AnswerWithEmptySettings_StillTurnsCompressionOn()
        {
            GzipServer server = Start(true);
            server.SettingsAnswer = "{\"v\":1,\"t\":1,\"c\":{},\"gzip\":true}";
            Init(server, true);

            Assert.True(Send(server, "big", Big).Compressed);
        }

        [Fact]
        /// <summary>A body over 1 MB goes plain, as the server refuses a bigger compressed body, and compression stays on.</summary>
        public void CompressionOn_BodyOverOneMegabyteStaysPlain_CompressionStaysOn()
        {
            GzipServer server = Start(true);
            Init(server, true);

            Received huge = Send(server, "huge", ApiBase.requestCompressionMaxBytes);
            Received big = Send(server, "big", Big);

            Assert.False(huge.Compressed);
            Assert.True(huge.BodyBytes > ApiBase.requestCompressionMaxBytes);
            Assert.True(big.Compressed);
        }

        [Fact]
        /// <summary>A refused compressed request stays queued and goes plain on its next try, and so do the requests after it.</summary>
        public void CompressedRequestRefused_RetryAndLaterRequestsArePlain()
        {
            GzipServer server = Start(true);
            server.RefuseCompressed = 1;
            Init(server, true);

            Countly.Instance.AddRequest(RawRequest("refused", Big)).Wait();
            Assert.False(Countly.Instance.Upload().Result);
            Assert.True(Countly.Instance.Upload().Result);
            Received after = Send(server, "after", Big);

            List<Received> refused = server.WithMarker("refused");
            Assert.Equal(new[] { true, false }, refused.Select(r => r.Compressed).ToArray());
            Assert.Equal(Without(refused[0].Params, "checksum256"), Without(refused[1].Params, "checksum256"));
            Assert.False(after.Compressed);
        }

        [Fact]
        /// <summary>A queued compressed request answered with 200 but without "result" was not accepted: compression stops.</summary>
        public void CompressedRequestAnsweredWithoutResult_StopsCompression()
        {
            GzipServer server = Start(true);
            server.RefuseCompressed = 1;
            server.RefuseStatus = 200;
            server.RefuseAnswer = "{}";
            Init(server, true);

            Countly.Instance.AddRequest(RawRequest("no_result", Big)).Wait();
            Assert.False(Countly.Instance.Upload().Result);
            Assert.True(Countly.Instance.Upload().Result);

            Assert.Equal(new[] { true, false }, server.WithMarker("no_result").Select(r => r.Compressed).ToArray());
        }

        [Fact]
        /// <summary>
        /// After a refusal stopped compression, a settings answer without gzip leaves it stopped, and the next one with
        /// "gzip": true turns it on again.
        /// </summary>
        public void CompressedRequestRefused_NextGzipAnswerTurnsCompressionOnAgain()
        {
            GzipServer server = Start(true);
            server.RefuseCompressed = 1;
            Init(server, true);
            Countly.Instance.AddRequest(RawRequest("refused", Big)).Wait();
            Countly.Instance.Upload().Wait();
            Countly.Instance.Upload().Wait();

            server.Gzip = false;
            Countly.Instance.moduleServerConfig.FetchServerConfig().Wait();
            Received withoutGzipAnswer = Send(server, "without_gzip_answer", Big);

            server.Gzip = true;
            Countly.Instance.moduleServerConfig.FetchServerConfig().Wait();
            Received withGzipAnswer = Send(server, "with_gzip_answer", Big);

            Assert.Equal(new[] { true, false }, server.WithMarker("refused").Select(r => r.Compressed).ToArray());
            Assert.False(withoutGzipAnswer.Compressed);
            Assert.True(withGzipAnswer.Compressed);
        }

        [Fact]
        /// <summary>A compressed direct request answered with JSON leaves compression on.</summary>
        public void CompressedDirectRequestAnsweredWithJson_CompressionStaysOn()
        {
            Assert.True(DirectRequestKeepsCompression(200, "{\"color\":\"red\"}"));
        }

        [Fact]
        /// <summary>A compressed direct request answered with a page, as a proxy may send, stops compression.</summary>
        public void CompressedDirectRequestAnsweredWithPage_StopsCompression()
        {
            Assert.False(DirectRequestKeepsCompression(200, "<html><body>Bad gateway</body></html>"));
        }

        [Fact]
        /// <summary>A compressed direct request answered with nothing stops compression.</summary>
        public void CompressedDirectRequestAnsweredWithNothing_StopsCompression()
        {
            Assert.False(DirectRequestKeepsCompression(200, ""));
        }

        [Fact]
        /// <summary>A compressed direct request answered with an error status stops compression.</summary>
        public void CompressedDirectRequestAnsweredWithError_StopsCompression()
        {
            Assert.False(DirectRequestKeepsCompression(400, "{\"result\":\"Invalid request body\"}"));
        }

        [Fact]
        /// <summary>A compressed request that failed without an answer does not stop compression.</summary>
        public void CompressedRequestWithoutAnswer_CompressionStaysOn()
        {
            GzipServer server = Start(true);
            server.DropCompressed = true;
            Init(server, true);

            Countly.Instance.AddRequest(RawRequest("dropped", Big)).Wait();
            Assert.False(Countly.Instance.Upload().Result);
            server.DropCompressed = false;
            Assert.True(Countly.Instance.Upload().Result);

            List<Received> dropped = server.WithMarker("dropped");
            Assert.True(dropped.Count >= 2);
            Assert.All(dropped, r => Assert.True(r.Compressed));
        }

        [Fact]
        /// <summary>After a server URL change the requests go plain until the new server says it reads gzip.</summary>
        public void ServerUrlChange_NewServerGetsPlainRequestsUntilItSaysGzip()
        {
            GzipServer first = Start(true);
            GzipServer second = Start(true);
            second.HoldSettings = new ManualResetEventSlim(false);
            Init(first, true);

            Task change = Countly.Instance.ChangeServerUrl(second.Url);
            Assert.True(SpinWait.SpinUntil(() => second.All().Any(r => r.Params.ContainsKey("method") && r.Params["method"] == "sc"), 15000));
            Countly.Instance.AddRequest(RawRequest("before_answer", Big)).Wait();
            Countly.Instance.Upload().Wait();
            second.HoldSettings.Set();
            change.Wait();
            Received after = Send(second, "after_answer", Big);

            Assert.False(second.WithMarker("before_answer").Single().Compressed);
            Assert.Empty(first.WithMarker("before_answer"));
            Assert.True(after.Compressed);
        }

        [Fact]
        /// <summary>Bodies under 1 KB and over 1 MB are left alone, the ones between are compressed into gzip that decodes to the body.</summary>
        public void GzipRequestBody_Bounds()
        {
            Assert.Null(ApiBase.GzipRequestBody(new string('a', ApiBase.requestCompressionMinBytes - 1)));
            Assert.Null(ApiBase.GzipRequestBody(new string('a', ApiBase.requestCompressionMaxBytes + 1)));
            foreach (int size in new[] { ApiBase.requestCompressionMinBytes, ApiBase.requestCompressionMaxBytes }) {
                string body = "app_key=key&payload=" + new string('a', size - 20);
                byte[] compressed = ApiBase.GzipRequestBody(body);
                Assert.NotNull(compressed);
                Assert.True(compressed.Length < body.Length);
                Assert.Equal(body, Encoding.UTF8.GetString(Gunzip(compressed)));
            }
        }

        [Fact]
        /// <summary>A direct request's answer is accepted for a 2xx status with a JSON object or array, and for nothing else.</summary>
        public void IsJsonAnswer_OnlyTwoHundredsWithJson()
        {
            Assert.True(new RequestResult { responseCode = 200, responseText = "{\"a\":1}" }.IsJsonAnswer());
            Assert.True(new RequestResult { responseCode = 204, responseText = "[1]" }.IsJsonAnswer());
            Assert.False(new RequestResult { responseCode = 400, responseText = "{\"result\":\"x\"}" }.IsJsonAnswer());
            Assert.False(new RequestResult { responseCode = 200, responseText = "<html></html>" }.IsJsonAnswer());
            Assert.False(new RequestResult { responseCode = 200, responseText = "" }.IsJsonAnswer());
            Assert.False(new RequestResult { responseCode = 200, responseText = "\"text\"" }.IsJsonAnswer());
            Assert.False(new RequestResult { responseCode = -1, responseText = null }.IsJsonAnswer());
        }

        /// <summary>
        /// Sends a big remote config request directly, off the queue, as the SDK's direct requests go, answered with the
        /// given status and text, then tells whether a big request sent after it was compressed.
        /// </summary>
        private bool DirectRequestKeepsCompression(int status, string answer)
        {
            GzipServer server = Start(true);
            server.RemoteConfigStatus = status;
            server.RemoteConfigAnswer = answer;
            Init(server, true);

            Api.Instance.SendDirectRequest(server.Url, RawRequest("direct", Big) + "&method=rc").Wait();
            Received after = Send(server, "after_direct", Big);

            Received direct = server.WithMarker("direct").Single();
            Assert.True(direct.Compressed);
            Assert.Equal("/o/sdk", direct.Path);
            return after.Compressed;
        }

        private GzipServer Start(bool gzip)
        {
            GzipServer server = new GzipServer { Gzip = gzip };
            servers.Add(server);
            return server;
        }

        private static CountlyConfig Config(GzipServer server, bool compression)
        {
            CountlyConfig cc = TestHelper.GetConfig();
            cc.serverUrl = server.Url;
            cc.DisableHealthCheck();
            if (compression) {
                cc.EnableRequestCompression();
            }
            return cc;
        }

        private static void Init(GzipServer server, bool compression)
        {
            Countly.Instance.Init(Config(server, compression)).Wait();
        }

        /// <summary>Queues a request with a payload of the given length, told apart by its marker parameter, and sends the queue.</summary>
        private static Received Send(GzipServer server, string marker, int payloadLength)
        {
            Countly.Instance.AddRequest(RawRequest(marker, payloadLength)).Wait();
            Assert.True(Countly.Instance.Upload().Result, "the request with marker " + marker + " was not delivered");
            return server.WithMarker(marker).Last();
        }

        private static string RawRequest(string marker, int payloadLength)
        {
            return "app_key=" + TestHelper.APP_KEY + "&device_id=" + TestHelper.DEVICE_ID + "&marker=" + marker + "&payload=" + new string('a', payloadLength);
        }

        private static Dictionary<string, string> Without(Dictionary<string, string> parameters, string key)
        {
            Dictionary<string, string> copy = new Dictionary<string, string>(parameters);
            copy.Remove(key);
            copy.Remove("rr");
            return copy;
        }

        private static string Sha256(string text)
        {
            using (SHA256 sha = SHA256.Create()) {
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text)).Select(b => b.ToString("x2")));
            }
        }

        private static byte[] Gunzip(byte[] compressed)
        {
            using (MemoryStream input = new MemoryStream(compressed))
            using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
            using (MemoryStream output = new MemoryStream()) {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }

        /// <summary>One request as the server received it: the body decoded and its parameters read the way the Countly server reads them.</summary>
        public class Received
        {
            public string Path;
            public bool Compressed;
            public string Body;
            public int BodyBytes;
            public string ContentType;
            public string ContentEncoding;
            public Dictionary<string, string> Params;
        }

        /// <summary>
        /// A Countly server stand-in on loopback that records every request with its raw body. It answers the SDK behavior
        /// settings with gzip when Gzip is set, remote config with RemoteConfigStatus and RemoteConfigAnswer, and anything
        /// else with success; it can refuse compressed requests with RefuseStatus and RefuseAnswer, drop them, and hold
        /// its settings answer.
        /// </summary>
        public class GzipServer : IDisposable
        {
            private readonly HttpListener listener = new HttpListener();
            private readonly List<Received> received = new List<Received>();
            public volatile bool Gzip;
            public volatile string SettingsAnswer;
            public volatile int RemoteConfigStatus = 200;
            public volatile string RemoteConfigAnswer = "{}";
            public volatile int RefuseCompressed;
            public volatile int RefuseStatus = 400;
            public volatile string RefuseAnswer = "{\"result\":\"Invalid request body\"}";
            public volatile bool DropCompressed;
            public volatile ManualResetEventSlim HoldSettings;
            public string Url { get; }

            public GzipServer()
            {
                TcpListener probe = new TcpListener(IPAddress.Loopback, 0);
                probe.Start();
                int port = ((IPEndPoint)probe.LocalEndpoint).Port;
                probe.Stop();
                Url = "http://localhost:" + port;
                listener.Prefixes.Add(Url + "/");
                listener.Start();
                Thread thread = new Thread(Listen) { IsBackground = true };
                thread.Start();
            }

            public List<Received> All()
            {
                lock (received) {
                    return received.ToList();
                }
            }

            public List<Received> WithMarker(string marker)
            {
                return All().Where(r => r.Params.ContainsKey("marker") && r.Params["marker"] == marker).ToList();
            }

            public Received Settings()
            {
                return All().First(r => r.Params.ContainsKey("method") && r.Params["method"] == "sc");
            }

            private void Listen()
            {
                while (listener.IsListening) {
                    HttpListenerContext ctx;
                    try {
                        ctx = listener.GetContext();
                    } catch {
                        return;
                    }
                    ThreadPool.QueueUserWorkItem(_ => Handle(ctx));
                }
            }

            private void Handle(HttpListenerContext ctx)
            {
                try {
                    byte[] raw;
                    using (MemoryStream copy = new MemoryStream()) {
                        ctx.Request.InputStream.CopyTo(copy);
                        raw = copy.ToArray();
                    }
                    string query = ctx.Request.Url.Query.TrimStart('?');
                    bool compressed = query == ApiBase.requestCompressionMarker;
                    string body = Encoding.UTF8.GetString(compressed ? Gunzip(raw) : raw);
                    Received request = new Received {
                        Path = ctx.Request.Url.AbsolutePath,
                        Compressed = compressed,
                        Body = body,
                        BodyBytes = raw.Length,
                        ContentType = ctx.Request.ContentType,
                        ContentEncoding = ctx.Request.Headers["Content-Encoding"],
                        Params = TestHelper.GetParams(body),
                    };
                    lock (received) {
                        received.Add(request);
                    }

                    if (compressed && DropCompressed) {
                        ctx.Response.Abort();
                        return;
                    }

                    int status = 200;
                    string answer = "{\"result\":\"Success\"}";
                    string method;
                    request.Params.TryGetValue("method", out method);
                    if (method == "sc") {
                        HoldSettings?.Wait(15000);
                        answer = SettingsAnswer ?? (Gzip ? "{\"v\":1,\"t\":1,\"c\":{\"lkl\":100},\"gzip\":true}" : "{\"v\":1,\"t\":1,\"c\":{\"lkl\":100}}");
                    } else if (method == "rc") {
                        status = RemoteConfigStatus;
                        answer = RemoteConfigAnswer;
                    }
                    if (compressed && RefuseCompressed > 0) {
                        RefuseCompressed--;
                        status = RefuseStatus;
                        answer = RefuseAnswer;
                    }

                    byte[] output = Encoding.UTF8.GetBytes(answer);
                    ctx.Response.StatusCode = status;
                    ctx.Response.ContentType = "application/json";
                    ctx.Response.ContentLength64 = output.Length;
                    ctx.Response.OutputStream.Write(output, 0, output.Length);
                    ctx.Response.Close();
                } catch {
                    // a request whose connection broke is not recorded twice
                }
            }

            public void Dispose()
            {
                HoldSettings?.Set();
                listener.Stop();
                listener.Close();
            }
        }
    }
}
