using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace CountlySDK.UI
{
    /// <summary>
    /// Thin WinForms adapter mapping <see cref="IWidgetWebHost"/> onto a WebView2 control hosted
    /// in a caller-owned <see cref="Form"/>. Logic-free by design.
    /// </summary>
    internal sealed class WinFormsWebView2WidgetHost : IWidgetWebHost
    {
        public event Action<string> NavigationStarting;

        private readonly WebView2 _webView;
        private readonly Form _form;

        public WinFormsWebView2WidgetHost(WebView2 webView, Form form)
        {
            _webView = webView;
            _form = form;
        }

        public async Task InitializeAsync()
        {
            await _webView.EnsureCoreWebView2Async(null);
            _webView.CoreWebView2.NavigationStarting += (s, e) => NavigationStarting?.Invoke(e.Uri);
        }

        public void Navigate(string url) { _webView.CoreWebView2.Navigate(url); }

        public void ResizeTo(int cssWidth, int cssHeight)
        {
            // WinForms is pixel-based; convert CSS px to device px for the form's current DPI.
            float scale = _form.DeviceDpi / 96f;
            _form.ClientSize = new Size((int)(cssWidth * scale), (int)(cssHeight * scale));
        }

        public void CloseHost() { _form.Close(); }
    }
}
