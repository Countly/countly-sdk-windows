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
using static CountlySDK.CountlyCommon.CountlyBase;

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

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //Record Handled Crash
            try
            {
                throw new DivideByZeroException();
            }
            catch (Exception ex)
            {
                await Countly.RecordException(ex.Message, ex.StackTrace);
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //Record View
            await Countly.Instance.RecordView("Some View Name");
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            //throwing unhandled exception
            throw new IndexOutOfRangeException();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            bool consent = true;

            Countly.Instance.SetConsent(new Dictionary<ConsentFeatures, bool>
            {
                { ConsentFeatures.Crashes, consent },
                { ConsentFeatures.Events, consent },
                { ConsentFeatures.Location, false },
                { ConsentFeatures.Sessions, consent },
                { ConsentFeatures.Users, false },
                { ConsentFeatures.Views, consent }
            });
        }
    }
}
