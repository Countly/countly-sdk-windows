using System;
using System.Windows;
using System.Windows.Media;
using CountlySDK.CountlyCommon;
using Microsoft.Web.WebView2.Wpf;

namespace CountlySDK.UI
{
    public static partial class CountlyWebView
    {
        /// <summary>
        /// Presents a feedback widget as a transparent, click-through-surrounded card window placed
        /// where the widget asks (corner of the screen, or the app window if
        /// <see cref="ShowWidgetsWithinApp"/> is set). Must be called on the UI thread. No-op (logs +
        /// onClosed) if the WebView2 runtime is missing.
        /// </summary>
        public static void PresentFeedbackWidget(Window owner, CountlyFeedbackWidget widget, Action onClosed = null)
        {
            if (widget == null) { onClosed?.Invoke(); return; }
            EnsurePerMonitorDpiAware();

            if (!WebView2Runtime.IsAvailable(out _)) {
                System.Diagnostics.Debug.WriteLine("[CountlyWebView] WebView2 runtime not available; cannot present widget");
                onClosed?.Invoke();
                return;
            }

            WidgetSurface surface = ResolveSurface(owner);

            // Transparent card window; starts tiny + off-screen so WebView2 can initialize, then the
            // presenter moves/sizes it to the widget's requested rect.
            // Opaque borderless window: a transparent (AllowsTransparency) window hit-tests by WPF's
            // own visual alpha, which treats the WebView2 region as transparent -> clicks fall
            // through the card. Opaque keeps the card interactive; clicks outside simply aren't ours.
            Window host = new Window {
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Topmost = true,
                // Owner couples the card to the host app's lifecycle/z-order (minimizes and closes
                // with it, never orphaned on top of other apps), matching the WinForms Show(owner) path.
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.Manual,
                // Start as a 1x1 window on the real target monitor (not off-screen) so the
                // devicePixelRatio measured at init reflects the correct monitor. Moved by PlaceAndShow.
                Left = surface.X, Top = surface.Y, Width = 1, Height = 1
            };
            WebView2 webView = new WebView2();
            host.Content = webView;

            WebView2WidgetHost adapter = new WebView2WidgetHost(webView, host, surface);
            FeedbackWidgetPresenter presenter = new FeedbackWidgetPresenter(adapter, CountlySDK.Countly.Instance.Feedback(), onClosed);

            host.Loaded += async (_, __) => {
                try {
                    await adapter.InitializeAsync();
                    await presenter.StartAsync(widget);
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine("[CountlyWebView] feedback widget failed: " + ex);
                    try { host.Close(); } catch { /* best-effort teardown */ }
                    onClosed?.Invoke();
                }
            };
            host.Show();
        }

        private static WidgetSurface ResolveSurface(Window owner)
        {
            if (ShowWidgetsWithinApp && owner != null) {
                Point tl = owner.PointToScreen(new Point(0, 0));
                DpiScale dpi = VisualTreeHelper.GetDpi(owner);
                return new WidgetSurface {
                    X = (int)(tl.X / dpi.DpiScaleX),
                    Y = (int)(tl.Y / dpi.DpiScaleY),
                    Width = (int)owner.ActualWidth,
                    Height = (int)owner.ActualHeight
                };
            }
            return new WidgetSurface {
                X = (int)SystemParameters.WorkArea.Left,
                Y = (int)SystemParameters.WorkArea.Top,
                Width = (int)SystemParameters.WorkArea.Width,
                Height = (int)SystemParameters.WorkArea.Height
            };
        }
    }
}
