using System;
using System.Diagnostics;
using System.Windows;
using CountlySDK;
using CountlySDK.Entities;

namespace CountlySampleWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public const string serverURL = "https://your.server.ly";
        public const string appKey = "YOUR_APP_KEY";

        private async void App_OnStartup(object sender, StartupEventArgs e)
        {
            if (serverURL.Equals("https://your.server.ly") || appKey.Equals("YOUR_APP_KEY")) {
                throw new Exception("Please do not use default set of app key and server url");
            }
            Debug.WriteLine("Before init");

            Countly.IsLoggingEnabled = true;

            CountlyConfig countlyConfig = new CountlyConfig();
            countlyConfig.serverUrl = serverURL;
            countlyConfig.appKey = appKey;
            countlyConfig.appVersion = "123";

            await Countly.Instance.Init(countlyConfig);
            await Countly.Instance.SessionBegin();
        }

        protected async override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            await Countly.Instance.SessionEnd();
        }
    }

}
