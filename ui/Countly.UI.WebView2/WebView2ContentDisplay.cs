using System;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using CountlySDK.CountlyCommon;
using Microsoft.Web.WebView2.Wpf;

namespace CountlySDK.UI
{
    /// <summary>
    /// WPF implementation of the core <see cref="IContentDisplay"/> bridge: reports the primary
    /// work-area size and shows server-placed content in a borderless, top-most, non-modal WebView2
    /// window at the server-computed coordinates. Constructed on the UI thread (captures its
    /// Dispatcher).
    ///
    /// Coordinate model: the server computes <c>geo</c> in the units of the resolution we report,
    /// and the content page itself lays out in CSS px. So we report the work area in CSS px
    /// (physical ÷ monitor-scale) and place the window at <c>geo × monitor-scale</c> for BOTH
    /// position and size. On a DPI-aware host or a 100% monitor the scale is 1.0 and this is a
    /// no-op; on a DPI-unaware host at 125% it makes a fullscreen item fill exactly and a top-right
    /// item land on the screen edge (instead of scaling size only, which pushed content off-screen).
    /// </summary>
    internal sealed class WebView2ContentDisplay : IContentDisplay
    {
        private readonly Dispatcher _dispatcher;

        // window-units per CSS-px (= devicePixelRatio / host DPI scale). Static so it survives across
        // content shows: GetScreen() runs during the fetch (before any content webview exists) and
        // needs it, but it can only be *measured* from a live page. A one-time hidden-webview probe
        // (started at construction, well before the first fetch) seeds it with the same signal the
        // widget adapter uses; each content show then refines it from its own devicePixelRatio.
        // Seeded synchronously (Win32) so the first fetch is correct even if it precedes the probe.
        private static double _monitorScale = CountlyWebView.EstimateMonitorScale();
        private static bool _scaleProbeStarted;

        public WebView2ContentDisplay()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;                 // captured on the UI thread
            StartScaleProbe();
        }

        // Measure window-units-per-CSS-px once, before any content is fetched, from a throwaway 1×1
        // WebView2 on the primary work area — exactly how the widget adapter measures it (a Win32 DPI
        // estimate is unreliable because it cannot see WPF's own DPI notion). On a DPI-aware host or a
        // 100% monitor this is 1.0; on a DPI-unaware host at 125% it is 1.25.
        private void StartScaleProbe()
        {
            if (_scaleProbeStarted) { return; }
            _scaleProbeStarted = true;
            _dispatcher.BeginInvoke(new Action(async () => {
                Window probe = null;
                try {
                    WebView2 wv = new WebView2();
                    probe = new Window {
                        WindowStyle = WindowStyle.None,
                        ResizeMode = ResizeMode.NoResize,
                        ShowInTaskbar = false,
                        ShowActivated = false,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        Left = SystemParameters.WorkArea.Left, Top = SystemParameters.WorkArea.Top,
                        Width = 1, Height = 1,
                        Content = wv
                    };
                    probe.Show();   // realize the HWND on the primary monitor so devicePixelRatio is accurate
                    await wv.EnsureCoreWebView2Async(null);
                    double wpfDpi = System.Windows.Media.VisualTreeHelper.GetDpi(probe).DpiScaleX;
                    string dprJson = await wv.CoreWebView2.ExecuteScriptAsync("window.devicePixelRatio");
                    double.TryParse(dprJson, NumberStyles.Any, CultureInfo.InvariantCulture, out double dpr);
                    if (dpr > 0 && wpfDpi > 0) { _monitorScale = dpr / wpfDpi; }
                } catch (Exception ex) {
                    // Non-fatal: fall back to 1.0 now and let the first content show refine the scale.
                    System.Diagnostics.Debug.WriteLine("[CountlyWebView] content scale probe failed: " + ex);
                } finally {
                    try { if (probe != null) { probe.Close(); } } catch { }
                }
            }));
        }

        public ContentScreen GetScreen()
        {
            // Report the work area in CSS px so the server's geo comes back in CSS px — the unit the
            // content page lays out in.
            double scale = _monitorScale > 0 ? _monitorScale : 1.0;
            return new ContentScreen {
                Width = (int)(SystemParameters.WorkArea.Width / scale),
                Height = (int)(SystemParameters.WorkArea.Height / scale)
            };
        }

