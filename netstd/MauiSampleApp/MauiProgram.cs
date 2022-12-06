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
        public SampleApp(ICrashTester crashTester)
        {
            MainPage = new AppShell();

            crashTester.Test();
        }
    }

    public interface ICrashTester
    {
        void Test();
    }

}
