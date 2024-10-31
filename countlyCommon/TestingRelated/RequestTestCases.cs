using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using CountlySDK;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.Entities;
using CountlySDK.Entities.EntityBase;
using Xunit;
using static CountlySDK.CountlyCommon.Helpers.RequestHelper;
using static CountlySDK.Helpers.TimeHelper;

namespace TestProject_common
{
    public class RequestTestCases : IDisposable
    {
        long timestampForRequest = 1666683640551;
        TimeInstant timeInstantForRequest;

        IRequestHelper testRequestHelperInterface;

        /// <summary>
        /// Test setup
        /// </summary>
        public RequestTestCases()
        {
            TestHelper.CleanDataFiles();
            Countly.Halt();
            TestHelper.CleanDataFiles();
            Countly.Instance.deferUpload = true;
            timeInstantForRequest = TimeInstant.Get(timestampForRequest);

            testRequestHelperInterface = new TestRequestHelperInterface(timeInstantForRequest);
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {
        }

        [Fact]
        /// <summary>
        /// It validates base request parameters.
        /// </summary>
        public async void ValidateBaseRequestParams()
        {

            CountlyConfig cc = new CountlyConfig() {
                serverUrl = "https://try.count.ly/",
                appKey = "YOUR_APP_KEY",
                developerProvidedDeviceId = "test device id",
                deviceIdMethod = Countly.DeviceIdMethod.developerSupplied,
                appVersion = "1.0",
            };

            await Countly.Instance.Init(cc);
            Dictionary<string, object> baseParams = await Countly.Instance.requestHelper.GetBaseParams();

            Assert.Equal(10, baseParams.Count);

            Assert.Equal("YOUR_APP_KEY", baseParams["app_key"]);
            Assert.Equal("test device id", baseParams["device_id"]);
            Assert.Equal("24.1.1", baseParams["sdk_version"]);
            Assert.Equal(0, baseParams["t"]);
            Assert.Equal("1.0", baseParams["av"]);

            Assert.True(baseParams.ContainsKey("sdk_name"));
            Assert.True(baseParams.ContainsKey("timestamp"));
            Assert.True(baseParams.ContainsKey("dow"));
            Assert.True(baseParams.ContainsKey("hour"));
            Assert.True(baseParams.ContainsKey("tz"));
        }

        [Fact]
        /// <summary>
        /// It validates request builder.
        /// </summary>
        public async void ValidateRequestBuilder()
        {
            Dictionary<string, object> param = new Dictionary<string, object>
             {
                {"a", "A"},
                {"b", "B"},
                {"1", 1},
                {"2", true},
            };


            RequestHelper requestHelper = new RequestHelper(testRequestHelperInterface);

            string request = await requestHelper.BuildRequest(param);

            NameValueCollection collection = HttpUtility.ParseQueryString(request.Substring(2));

            Assert.Equal("A", collection.Get("a"));
            Assert.Equal("B", collection.Get("b"));
            Assert.Equal("1", collection.Get("1"));
            Assert.Equal("True", collection.Get("2"));

            Assert.Equal("app-key", collection.Get("app_key"));
            Assert.Equal("0", collection.Get("t"));
            Assert.Equal("sdk-name", collection.Get("sdk_name"));
            Assert.Equal("sdk-version", collection.Get("sdk_version"));
            Assert.Equal("device-id", collection.Get("device_id"));
            Assert.Equal("1.0", collection.Get("av"));

            Assert.Equal(timeInstantForRequest.Timezone, collection.Get("tz"));
            Assert.Equal("2", collection.Get("dow"));
            Assert.Equal("7", collection.Get("hour"));
            Assert.Equal("1666683640551", collection.Get("timestamp"));

        }

        private class TestRequestHelperInterface : IRequestHelper
        {
            TimeInstant usedInstance;

            public TestRequestHelperInterface(TimeInstant usedInstance)
            {
                this.usedInstance = usedInstance;
            }

            public string GetAppKey()
            {
                return "app-key";
            }

            public async Task<DeviceId> GetDeviceId()
            {
                DeviceId deviceId = new DeviceId("device-id", DeviceBase.DeviceIdMethodInternal.developerSupplied);
                return deviceId;
            }

            public string GetSDKName()
            {
                return "sdk-name";
            }

            public string GetSDKVersion()
            {
                return "sdk-version";
            }

            public TimeInstant GetTimeInstant()
            {
                return usedInstance;
            }

            public string GetAppVersion()
            {
                return "1.0";
            }
        }
    }
}
