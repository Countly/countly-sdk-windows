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

using CountlySDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
namespace CountlySample
{
    public sealed partial class MainPage : Page
    {
        bool initialized = false;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            Loaded += (s, e) =>
            {
                initialized = true;
            };
        }

        private void RecordBasicEvent_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Countly.RecordEvent("seconds", DateTime.Now.Second);
        }

        private void RecordEventSum_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Countly.RecordEvent("seconds", DateTime.Now.Second, 0.99);
        }

        private void RecordEventSegmentation_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Segmentation segmentation = new Segmentation();
            segmentation.Add("country", "Ukraine");
            segmentation.Add("app_version", "1.2");

            Countly.RecordEvent("seconds", DateTime.Now.Second + 1, segmentation);
        }

        private void TrackingCheck_Checked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
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
            }
        }

        private void TrackingCheck_Unchecked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Countly.Halt();

            RecordBasicEvent.IsEnabled = false;
            RecordEventSum.IsEnabled = false;
            RecordEventSegmentation.IsEnabled = false;
        }
    }
}
