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
        /// Presents a feedback widget in a WebView2-hosted WinForms form owned by
        /// <paramref name="owner"/>. Must be called on the UI thread. If the WebView2 runtime is
        /// missing, this is a graceful no-op (logs and invokes <paramref name="onClosed"/>).
        /// </summary>
        public static void PresentFeedbackWidget(IWin32Window owner, CountlyFeedbackWidget widget, Action onClosed = null)
        {
            if (widget == null) { onClosed?.Invoke(); return; }

            if (!WebView2Runtime.IsAvailable(out _)) {
                System.Diagnostics.Debug.WriteLine("[CountlyWebView] WebView2 runtime not available; cannot present widget");
                onClosed?.Invoke();
                return;
            }

            Form form = new Form {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(400, 600),
                ShowInTaskbar = false
            };
            WebView2 webView = new WebView2 { Dock = DockStyle.Fill };
            form.Controls.Add(webView);

            WinFormsWebView2WidgetHost adapter = new WinFormsWebView2WidgetHost(webView, form);
            FeedbackWidgetPresenter presenter = new FeedbackWidgetPresenter(adapter, CountlySDK.Countly.Instance.Feedback(), isLandscape: false, onClosed);

            form.Load += async (s, e) => {
                try {
                    await adapter.InitializeAsync();
                    await presenter.StartAsync(widget);
                } catch (Exception ex) {
                    // async void: a display failure must never crash the host app.
                    System.Diagnostics.Debug.WriteLine("[CountlyWebView] feedback widget failed: " + ex);
                    try { form.Close(); } catch { }
                    onClosed?.Invoke();
                }
            };
            form.Show(owner);
        }
    }
}
