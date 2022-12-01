using CountlySDK;
using CountlySDK.Entities;
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace MauiSampleApp
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public const string serverURL = "https://master.count.ly";
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

            MauiExceptions.UnhandledException += (sender, args) =>
            {
                Countly.RecordException(args.ExceptionObject.ToString(), null, null, true).Wait();
            };

        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
            {
                CounterBtn.Text = $"Clicked {count} time";
            }
            else
            {
                CounterBtn.Text = $"Clicked {count} times";
            }

            SemanticScreenReader.Announce(CounterBtn.Text);

            await Countly.RecordEvent($"Event {count}");
        }
    }
}
