using CountlySDK;
using CountlySDK.Entities;
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace MauiSampleApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnCounterClicked(object sender, EventArgs e)
        {
            await Countly.RecordEvent("Basic event");
        }
    }
}
