

using Com.Countly.Nativecrashlib;

namespace MauiSampleApp;

public class CrashTester : ICrashTester
{
    public void Test()
    {
        NativeCrashHandler handler = new NativeCrashHandler();
        handler.ThrowException();
    }
}
