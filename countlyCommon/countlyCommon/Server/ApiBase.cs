using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.CountlyCommon.Server.Responses;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace CountlySDK.CountlyCommon.Server
{
    abstract class ApiBase
    {
        internal const int maxLengthForDataInUrl = 2000;
        internal const string sdkEndpoint = "/i";
        internal string tamperingProtectionSalt = null;
        internal IDictionary<string, string> customNetworkRequestHeaders = null;
        internal const int requestCompressionMinBytes = 1024;
        // the server refuses a compressed body that is larger than this once decoded
        internal const int requestCompressionMaxBytes = 1024 * 1024;
        internal const string requestCompressionMarker = "content_encoding=gzip";
        private readonly object requestCompressionLock = new object();
        private bool requestCompressionEnabled = false;
        private string gzipAcceptingServerUrl = null;
        private bool requestCompressionBlocked = false;

        public async Task<RequestResult> SendSession(string serverUrl, int rr, SessionEvent sessionEvent, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = string.Empty;

            if (userDetails != null) {
                userDetailsJson = "&user_details=" + UtilityHelper.EncodeDataForURL(RequestHelper.Json(userDetails));
            }

            return await Call(serverUrl, string.Format("{0}{1}&rr={2}", sessionEvent.Content, userDetailsJson, rr));
        }

        public async Task<RequestResult> SendEvents(string serverUrl, RequestHelper requestHelper, int rr, List<CountlyEvent> events, CountlyUserDetails userDetails = null)
        {
            string eventsJson = RequestHelper.Json(events);

            string userDetailsJson = string.Empty;

            if (userDetails != null) {
                userDetailsJson = "&user_details=" + UtilityHelper.EncodeDataForURL(RequestHelper.Json(userDetails));
            }

            return await Call(serverUrl, string.Format("{0}&events={1}{2}&rr={3}", await requestHelper.BuildRequest(), UtilityHelper.EncodeDataForURL(eventsJson), userDetailsJson, rr));
        }

        public async Task<RequestResult> SendException(string serverUrl, RequestHelper requestHelper, int rr, ExceptionEvent exception)
        {
            string exceptionJson = UtilityHelper.EncodeDataForURL(RequestHelper.Json(exception));
            return await Call(serverUrl, string.Format("{0}&crash={1}&rr={2}", await requestHelper.BuildRequest(), exceptionJson, rr));
        }

        public async Task<RequestResult> SendUserDetails(string serverUrl, RequestHelper requestHelper, int rr, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = string.Empty;

            if (userDetails != null) {
                userDetailsJson = UtilityHelper.EncodeDataForURL(RequestHelper.Json(userDetails));
            }

            return await Call(serverUrl, string.Format("{0}&user_details={1}&rr={2}", await requestHelper.BuildRequest(), userDetailsJson, rr));
        }

        public async Task<RequestResult> SendUserPicture(string serverUrl, RequestHelper requestHelper, int rr, Stream imageStream, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = string.Empty;

            if (userDetails != null) {
                userDetailsJson = "=" + UtilityHelper.EncodeDataForURL(RequestHelper.Json(userDetails));
            }

            return await Call(serverUrl, string.Format("{0}&user_details={1}&rr={2}", await requestHelper.BuildRequest(), userDetailsJson, rr), imageStream);
        }

        public async Task<RequestResult> SendStoredRequest(string serverUrl, StoredRequest request, int rr)
        {
            Debug.Assert(serverUrl != null);
            Debug.Assert(request != null);

            return await Call(serverUrl, string.Format("{0}&rr={1}", request.Request, rr));
        }

        internal async Task<RequestResult> SendDirectRequest(string serverUrl, string requestPayload, string endpoint = "/o/sdk")
        {
            Debug.Assert(serverUrl != null);
            Debug.Assert(requestPayload != null);

            return await Call(serverUrl, string.Format("{0}", requestPayload), endpoint: endpoint);
        }

        /// <summary>
        /// Sets whether the developer turned request compression on, and forgets which server said it
        /// reads compressed bodies and whether a compressed request was refused; called at every init
        /// </summary>
        /// <param name="enabled">whether request compression is on</param>
        internal void ResetRequestCompression(bool enabled)
        {
            lock (requestCompressionLock) {
                requestCompressionEnabled = enabled;
                gzipAcceptingServerUrl = null;
                requestCompressionBlocked = false;
            }
        }

        /// <summary>
        /// Whether request compression is on, as the SDK Behavior Settings request asks the server
        /// about it only then
        /// </summary>
        internal bool RequestCompressionEnabled
        {
            get {
                lock (requestCompressionLock) {
                    return requestCompressionEnabled;
                }
            }
        }

        /// <summary>
        /// Takes the gzip answer of an SDK Behavior Settings fetch: "gzip": true from the server at
        /// serverUrl turns request compression on for that server, also after a refused compressed
        /// request had stopped it; an answer without it turns compression off
        /// </summary>
        /// <param name="serverUrl">the server the settings request went to</param>
        /// <param name="gzipAccepted">whether the answer had "gzip": true</param>
        internal void OnGzipAnswer(string serverUrl, bool gzipAccepted)
        {
            lock (requestCompressionLock) {
                if (!requestCompressionEnabled) {
                    return;
                }
                gzipAcceptingServerUrl = gzipAccepted ? serverUrl : null;
                if (gzipAccepted && requestCompressionBlocked) {
                    requestCompressionBlocked = false;
                    UtilityHelper.CountlyLogging("[ApiBase] OnGzipAnswer, a behavior settings answer says that the server reads compressed bodies, requests are compressed again", LogLevel.INFO);
                }
            }
            UtilityHelper.CountlyLogging("[ApiBase] OnGzipAnswer, the server " + (gzipAccepted ? "reads" : "does not say it reads") + " compressed request bodies");
        }

        /// <summary>
        /// Whether a request body to the given server may be sent compressed now: request compression
        /// is on, that server said it reads compressed bodies, and no compressed request was refused
        /// since its answer
        /// </summary>
        /// <param name="serverUrl">the server the request goes to</param>
        /// <returns>true when the body may be compressed</returns>
        private bool RequestCompressionAllowed(string serverUrl)
        {
            lock (requestCompressionLock) {
                return requestCompressionEnabled && !requestCompressionBlocked && gzipAcceptingServerUrl != null && gzipAcceptingServerUrl == serverUrl;
            }
        }

        /// <summary>
        /// Stops request compression after the server, or something in front of it, answered a
        /// compressed request without accepting it, until the next SDK Behavior Settings answer says
        /// that the server reads compressed bodies, or the SDK is initialized again
        /// </summary>
        private void BlockRequestCompression()
        {
            lock (requestCompressionLock) {
                if (requestCompressionBlocked) {
                    return;
                }
                requestCompressionBlocked = true;
            }
            UtilityHelper.CountlyLogging("[ApiBase] BlockRequestCompression, a compressed request was refused, requests are sent uncompressed until a behavior settings answer says that the server reads compressed bodies, or the SDK is initialized again", LogLevel.INFO);
        }

        /// <summary>
        /// Compresses a request body with gzip, for sending it with the compression marker as the URL query
        /// </summary>
        /// <param name="body">the url encoded request body, checksum included</param>
        /// <returns>the gzip bytes, or null when the body is smaller than 1 KB or larger than 1 MB,
        /// compressing fails, or the result is not smaller than the body</returns>
        internal static byte[] GzipRequestBody(string body)
        {
            byte[] plain = Encoding.UTF8.GetBytes(body);
            if (plain.Length < requestCompressionMinBytes || plain.Length > requestCompressionMaxBytes) {
                return null;
            }

            try {
                using (MemoryStream output = new MemoryStream()) {
                    using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress, true)) {
                        gzip.Write(plain, 0, plain.Length);
                    }
                    byte[] compressed = output.ToArray();
                    return compressed.Length < plain.Length ? compressed : null;
                }
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("[ApiBase] GzipRequestBody, compressing failed, the body is sent uncompressed: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Platform specific task wrapper
        /// </summary>
        /// <param name="address"></param>
        /// <param name="requestData"></param>
        /// <param name="imageData"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        protected abstract Task<RequestResult> Call(string address, string requestData, Stream imageData = null, string endpoint = sdkEndpoint);

        /// <summary>
        /// Common job handler
        /// </summary>
        /// <param name="address"></param>
        /// <param name="requestData"></param>
        /// <param name="imageData"></param>
        /// <param name="endpoint"></param>
        /// <returns></returns>
        protected async Task<RequestResult> CallJob(string address, string requestData, string endpoint, Stream imageData = null)
        {
            Debug.Assert(address != null);
            TaskCompletionSource<RequestResult> tcs = new TaskCompletionSource<RequestResult>();
            if (requestData.StartsWith("/i?")) { // for migrating old requests
                requestData = requestData.Replace("/i?", "");
            }
            requestData = AddChecksum(requestData);
            UtilityHelper.CountlyLogging(string.Format("[ApiBase] CallJob, address: [{0}], endpoint: [{1}] requestData: [{2}]", address, endpoint, requestData));

            string url = address + endpoint;
            byte[] compressedBody = null;
            if (imageData == null && url.IndexOf('?') == -1 && RequestCompressionAllowed(address)) {
                compressedBody = GzipRequestBody(requestData);
                if (compressedBody != null) {
                    url = url + "?" + requestCompressionMarker;
                    UtilityHelper.CountlyLogging("[ApiBase] CallJob, the request body is sent gzip-compressed, [" + Encoding.UTF8.GetByteCount(requestData) + "] -> [" + compressedBody.Length + "] bytes");
                }
            }

            try {
                RequestResult requestResult = await RequestAsync(url, requestData, imageData, customNetworkRequestHeaders, compressedBody);
                // a queued request is accepted with a "result", a direct one with any JSON answer
                if (compressedBody != null && requestResult.responseCode != -1 && !(endpoint == sdkEndpoint ? requestResult.IsSuccess() : requestResult.IsJsonAnswer())) {
                    BlockRequestCompression();
                }
                tcs.SetResult(requestResult);

                if (requestResult.responseText != null) {
                    UtilityHelper.CountlyLogging(requestResult.responseText);
                } else {
                    UtilityHelper.CountlyLogging("Received null response");
                }
            } catch (Exception ex) {
                RequestResult requestResult = new RequestResult();
                requestResult.responseText = "Encountered an exception while making a request, " + ex;
                UtilityHelper.CountlyLogging(requestResult.responseText);
            }

            return await tcs.Task;
        }

        private string AddChecksum(string data)
        {
            if (tamperingProtectionSalt == null || tamperingProtectionSalt.Length == 0) {
                return data;
            }
            string decodedData = data;
            UtilityHelper.CountlyLogging(decodedData);
            using (SHA256 sha256 = SHA256.Create()) {
                byte[] bytes = Encoding.UTF8.GetBytes(decodedData + tamperingProtectionSalt);
                byte[] hash = sha256.ComputeHash(bytes);

                var sb = new StringBuilder();
                foreach (byte b in hash) {
                    sb.Append(b.ToString("x2"));

                }
                data = data + "&checksum256=" + sb.ToString();
                return data;
            }
        }

        /// <summary>
        /// Platform specific networking code
        /// </summary>
        /// <param name="address"></param>
        /// <param name="requestData"></param>
        /// <param name="imageData"></param>
        /// <param name="customHeaders"></param>
        /// <param name="compressedBody">the gzip-compressed body to send instead of requestData, or null</param>
        /// <returns></returns>
        protected abstract Task<RequestResult> RequestAsync(string address, string requestData, Stream imageData = null, IDictionary<string, string> customHeaders = null, byte[] compressedBody = null);
    }
}
