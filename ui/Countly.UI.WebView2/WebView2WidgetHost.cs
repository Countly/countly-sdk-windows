using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Wpf;

namespace CountlySDK.UI
{
    /// <summary>
    /// Thin WPF adapter mapping <see cref="IWidgetWebHost"/> onto a WebView2 control hosted in a
    /// caller-owned <see cref="Window"/>. Intentionally logic-free — all behavior lives in
    /// <see cref="FeedbackWidgetPresenter"/>.
    /// </summary>
    internal sealed class WebView2WidgetHost : IWidgetWebHost
    {
        public event Action<string> NavigationStarting;

        private readonly WebView2 _webView;
        private readonly Window _window;

        public WebView2WidgetHost(WebView2 webView, Window window)
        {
            _webView = webView;
            _window = window;
        }

        public async Task InitializeAsync()
        {
            await _webView.EnsureCoreWebView2Async(null);
            _webView.CoreWebView2.NavigationStarting += (s, e) => NavigationStarting?.Invoke(e.Uri);
        }

        public void Navigate(string url) { _webView.CoreWebView2.Navigate(url); }

        public void ResizeTo(int cssWidth, int cssHeight)
        {
            // WPF sizes are device-independent units (96=1.0), which match WebView2's CSS px.
            _window.Width = cssWidth;
            _window.Height = cssHeight;
        }

        public void CloseHost() { _window.Close(); }
    }
}
