using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using CountlySDK;
using CountlySDK.Entities;

namespace CountlySampleWindowsForm
{
    public partial class Form1 : Form
    {

        public const string serverURL = "https://your.server.ly";
        public const string appKey = "YOUR_APP_KEY";

        public Form1()
        {
            if (serverURL.Equals("https://your.server.ly") || appKey.Equals("YOUR_APP_KEY")) {
                throw new Exception("Please do not use default set of app key and server url");
            }

            InitializeComponent();
            Countly.IsLoggingEnabled = true;

            CountlyConfig countlyConfig = new CountlyConfig();
            countlyConfig.serverUrl = serverURL;
            countlyConfig.appKey = appKey;
            countlyConfig.appVersion = "123";
            countlyConfig.EnableBackendMode();

            Countly.Instance.Init(countlyConfig);
        }

        private void btnSend_Click(object sender, EventArgs e)
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

        private void GenerateAndRecordEvent(int appCount, int eventCount, int deviceCount)
        {

            List<StringPair> devices = new List<StringPair>();

            Random random = new Random();

            for (int i = 0; i < deviceCount; i++) {
                devices.Add(new StringPair($"App_{random.Next(appCount)}", $"Device_{random.Next(deviceCount)}"));
            }

            for (int i = 0; i < eventCount; i++) {
                StringPair pair = devices[Math.Abs(random.Next(deviceCount) - 1)];
                Countly.Instance.BackendMode().RecordEvent(pair.two, pair.one, $"Event_{i}", 0, 1, 0, null, 1);

            }
        }
    }
}
