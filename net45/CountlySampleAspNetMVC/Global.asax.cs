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
        const String serverURL = "https://master.count.ly/";//put your server URL here
        const String appKey = "5e20d03806255d314eb6679b26fda6e580b3d899";//put your server APP key here   
        protected async void Application_Start()
        {
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
