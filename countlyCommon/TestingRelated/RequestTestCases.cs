using CountlySDK.CountlyCommon.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace TestProject_common
{
    public class RequestTestCases : IDisposable
    {
        ITestOutputHelper output;

        /// <summary>
        /// Test setup
        /// </summary>
        public RequestTestCases(ITestOutputHelper output)
        {
            this.output = output;
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
            String req = RequestHelper.CreateBaseRequest("a", "b", 123);
            Assert.Equal("/i?app_key=a&device_id=b&timestamp=123", req);

            String req2 = RequestHelper.CreateBaseRequest("a", "b");
            Assert.Contains("/i?app_key=a&device_id=b&timestamp=", req2);
        }

        [Fact]
        public void LocationRequestBasic()
        {
            String res = RequestHelper.CreateLocationRequest(null);
            Assert.Null(res);

            String res2 = RequestHelper.CreateLocationRequest("asd");
            Assert.Null(res2);

            String res3 = RequestHelper.CreateLocationRequest("asd", null, null, null, null);
            Assert.Null(res3);
        }

        [Fact]
        public void LocationRequestSimple()
        {
            String br = "asd";
            String res;
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
