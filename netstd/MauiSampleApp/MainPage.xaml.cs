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

        private async void OnHandledCrash(object sender, EventArgs e){
            Dictionary<string, string> customInfo = new Dictionary<string, string>{
                { "customData", "importantStuff" }
            };

            try {
                throw new Exception("It is an exception");
            } catch (Exception ex) {
                await Countly.RecordException(ex.Message, ex.StackTrace, customInfo, false);
            }
        }

        private async void OnRecordView(object sender, EventArgs e)
        {
            await Countly.Instance.RecordView("TestView");
        }

        private async void OnBeginSession(object sender, EventArgs e)
        {
            await Countly.Instance.SessionBegin();
        }

        private async void OnUpdateSession(object sender, EventArgs e)
        {
            await Countly.Instance.SessionUpdate(45);
        }

        private async void OnEndSession(object sender, EventArgs e)
        {
            await Countly.Instance.SessionEnd();
        }

        private void OnAddBreadCrumb(object sender, EventArgs e)
        {
            Countly.Instance.AddCrashBreadCrumb("breadcrumb" + new Random().Next(1,13));
        }

        private async void OnChangeDeviceIDWithMerge(object sender, EventArgs e)
        {
            await Countly.Instance.ChangeDeviceId("new-device-id" + new Random().Next(1,13), true);
        }

        private async void OnChangeDeviceIDWithoutMerge(object sender, EventArgs e)
        {
            await Countly.Instance.ChangeDeviceId("new-device-id" + new Random().Next(1,13), false);
        }

        private void OnUserProfile(object sender, EventArgs e)
        {
            Countly.UserDetails.Name = "full name";
            Countly.UserDetails.Username = "username1";
            Countly.UserDetails.Email = "test@count.ly";
            Countly.UserDetails.Organization = "organization";
            Countly.UserDetails.Phone = "000-111-000 ";
            Countly.UserDetails.Gender = "Male";
            Countly.UserDetails.Custom.Add("test","test");
        }

        private async void OnLocation(object sender, EventArgs e)
        {
            await Countly.Instance.SetLocation("31.5204, 74.3587", "192.0.0.1", "PK", "Lahore");
        }
    }
}
