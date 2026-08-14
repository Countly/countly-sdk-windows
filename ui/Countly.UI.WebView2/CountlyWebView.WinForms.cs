using System;
using System.Drawing;
using System.Windows.Forms;
using CountlySDK.CountlyCommon;
using Microsoft.Web.WebView2.WinForms;

namespace CountlySDK.UI
{
    public static partial class CountlyWebView
    {
        /// <summary>
        /// Presents a feedback widget as a transparent card form placed where the widget asks
        /// (corner of the screen, or the app window if <see cref="ShowWidgetsWithinApp"/> is set).
        /// Must be called on the UI thread. No-op (logs + onClosed) if the WebView2 runtime is missing.
        /// </summary>
        public static void PresentFeedbackWidget(IWin32Window owner, CountlyFeedbackWidget widget, Action onClosed = null)
        {
            if (widget == null) { onClosed?.Invoke(); return; }
            EnsurePerMonitorDpiAware();

            if (!WebView2Runtime.IsAvailable(out _)) {
                System.Diagnostics.Debug.WriteLine("[CountlyWebView] WebView2 runtime not available; cannot present widget");
                onClosed?.Invoke();
                return;
            }

            WidgetSurface surface = ResolveSurfaceWinForms(owner);

            Form form = new Form {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                TopMost = true,
                // 1x1 on the real target monitor (not off-screen) so devicePixelRatio is measured
                // correctly at init. Moved/sized by the presenter.
                Location = new Point(surface.X, surface.Y),
                ClientSize = new Size(1, 1)
            };
            // TransparencyKey lets the card's transparent margins show through.
            form.BackColor = Color.Magenta;
            form.TransparencyKey = Color.Magenta;
            WebView2 webView = new WebView2 { Dock = DockStyle.Fill };
            form.Controls.Add(webView);

            WinFormsWebView2WidgetHost adapter = new WinFormsWebView2WidgetHost(webView, form, surface);
            FeedbackWidgetPresenter presenter = new FeedbackWidgetPresenter(adapter, CountlySDK.Countly.Instance.Feedback(), onClosed);

            form.Load += async (_, __) => {
                try {
                    await adapter.InitializeAsync();
                    await presenter.StartAsync(widget);
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine("[CountlyWebView] feedback widget failed: " + ex);
                    try { form.Close(); } catch { /* best-effort teardown */ }
                    onClosed?.Invoke();
                }
            };
            form.Show(owner);
        }

        private static WidgetSurface ResolveSurfaceWinForms(IWin32Window owner)
        {
            Control ctl = owner as Control;
            if (ShowWidgetsWithinApp && ctl != null) {
                Form form = ctl.FindForm() ?? ctl as Form;
                if (form != null) {
                    Point p = form.PointToScreen(Point.Empty);
                    // Control.DeviceDpi is net4.7+; on net462 (system-DPI only) read it from a device context.
#if NET462
                    float s;
                    using (Graphics g = form.CreateGraphics()) { s = g.DpiX / 96f; }
#else
                    float s = form.DeviceDpi / 96f;
#endif
                    return new WidgetSurface {
                        X = (int)(p.X / s), Y = (int)(p.Y / s),
                        Width = (int)(form.ClientSize.Width / s), Height = (int)(form.ClientSize.Height / s)
                    };
                }
            }
            // Screen.WorkingArea is physical px on a DPI-aware host; convert to DIPs (the WidgetSurface
            // contract) like the within-app branch and the WPF equivalent. Uses the owner's DPI when
            // available; falls back to 1.0 (no host to measure) which is correct on DPI-unaware hosts.
            float scale = 1f;
            if (ctl != null) {
#if NET462
                using (Graphics g = ctl.CreateGraphics()) { scale = g.DpiX / 96f; }
#else
                scale = ctl.DeviceDpi / 96f;
#endif
            }
            if (scale <= 0) { scale = 1f; }
            System.Drawing.Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            return new WidgetSurface {
                X = (int)(wa.X / scale), Y = (int)(wa.Y / scale),
                Width = (int)(wa.Width / scale), Height = (int)(wa.Height / scale)
            };
        }
    }
}
