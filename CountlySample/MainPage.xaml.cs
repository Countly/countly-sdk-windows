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

namespace CountlySample
{
    public partial class MainPage : PhoneApplicationPage
    {
        bool initialized = false;

        public MainPage()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                initialized = true;
            };
        }

        private void RecordBasicEvent_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Countly.RecordEvent("seconds", DateTime.Now.Second);
        }

        private void RecordEventSum_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Countly.RecordEvent("seconds", DateTime.Now.Second, 0.99);
        }

        private void RecordEventSegmentation_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Segmentation segmentation = new Segmentation();
            segmentation.Add("country", "Ukraine");
            segmentation.Add("app_version", "1.2");

            Countly.RecordEvent("seconds", DateTime.Now.Second, segmentation);
        }

        private void TrackingCheck_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (initialized)
            {
                Countly.StartSession("http://162.243.236.88", "14d9cc3faa4ce2a96672845b0281214b3dc9ee92");

                RecordBasicEvent.IsEnabled = true;
                RecordEventSum.IsEnabled = true;
                RecordEventSegmentation.IsEnabled = true;
            }
        }

        private void TrackingCheck_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            Countly.Halt();

            RecordBasicEvent.IsEnabled = false;
            RecordEventSum.IsEnabled = false;
            RecordEventSegmentation.IsEnabled = false;
        }
    }
}