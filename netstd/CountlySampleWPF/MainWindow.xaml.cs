using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CountlySDK;
using CountlySDK.CountlyCommon;

namespace CountlySampleWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click_0(object sender, RoutedEventArgs e)
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

            Countly.Instance.SetConsent(new Dictionary<CountlyBase.ConsentFeatures, bool>
            {
                { CountlyBase.ConsentFeatures.Crashes, consent },
                { CountlyBase.ConsentFeatures.Events, consent },
                { CountlyBase.ConsentFeatures.Location, false },
                { CountlyBase.ConsentFeatures.Sessions, consent },
                { CountlyBase.ConsentFeatures.Users, false },
                { CountlyBase.ConsentFeatures.Views, consent }
            });
        }
    }
}
