using System;
using System.Runtime.InteropServices;

namespace CountlySDK.UI
{
    public static partial class CountlyWebView
    {
        /// <summary>
        /// When true, feedback widgets anchor within the host app window; otherwise (default) the
        /// whole screen. The host app must be per-monitor-DPI-aware for correct sizing/placement.
        /// </summary>
        public static bool ShowWidgetsWithinApp { get; set; }

        private static readonly IntPtr PerMonitorAwareV2 = new IntPtr(-4);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        // Best-effort; only takes effect if no window exists yet. Hosts should also declare
        // per-monitor DPI awareness (manifest or a module initializer) — see docs.
        internal static void EnsurePerMonitorDpiAware()
        {
            try { SetProcessDpiAwarenessContext(PerMonitorAwareV2); } catch { }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetThreadDpiAwarenessContext();

        [DllImport("user32.dll")]
        private static extern int GetAwarenessFromDpiAwarenessContext(IntPtr context);

        [DllImport("shcore.dll")]
        private static extern int GetScaleFactorForMonitor(IntPtr hMon, out int scale);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint flags);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        private const uint MonitorDefaultToPrimary = 1;

        /// <summary>
        /// Synchronous best-effort estimate of window-units-per-CSS-px for the primary monitor,
        /// computed WITHOUT a live webview so the first content fetch (which can precede the async
        /// probe) already sizes correctly. If the process is DPI-aware, WPF already scales window
        /// units to the monitor, so the factor is 1.0; if DPI-unaware, WPF uses 96-DPI units and we
        /// multiply by the physical monitor scale. Returns 1.0 when the APIs are unavailable; the
        /// content display then refines this from the page's devicePixelRatio.
        /// </summary>
        internal static double EstimateMonitorScale()
        {
            try {
                int awareness = GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext());
                if (awareness > 0) { return 1.0; }   // 1 = system-aware, 2 = per-monitor-aware
                IntPtr mon = MonitorFromPoint(new POINT { X = 0, Y = 0 }, MonitorDefaultToPrimary);
                if (GetScaleFactorForMonitor(mon, out int pct) == 0 && pct > 0) { return pct / 100.0; }
            } catch { /* fall through to 1.0 */ }
            return 1.0;
        }
    }
}
