using System;
using System.Collections.Generic;
using CountlySDK;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.Entities;
using CountlySDK.Entities.EntityBase;
using CountlySDK.Helpers;
using Xunit;
using static CountlySDK.Helpers.TimeHelper;

namespace TestProject_common
{
    public class RequestTestCases : IDisposable
    {
        /// <summary>
        /// Test setup
        /// </summary>
        public RequestTestCases()
        {
            TestHelper.CleanDataFiles();
            Countly.Halt();
            TestHelper.CleanDataFiles();
            Countly.Instance.deferUpload = true;
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {
        }

        [Fact]
        public void BaseRequestBasic()
        {
            TimeHelper timeHelper = new TimeHelper();
            TimeInstant instant = timeHelper.GetUniqueInstant();
            DeviceId dId = new DeviceId("b", DeviceBase.DeviceIdMethodInternal.developerSupplied);
            string req = RequestHelper.CreateBaseRequest("a", dId, "c", "d", instant);
            string expected = string.Format("/i?app_key={0}&device_id={1}&timestamp={2}&sdk_version={3}&sdk_name={4}&hour={5}&dow={6}&tz={7}&t={8}", "a", "b", instant.Timestamp, "c", "d", instant.Hour, instant.Dow, instant.Timezone, dId.Type());

            Assert.Contains(expected, req);
        }

        [Fact]
        public void LocationRequestBasic()
        {
            string res2 = RequestHelper.CreateLocationRequest("asd");
            Assert.Null(res2);

            string res3 = RequestHelper.CreateLocationRequest("asd", null, null, null, null);
            Assert.Null(res3);
        }

        [Fact]
        public void LocationRequestSimple()
        {
            string br = "asd";
            string res;
            res = RequestHelper.CreateLocationRequest(br, null, null, null, null);
            Assert.Null(res);

            res = RequestHelper.CreateLocationRequest(br, "", "", "", "");
            Assert.Equal("asd&location=&ip=&country_code=&city=", res);

            res = RequestHelper.CreateLocationRequest(br, null, "a", "b", "c");
            Assert.Equal("asd&ip=a&country_code=b&city=c", res);

            res = RequestHelper.CreateLocationRequest(br, "a", null, "b", "c");
            Assert.Equal("asd&location=a&country_code=b&city=c", res);

            res = RequestHelper.CreateLocationRequest(br, "a", "b", null, "c");
            Assert.Equal("asd&location=a&ip=b&city=c", res);

            res = RequestHelper.CreateLocationRequest(br, "a", "b", "c", null);
            Assert.Equal("asd&location=a&ip=b&country_code=c", res);
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
                deviceIdMethod = Countly.DeviceIdMethod.developerSupplied
            };

            await Countly.Instance.Init(cc);
            Dictionary<string, object> baseParams = await Countly.Instance.requestHelper.GetBaseParams();

            Assert.Equal(9, baseParams.Count);

            Assert.Equal("YOUR_APP_KEY", baseParams["app_key"]);
            Assert.Equal("test device id", baseParams["device_id"]);
            Assert.Equal("22.02.1", baseParams["sdk_version"]);
            Assert.Equal(0, baseParams["t"]);

            Assert.True(baseParams.ContainsKey("sdk_name"));
            Assert.True(baseParams.ContainsKey("timestamp"));
            Assert.True(baseParams.ContainsKey("dow"));
            Assert.True(baseParams.ContainsKey("hour"));
            Assert.True(baseParams.ContainsKey("tz"));
        }

        //[Fact]
        ///// <summary>
        ///// It validates request builder.
        ///// </summary>
        //public void ValidateRequestBuilder()
        //{
        //    Dictionary<string, object> param1 = new Dictionary<string, object>
        //     {
        //        {"a", "A"},
        //        {"b", "B"},
        //        {"1", 1},
        //        {"2", true},
        //    };

        //    Dictionary<string, object> parame2 = new Dictionary<string, object>
        //    {
        //        {"c", "C"},
        //        {"d", "D"},
        //        {"1", 1},
        //        {"3", 3},
        //        {"4", false},
        //    };

        //    RequestHelper requestHelper = new RequestHelper();

        //    string request = requestHelper.BuildQueryString(param1);
        //    Assert.Equal("/i?a=A&b=B&1=1&2=True", request);

        //}
    }
}
