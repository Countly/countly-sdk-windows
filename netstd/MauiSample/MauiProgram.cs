using Microsoft.Extensions.Logging;

namespace MauiSample
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<TestApp>()
                .ConfigureCrash()
                .ConfigureFonts(fonts => {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static MauiAppBuilder ConfigureCrash(this MauiAppBuilder builder)
        {
            builder.Services.AddSingleton<INativeCrash, MauiSampleApp.CrashNative>();
            return builder;
        }
    }
}

public class TestApp : Application
{
    public TestApp(INativeCrash crashTester)
    {
        MainPage = new ContentPage
        {
            Content = new Label
            {
                Text = "Test App",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            },
        };

        crashTester.Test();
    }
}

public interface INativeCrash
{
    void Test();
}
