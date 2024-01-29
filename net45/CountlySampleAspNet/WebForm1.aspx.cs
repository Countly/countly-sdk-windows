using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CountlySDK;
using CountlySDK.Entities;

namespace CountlySampleAspNet
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        private const string serverURL = "https://your.server.ly";
        private const string appKey = "YOUR_APP_KEY";

        protected void Page_Load(object sender, EventArgs e)
        {
            //https://stackoverflow.com/questions/2859790/the-request-was-aborted-could-not-create-ssl-tls-secure-channel
            //to use TLS 1.2
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            (StartCountly()).GetAwaiter().GetResult();
        }

        public async Task StartCountly()
        {
            if (serverURL.Equals("https://your.server.ly") || appKey.Equals("YOUR_APP_KEY")) {
                Debug.WriteLine("Please do not use default set of app key and server url");
            }
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
