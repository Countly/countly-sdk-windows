/*
Copyright (c) 2012, 2013, 2014 Countly

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using CountlySample.Resources;
using CountlySDK;
using Microsoft.Phone.Tasks;

namespace CountlySample
{
    public partial class MainPage : PhoneApplicationPage
    {
        bool initialized = false;

        PhotoChooserTask photoChooserTask;

        public MainPage()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                initialized = true;

                UserNameText.Text = Countly.UserDetails.Name ?? String.Empty;
            };
        }

        private void RecordBasicEvent_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Countly.RecordEvent("seconds", DateTime.Now.Second);

            Countly.AddBreadCrumb("basic event");
        }

        private void RecordEventSum_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Countly.RecordEvent("seconds", DateTime.Now.Second, 0.99);

            Countly.AddBreadCrumb("sum event");
        }

        private void RecordEventSegmentation_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Segmentation segmentation = new Segmentation();
            segmentation.Add("country", "Ukraine");
            segmentation.Add("app_version", "1.2");

            Countly.RecordEvent("seconds", DateTime.Now.Second, segmentation);

            Countly.AddBreadCrumb("segmentation event");
        }

        private void TrackingCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (initialized)
            {
                string ServerUrl = "https://cloud.count.ly";
                string AppKey = null;

                if (ServerUrl == null)
                    throw new ArgumentNullException("Type your ServerUrl");
                if (AppKey == null)
                    throw new ArgumentNullException("Type your AppKey");

                Countly.StartSession(ServerUrl, AppKey);

                RecordBasicEvent.IsEnabled = true;
                RecordEventSum.IsEnabled = true;
                RecordEventSegmentation.IsEnabled = true;
                UserNameText.IsEnabled = true;
                UploadUserPictureButton.IsEnabled = true;
                CrashButton.IsEnabled = true;
            }
        }

        private void TrackingCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            Countly.Halt();

            RecordBasicEvent.IsEnabled = false;
            RecordEventSum.IsEnabled = false;
            RecordEventSegmentation.IsEnabled = false;
            UserNameText.IsEnabled = false;
            UserNameText.Text = String.Empty;
            UploadUserPictureButton.IsEnabled = false;
            CrashButton.IsEnabled = false;
        }

        private void UserNameText_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (Countly.UserDetails.Name != UserNameText.Text)
            {
                Countly.UserDetails.Name = UserNameText.Text;

                Countly.AddBreadCrumb("username updated: " + UserNameText.Text);
            }
        }

        private void UploadUserPictureButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            photoChooserTask.Completed += OnPhotoChooserTask_Completed;
            photoChooserTask.Show();
        }

        private async void OnPhotoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK)
            {
                if (await Countly.UserDetails.UploadUserPicture(e.ChosenPhoto))
                {
                    Countly.AddBreadCrumb("user picture updated");

                    MessageBox.Show("Picture uploaded successfully");
                }
            }
        }

        private void CrashButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            throw new Exception("Unhandled Exception");
        }
    }
}