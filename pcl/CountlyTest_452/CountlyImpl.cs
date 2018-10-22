using CountlySDK;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using PCLStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestProject_common;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace TestProject_common
{
    class CountlyImpl
    {
        public static void SetPCLStorageIfNeeded()
        {
            Storage.Instance.fileSystem = FileSystem.Current;
        }

        public static async Task StartLegacyCountlySession(string serverUrl, string appKey, string appVersion)
        {
            await Countly.StartSession(serverUrl, appKey, appVersion, FileSystem.Current);
        }

        public static CountlyConfig CreateCountlyConfig()
        {
            return new CountlyConfig() { serverUrl = ServerInfo.serverURL, appKey = ServerInfo.appKey, appVersion = ServerInfo.appVersion, fileSystem = FileSystem.Current };
        }
    }
}
