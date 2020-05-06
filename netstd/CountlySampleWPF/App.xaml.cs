using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CountlySDK;
using CountlySDK.CountlyCommon;
using CountlySDK.Entities;

namespace CountlySampleWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public const String serverURL = "";
        public const String appKey = "";

        private async void App_OnStartup(object sender, StartupEventArgs e)
        {
            Debug.WriteLine("Before init");

            Countly.IsLoggingEnabled = true;
            
            CountlyConfig countlyConfig = new CountlyConfig();
            countlyConfig.serverUrl = serverURL;
            countlyConfig.appKey = appKey;
            countlyConfig.appVersion = "123";

            await Countly.Instance.Init(countlyConfig);
            await Countly.Instance.SessionBegin();

            Debug.WriteLine("After init");

            Countly.UserDetails.Name = "fdf";
        }

        protected async override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            await Countly.Instance.SessionEnd();
        }
    }
    
}
