using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using CountlySDK.Entities;

namespace CountlyTestBackendMode
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public const string serverURL = "http://127.0.0.1:5000";
        public const string appKey = "YOUR_APP_KEY";

        public MainWindow()
        {
            InitializeComponent();
            Debug.WriteLine("Before init");

            Countly.IsLoggingEnabled = true;

            CountlyConfig countlyConfig = new CountlyConfig();
            countlyConfig.serverUrl = serverURL;
            countlyConfig.appKey = appKey;
            countlyConfig.appVersion = "123";
            countlyConfig.backendMode = true;

            Countly.Instance.Init(countlyConfig);
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            int appCountValue = int.Parse(appCount.Text);
            int eventCountValue = int.Parse(eventCount.Text);
            int deviceCountValue = int.Parse(deviceCount.Text);

            GenerateAndRecordEvent(appCountValue, eventCountValue, deviceCountValue);
        }

        internal class StringPair
        {
            internal string one;
            internal string two;

            internal StringPair(string one, string two)
            {
                this.one = one;
                this.two = two;
            }
        }

        private void GenerateAndRecordEvent(int appCount, int eventCount, int deviceCount) {

            List<StringPair> devices = new List<StringPair>();

            Random random = new Random();

            for (int i = 0; i < deviceCount; i++)
            {
                devices.Add(new($"App_{random.Next(appCount)}", $"Device_{random.Next(deviceCount)}"));
            }

            DateTime startTime = DateTime.Now;
            for (int i = 0; i < eventCount; i++)
            {
                StringPair pair = devices[Math.Abs(random.Next(deviceCount) - 1)];
                Countly.Instance.BackendMode().RecordEvent(pair.two,pair.one,$"Event_{i}",0,1,0,null,1);

            }

            DateTime endTime = DateTime.Now;

            Debug.WriteLine(endTime - startTime);

        }


    }
}
