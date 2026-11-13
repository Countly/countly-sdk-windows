using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject_common
{
    // CI copies this file to ../countly-sdk-windows-server-info, which the test projects compile; the tests need no real server.
    public static class ServerInfo
    {
        public const String serverURL = "https://domin.com";
        public const String appKey = "APP_KEY";
        public const String appVersion = "1.0";
    }
}
