using CountlySDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace CountlySampleAspNet
{
    public partial class WebForm1 : System.Web.UI.Page
    {        
        const String serverURL = "http://try.count.ly";//put your server URL here
        const String appKey = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";//put your server APP key here   

        protected void Page_Load(object sender, EventArgs e)
        {
            (StartCountly()).GetAwaiter().GetResult();
        }

        public async Task StartCountly()
        {
            Debug.WriteLine("Initializing Countly");
            Countly.IsLoggingEnabled = true;
            await Countly.StartSession(serverURL, appKey, "1.234", Countly.DeviceIdMethod.multipleFields);       
            Countly.RecordEvent("SomeKey");
            Thread.Sleep(2000);
        }

    }
}