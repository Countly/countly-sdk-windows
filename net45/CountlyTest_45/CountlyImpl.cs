﻿using System.Threading.Tasks;
using CountlySDK;
using CountlySDK.Entities;

namespace TestProject_common
{
    class CountlyImpl
    {
        public static void SetPCLStorageIfNeeded()
        {

        }

        public static async Task StartLegacyCountlySession(string serverUrl, string appKey, string appVersion)
        {
            //reworked after removal of the deprecated function
            CountlyConfig cc = new CountlyConfig() { serverUrl = serverUrl, appKey = appKey, appVersion = appVersion };
            await Countly.Instance.Init(cc);
            await Countly.Instance.SessionBegin();
        }

        public static CountlyConfig CreateCountlyConfig()
        {
            return new CountlyConfig() { serverUrl = ServerInfo.serverURL, appKey = ServerInfo.appKey, appVersion = ServerInfo.appVersion };
        }
    }
}
