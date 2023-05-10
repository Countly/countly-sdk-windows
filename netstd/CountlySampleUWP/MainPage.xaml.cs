using System;
using System.Collections.Generic;
using CountlySDK;
using CountlySDK.Entities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CountlySampleUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Record_Event(object sender, RoutedEventArgs e)
        {

            await Countly.RecordEvent("Basic Event");
        }

        private async void Record_Event_1(object sender, RoutedEventArgs e)
        {
            Segmentation segmentation = new Segmentation();
            segmentation.Add("retry", "3");
            segmentation.Add("time_left", "60");

            await Countly.RecordEvent("Basic Event", 1, 10, 10.5, segmentation);
        }

        private async void Record_View(object sender, RoutedEventArgs e)
        {
            await Countly.Instance.RecordView("Start View");
        }

        private async void Record_Exception(object sender, RoutedEventArgs e)
        {
            try {
                throw new Exception("Exception with segmentation");
            } catch (Exception ex) {
                Dictionary<string, string> customInfo = new Dictionary<string, string>();
                customInfo.Add("customData", "importantStuff");

                await Countly.RecordException(ex.Message, ex.StackTrace, customInfo);
            }
        }

        private async void Change_DeviceID_With_Merge(object sender, RoutedEventArgs e)
        {
            await Countly.Instance.ChangeDeviceId("new-device-id-1", true);
        }

        private async void Change_DeviceID_Without_Merge(object sender, RoutedEventArgs e)
        {
            await Countly.Instance.ChangeDeviceId("new-device-id-1");
        }

        private void Set_User_Profile(object sender, RoutedEventArgs e)
        {
            Countly.UserDetails.Name = "full name";
            Countly.UserDetails.Username = "username1";
            Countly.UserDetails.Email = "test@count.ly";
            Countly.UserDetails.Organization = "organization";
            Countly.UserDetails.Phone = "000-111-000 ";
            Countly.UserDetails.Gender = "Male";
        }

        private void Set_User_Custom_Profile(object sender, RoutedEventArgs e)
        {
            CustomInfo customInfo = new CustomInfo();
            customInfo.Add("Height", "5'10");
            customInfo.Add("Weight", "79 kg");
            customInfo.Add("Black", "Hair Color");

            Countly.UserDetails.Custom = customInfo;
        }

        private async void Set_User_Location(object sender, RoutedEventArgs e)
        {
            await Countly.Instance.SetLocation("31.5204, 74.3587", "192.0.0.1", "PK", "Lahore");

        }

        private async void Disable_Location(object sender, RoutedEventArgs e)
        {
            await Countly.Instance.DisableLocation();
        }

        private void Start_Timed_Event(object sender, RoutedEventArgs e)
        {
            Countly.Instance.StartEvent("timed event");
        }

        private async void End_Timed_Event(object sender, RoutedEventArgs e)
        {
            Segmentation segment = new Segmentation();
            segment.Add("Time Spent", "60");
            segment.Add("Retry Attempts", "5");

            await Countly.Instance.EndEvent("timed event", segment, 2, 10.0);
        }
    }
}