        public void Present(ContentPlacement portrait, ContentPlacement landscape, string url, Action onClosed)
        {
            _dispatcher.BeginInvoke(new Action(() => {
                try {
                    // Capture the scale used to report the resolution that produced this geo; place
                    // with the SAME scale (do not re-apply a freshly measured one to this window, or
                    // geo computed for the old resolution would be scaled twice).
                    double scale = _monitorScale > 0 ? _monitorScale : 1.0;
                    double originX = SystemParameters.WorkArea.Left;
                    double originY = SystemParameters.WorkArea.Top;
                    bool landscapeScreen = SystemParameters.WorkArea.Width >= SystemParameters.WorkArea.Height;

                    ContentPlacement r = (landscapeScreen ? landscape : portrait) ?? portrait ?? landscape;
                    if (r == null) { onClosed?.Invoke(); return; }

                    // Capture the content module NOW (consent is good — we were just asked to present)
                    // and relay events through it, rather than re-resolving Countly.Instance.Content()
                    // per navigation, which could return a no-op MockContent if consent is revoked or
                    // the SDK is re-initialised while this overlay is still on screen.
                    Content contentModule = CountlySDK.Countly.Instance.Content();

                    Window host = new Window {
                        WindowStyle = WindowStyle.None,
                        ResizeMode = ResizeMode.NoResize,
                        Topmost = true,
                        ShowInTaskbar = false,
                        ShowActivated = false,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        Left = originX + r.X * scale, Top = originY + r.Y * scale,
                        Width = r.W * scale, Height = r.H * scale
                    };
                    WebView2 webView = new WebView2();
                    host.Content = webView;
                    // WPF doesn't dispose child controls on close, so dispose the WebView2 explicitly
                    // to avoid leaking its browser host (msedgewebview2.exe) per content shown.
                    host.Closed += (_, __) => { try { webView.Dispose(); } catch { /* teardown best-effort */ } };

                    host.Loaded += async (_, e) => {
                        try {
                            await webView.EnsureCoreWebView2Async(null);

                            // Refine the cached monitor scale from the live page for the NEXT fetch.
                            // Not re-applied to this window (see the capture note above).
                            try {
                                double wpfDpi = System.Windows.Media.VisualTreeHelper.GetDpi(host).DpiScaleX;
                                string dprJson = await webView.CoreWebView2.ExecuteScriptAsync("window.devicePixelRatio");
                                double.TryParse(dprJson, NumberStyles.Any, CultureInfo.InvariantCulture, out double dpr);
                                if (dpr > 0 && wpfDpi > 0) { _monitorScale = dpr / wpfDpi; }
                            } catch { }

                            webView.CoreWebView2.NavigationStarting += (_, e2) => {
                                WidgetAction a = WidgetActionParser.Parse(e2.Uri);
                                if (!a.IsActionEvent) { return; }
                                // Action-event URLs are signals, never real navigations. Cancel ALL of
                                // them, or the WebView navigates to the invalid countly_action_event host
                                // and the content is replaced by a chrome-error page.
                                e2.Cancel = true;

                                // Process event/link first, close last (per the content protocol).
                                if (a.EventPayload != null) {
                                    try { contentModule.RecordContentEvents(a.EventPayload); }
                                    catch (Exception ev) { System.Diagnostics.Debug.WriteLine("[CountlyWebView] content event relay failed: " + ev); }
                                }
                                if (a.Link != null) {
                                    // ShellExecute routes http(s) -> default browser and custom schemes -> deeplink handler.
                                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(a.Link) { UseShellExecute = true }); }
                                    catch (Exception lx) { System.Diagnostics.Debug.WriteLine("[CountlyWebView] content link open failed: " + lx); }
                                }
                                if (a.HasResize) {
                                    WidgetRect rect = landscapeScreen ? (a.Landscape ?? a.Portrait) : (a.Portrait ?? a.Landscape);
                                    if (rect != null) {
                                        host.Left = originX + rect.X * scale; host.Top = originY + rect.Y * scale;
                                        host.Width = rect.W * scale; host.Height = rect.H * scale;
                                    }
                                }
                                if (a.Close) {
                                    host.Close();
                                    onClosed?.Invoke();
                                }
                            };
                            webView.CoreWebView2.Navigate(url);   // 'html' from the server is a URL
                        } catch (Exception ex) {
                            // async void: a display failure must never crash the host app.
                            System.Diagnostics.Debug.WriteLine("[CountlyWebView] content overlay failed: " + ex);
                            try { host.Close(); } catch { /* best-effort close during teardown; not actionable */ }
                            onClosed?.Invoke();   // return the module to a fetchable state so the zone is not wedged
                        }
                    };
                    host.Show();
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine("[CountlyWebView] content overlay failed to open: " + ex);
                    onClosed?.Invoke();
                }
            }));
        }
    }
}
