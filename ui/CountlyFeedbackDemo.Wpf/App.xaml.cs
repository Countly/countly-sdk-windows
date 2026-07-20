using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CountlyFeedbackDemo.Wpf
{
    public partial class App : System.Windows.Application
    {
    }

    internal static class DpiBootstrap
    {
        // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 (-4). Set at module load — before WPF
        // initializes and locks the process DPI awareness — so WebView2 content renders 1:1
        // with the window instead of being mis-scaled/clipped. Windows 10 1703+.
        private static readonly IntPtr PerMonitorAwareV2 = new IntPtr(-4);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        [ModuleInitializer]
        internal static void Init()
        {
            try { SetProcessDpiAwarenessContext(PerMonitorAwareV2); }
            catch { /* pre-1703 Windows: fall back to the embedded manifest */ }
        }
    }
}
