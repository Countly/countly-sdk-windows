using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using OpenUDIDPhone;

namespace PhoneAppTester
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            txtOldDeviceId.Text = OpenUDID.OldDeviceId;
            txtCorpUDID.Text = OpenUDID.GetCorpUDID("com.wavespread");// unique company domain
            txtOpenUDID.Text = OpenUDID.value;
        }
    }
}