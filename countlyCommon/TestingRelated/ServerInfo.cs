using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject_common
{
    // CI copies this file to ../countly-sdk-windows-server-info, which the test projects compile, and answers this URL from
    // .github/scripts/countly_stand_in_server.py, since some tests wait until their requests are delivered.
    public static class ServerInfo
    {
        public const String serverURL = "http://127.0.0.1:18080";
        public const String appKey = "APP_KEY";
        public const String appVersion = "1.0";
    }
}
