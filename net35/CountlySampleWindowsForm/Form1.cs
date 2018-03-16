using CountlySDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace CountlySampleWindowsForm
{
    public partial class Form1 : Form
    {        
        //const String serverURL = "http://try.count.ly";//put your server URL here
        //const String appKey = "APP_key";//put your server APP key here       

        public Form1()
        {
            InitializeComponent();
            Countly.IsLoggingEnabled = true;
        }

        private void btnBeginSession_Click(object sender, EventArgs e)
        {
            Countly.StartSession(serverURL, appKey, "1.234");
        }

        private void btnEndSession_Click(object sender, EventArgs e)
        {
            Countly.EndSession();
        }

        private void btnEventSimple_Click(object sender, EventArgs e)
        {
            Countly.RecordEvent("Some event");
        }

        private void btnCrash_Click(object sender, EventArgs e)
        {
            try
            {
                throw new Exception("This is some bad exception 3");
            }
            catch (Exception ex)
            {
                Dictionary<string, string> customInfo = new Dictionary<string, string>();
                customInfo.Add("customData", "importantStuff");
                Countly.RecordException(ex.Message, ex.StackTrace, customInfo);
            }
        }
    }
}
