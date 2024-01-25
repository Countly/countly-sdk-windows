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

        protected async override void OnStartup(StartupEventArgs e)
        {
            if (serverURL.Equals("https://your.server.ly") || appKey.Equals("YOUR_APP_KEY")) {
                throw new Exception("Please do not use default set of app key and server url");
            }
            base.OnStartup(e);

            Debug.WriteLine("Before init");

            Countly.IsLoggingEnabled = true;

            CountlyConfig countlyConfig = new CountlyConfig();
            countlyConfig.serverUrl = serverURL;
            countlyConfig.appKey = appKey;
            countlyConfig.appVersion = "123";

            await Countly.Instance.Init(countlyConfig);

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
