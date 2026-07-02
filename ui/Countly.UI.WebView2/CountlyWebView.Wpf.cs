using System;
using System.Windows;
using CountlySDK.CountlyCommon;
using Microsoft.Web.WebView2.Wpf;

namespace CountlySDK.UI
{
    public static partial class CountlyWebView
    {
        /// <summary>
        /// Presents a feedback widget in a WebView2-hosted WPF window owned by
        /// <paramref name="owner"/>. Must be called on the UI thread. If the WebView2 runtime is
        /// missing, this is a graceful no-op (logs and invokes <paramref name="onClosed"/>).
        /// </summary>
        public static void PresentFeedbackWidget(Window owner, CountlyFeedbackWidget widget, Action onClosed = null)
        {
            if (widget == null) { onClosed?.Invoke(); return; }

            if (!WebView2Runtime.IsAvailable(out _)) {
                System.Diagnostics.Debug.WriteLine("[CountlyWebView] WebView2 runtime not available; cannot present widget");
                onClosed?.Invoke();
                return;
            }

            Window host = new Window {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = 400,
                Height = 600,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false
            };
            WebView2 webView = new WebView2();
            host.Content = webView;

            WebView2WidgetHost adapter = new WebView2WidgetHost(webView, host);
            bool isLandscape = owner != null && owner.ActualWidth >= owner.ActualHeight;
            FeedbackWidgetPresenter presenter = new FeedbackWidgetPresenter(adapter, CountlySDK.Countly.Instance.Feedback(), isLandscape, onClosed);

            host.Loaded += async (s, e) => {
                await adapter.InitializeAsync();
                await presenter.StartAsync(widget);
            };
            host.Show();
        }
    }
}
