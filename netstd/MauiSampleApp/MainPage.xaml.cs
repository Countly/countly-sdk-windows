using CountlySDK;

namespace MauiSampleApp
{
    public partial class MainPage : ContentPage
    {
        ICrashTester crashTester;
        public MainPage(ICrashTester crashTester)
        {
            InitializeComponent();
            this.crashTester = crashTester;
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            await Countly.RecordEvent("Basic event");
        }

        private void OnCrashAppClicked(object sender, EventArgs e)
        {
            crashTester.Test();
        }
    }
}
