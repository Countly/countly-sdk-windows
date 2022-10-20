﻿using System;
using System.Collections.Specialized;
using System.Web;
using CountlySDK;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.Entities;
using Xunit;

namespace TestProject_common
{
    public class SessionTests : IDisposable
    {
        /// <summary>
        /// Test setup
        /// </summary>
        public SessionTests()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Halt();
            TestHelper.CleanDataFiles();
            Countly.Instance.deferUpload = false;
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {

        }

        private void ValidateSessionRequestParams(NameValueCollection collection, string appKey, string deviceId, string deviceIdType)
        {
            Assert.False(string.IsNullOrEmpty(collection.Get("sdk_version")));
            Assert.False(string.IsNullOrEmpty(collection.Get("sdk_name")));
            Assert.False(string.IsNullOrEmpty(collection.Get("dow")));
            Assert.False(string.IsNullOrEmpty(collection.Get("hour")));
            Assert.False(string.IsNullOrEmpty(collection.Get("tz")));
            Assert.False(string.IsNullOrEmpty(collection.Get("timestamp")));

            Assert.Equal(appKey, collection.Get("app_key"));
            Assert.Equal(deviceIdType, collection.Get("t"));
            Assert.Equal(deviceId, collection.Get("device_id"));
        }

        [Fact]
        /// <summary>
        /// It validates 'SessionBegin' method.
        /// </summary>
        public async void TestSessionBegin()
        {
            CountlyConfig cc = new CountlyConfig {
                serverUrl = "https://try.count.ly",
                appKey = "YOUR_APP_KEY",
                developerProvidedDeviceId = "device-id"
            };

            await Countly.Instance.Init(cc);
            await Countly.Instance.SessionBegin();

            StoredRequest model = Countly.Instance.StoredRequests.Dequeue();
            NameValueCollection collection = HttpUtility.ParseQueryString(model.Request.Substring(2));

            Assert.Equal("1", collection.Get("begin_session"));
            Assert.False(string.IsNullOrEmpty(collection.Get("metrics")));
            ValidateSessionRequestParams(collection, "YOUR_APP_KEY", "device-id", "0");
        }

        [Fact]
        /// <summary>
        /// It validates 'SessionUpdate' method.
        /// </summary>
        public async void TestSessionUpdate()
        {
            CountlyConfig cc = new CountlyConfig {
                serverUrl = "https://try.count.ly",
                appKey = "YOUR_APP_KEY",
                developerProvidedDeviceId = "device-id"
            };

            await Countly.Instance.Init(cc);
            await Countly.Instance.SessionUpdate(60);

            StoredRequest model = Countly.Instance.StoredRequests.Dequeue();
            NameValueCollection collection = HttpUtility.ParseQueryString(model.Request.Substring(2));

            Assert.Equal("60", collection.Get("session_duration"));
            ValidateSessionRequestParams(collection, "YOUR_APP_KEY", "device-id", "0");
        }

        [Fact]
        /// <summary>
        /// It validates 'SessionEnd' method.
        /// </summary>
        public async void TestSessionEnd()
        {
            CountlyConfig cc = new CountlyConfig {
                serverUrl = "https://try.count.ly",
                appKey = "YOUR_APP_KEY",
                developerProvidedDeviceId = "device-id"
            };

            await Countly.Instance.Init(cc);
            await Countly.Instance.SessionBegin();
            Countly.Instance.StoredRequests.Clear();

            await Countly.Instance.SessionEnd();

            StoredRequest model = Countly.Instance.StoredRequests.Dequeue();
            NameValueCollection collection = HttpUtility.ParseQueryString(model.Request.Substring(2));

            Assert.Equal("1", collection.Get("end_session"));
            Assert.False(string.IsNullOrEmpty(collection.Get("session_duration")));
            ValidateSessionRequestParams(collection, "YOUR_APP_KEY", "device-id", "0");
        }
    }
}
