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
        public const string serverURL = "https://try.count.ly";
        public const string appKey = "YOUR_APP_KEY";

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
        }

        protected async override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            await Countly.Instance.SessionEnd();
        }
    }

}
