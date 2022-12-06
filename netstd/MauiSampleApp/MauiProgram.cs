using CountlySDK.Entities;
using CountlySDK;
using Microsoft.Extensions.Logging;

namespace MauiSampleApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            return MauiApp.CreateBuilder()
                .UseMauiApp<SampleApp>()
                .ConfigureFonts(fonts =>
                                {
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

        public const string serverURL = "https://try.count.ly";
        public const string appKey = "YOUR_APP_KEY";
        public SampleApp(ICrashTester crashTester)
        {
            InitCountlySDK();
            MainPage = new AppShell();
            
            crashTester.Test();
        }

        private async void InitCountlySDK()
        {
            Countly.IsLoggingEnabled = true;

            CountlyConfig countlyConfig = new CountlyConfig();
            countlyConfig.serverUrl = serverURL;
            countlyConfig.appKey = appKey;
            countlyConfig.appVersion = "123";

            await Countly.Instance.Init(countlyConfig);
            await Countly.Instance.SessionBegin();

            await Countly.RecordEvent("App started");

            // report unhandled crash
            MauiExceptions.UnhandledException += (sender, args) =>
            {
                Countly.RecordException(args.ExceptionObject.ToString(), null, null, true).Wait();
            };

        }
    }

    public interface ICrashTester
    {
        void Test();
    }

}
