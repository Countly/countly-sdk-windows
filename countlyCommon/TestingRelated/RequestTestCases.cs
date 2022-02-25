using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            string req = RequestHelper.CreateBaseRequest("a", "b", "c", "d", instant);
            string expected = string.Format("/i?app_key={0}&device_id={1}&timestamp={2}&sdk_version={3}&sdk_name={4}&hour={5}&dow={6}&tz={7}", "a", "b", instant.Timestamp, "c", "d", instant.Hour, instant.Dow, instant.Timezone);

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

    }
}
