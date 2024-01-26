using CountlySDK;
using CountlySDK.Entities;
using System.Diagnostics;

namespace MauiSampleApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            return MauiApp.CreateBuilder()
                .UseMauiApp<SampleApp>()
                .ConfigureFonts(fonts => {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureServices()
                .Build();
        }

        private static MauiAppBuilder ConfigureServices(this MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<ICrashTester, CrashTester>();
            return builder;
        }
    }

    public class SampleApp : Application
    {

        private const string serverURL = "https://your.server.ly";
        private const string appKey = "YOUR_APP_KEY";

        public SampleApp(ICrashTester crashTester)
        {
            InitCountlySDK();
            MainPage = new MainPage(crashTester);
        }

        private async void InitCountlySDK()
        {
            if (serverURL.Equals("https://your.server.ly") || appKey.Equals("YOUR_APP_KEY")) {
                Debug.WriteLine("Please do not use default set of app key and server url");
            }

            Countly.IsLoggingEnabled = true;

            CountlyConfig countlyConfig = new CountlyConfig();
            countlyConfig.serverUrl = serverURL;
            countlyConfig.appKey = appKey;
            countlyConfig.appVersion = "123";

            await Countly.Instance.Init(countlyConfig);
            await Countly.Instance.SessionBegin();

            await Countly.RecordEvent("App started");

            // report unhandled crash
            MauiExceptions.UnhandledException += (sender, args) => {
                Countly.RecordException(args.ExceptionObject.ToString(), null, null, true).Wait();
            };

        }
    }

    public interface ICrashTester
    {
        void Test();
    }

}
