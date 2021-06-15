using CountlySDK;
using CountlySDK.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
            //https://stackoverflow.com/questions/2859790/the-request-was-aborted-could-not-create-ssl-tls-secure-channel
            //to use TLS 1.2
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            (StartCountly()).GetAwaiter().GetResult();
        }

        public async Task StartCountly()
        {
            Debug.WriteLine("Before init");

            Countly.IsLoggingEnabled = true;

            CountlyConfig countlyConfig = new CountlyConfig();
            countlyConfig.serverUrl = serverURL;
            countlyConfig.appKey = appKey;
            countlyConfig.appVersion = "12.3";

            await Countly.Instance.Init(countlyConfig);
            await Countly.Instance.SessionBegin();

            Debug.WriteLine("After init");

            Countly.UserDetails.Name = "fdf";

            Thread.Sleep(2000);
        }

    }
}