using System;
using System.Diagnostics;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using CountlySDK;
using CountlySDK.Entities;

namespace CountlySampleAspNetMVC
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public const string serverURL = "https://your.server.ly";
        public const string appKey = "YOUR_APP_KEY";
        protected async void Application_Start()
        {
            if (serverURL.Equals("https://your.server.ly") || appKey.Equals("YOUR_APP_KEY")) {
                throw new Exception("Please do not use default set of app key and server url");
            }
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            Debug.WriteLine("Before init");

            Countly.IsLoggingEnabled = true;

            CountlyConfig countlyConfig = new CountlyConfig();
            countlyConfig.serverUrl = serverURL;
            countlyConfig.appKey = appKey;
            countlyConfig.appVersion = "123";

            await Countly.Instance.Init(countlyConfig);

            await Countly.Instance.SessionBegin();

            Debug.WriteLine("After init");
        }
    }
}
