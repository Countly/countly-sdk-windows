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
        private const string serverURL = "https://your.server.ly";
        private const string appKey = "YOUR_APP_KEY";

        public Form1()
        {
            if (serverURL.Equals("https://your.server.ly") || appKey.Equals("YOUR_APP_KEY")) {
                Debug.WriteLine("Please do not use default set of app key and server url");
            }
            InitializeComponent();
            Countly.IsLoggingEnabled = true;
        }

        private async void btnBeginSession_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Before init");

            Countly.IsLoggingEnabled = true;

            CountlyConfig countlyConfig = new CountlyConfig {
                serverUrl = serverURL,
                appKey = appKey,
                appVersion = "123"
            };

            await Countly.Instance.Init(countlyConfig);

            Countly.UserDetails.Custom.Add("aaa", "666");

            await Countly.Instance.SessionBegin();

            Debug.WriteLine("After init");
        }

        private async void btnEndSession_Click(object sender, EventArgs e)
        {
            await Countly.Instance.SessionEnd();
        }

        private void btnEventSimple_Click(object sender, EventArgs e)
        {
            Countly.RecordEvent("Some event");
        }

        private async void btnCrash_Click(object sender, EventArgs e)
        {
            try {
                throw new Exception("This is some bad exception 3");
            } catch (Exception ex) {
                Dictionary<string, string> customInfo = new Dictionary<string, string>
                {
                    { "customData", "importantStuff" }
                };
                await Countly.RecordException(ex.Message, ex.StackTrace, customInfo);
            }
        }
    }
}
