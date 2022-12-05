using CountlySDK;
using CountlySDK.Entities;
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace MauiSampleApp
{
    public partial class MainPage : ContentPage
    {
        public const string serverURL = "https://try.count.ly";
        public const string appKey = "YOUR_APP_KEY";

        public MainPage()
        {
            InitializeComponent();
            InitCountlySDK();
        }

        private async void InitCountlySDK()
        {
            Countly.IsLoggingEnabled = true;

            CountlyConfig countlyConfig = new CountlyConfig();
            countlyConfig.serverUrl = serverURL;
            countlyConfig.appKey = appKey;
            countlyConfig.appVersion = "123";

            await Countly.Instance.Init(countlyConfig);
            await Countly.Instance.SessionBegin();

            await Countly.Instance.RecordView("MainPage view");

            // record native crash
            MauiExceptions.UnhandledException += (sender, args) =>
            {
                Countly.RecordException(args.ExceptionObject.ToString(), null, null, true).Wait();
            };

        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            await Countly.RecordEvent("Basic event");
        }
    }
}
