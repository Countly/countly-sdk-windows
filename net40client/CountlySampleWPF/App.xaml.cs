using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CountlySDK;
using CountlySDK.Entities;
using CountlySDK.Helpers;

namespace CountlySampleWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public const String serverURL = "";
        public const String appKey = "";

        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Debug.WriteLine("Before init");

            Countly.IsLoggingEnabled = true;

            CountlyConfig countlyConfig = new CountlyConfig();
            countlyConfig.serverUrl = serverURL;
            countlyConfig.appKey = appKey;
            countlyConfig.appVersion = "123";

            await Countly.Instance.Init(countlyConfig);
            
            Countly.UserDetails.Custom.Add("aaa", "666");

            String cpuid = OpenUDID.value;
            String newId = Countly.Instance.GenerateDeviceIdMultipleFields();

            await Countly.Instance.SessionBegin();

            Debug.WriteLine("After init");

        }

        protected async override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            Debug.WriteLine("On Exit");

            await Countly.Instance.SessionEnd();
        }
    }
}
