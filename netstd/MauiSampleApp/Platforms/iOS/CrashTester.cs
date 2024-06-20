namespace MauiSampleApp;

public class CrashTester : ICrashTester
{
    public void Test()
    {
        throw new InvalidOperationException("This is a test unhandled exception.");
    }
}
