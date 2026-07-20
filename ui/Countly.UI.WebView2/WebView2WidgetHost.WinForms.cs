using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using CountlySDK.CountlyCommon;
using Microsoft.Web.WebView2.WinForms;

namespace CountlySDK.UI
{
    /// <summary>
    /// Thin WinForms adapter mapping <see cref="IWidgetWebHost"/> onto a WebView2 in a caller-owned
    /// card <see cref="Form"/>. Mirrors the WPF adapter (scaled placement + measure-content fallback).
    /// </summary>
    internal sealed class WinFormsWebView2WidgetHost : IWidgetWebHost
    {
        public event Action<string> NavigationStarting;
        public event Action<WidgetAction> ResizeRequested;
        public event Action PageLoaded;
        public event Action LoadFailed;
        public WidgetSurface Surface { get; }

        private readonly WebView2 _webView;
        private readonly Form _form;
        private double _scale = 1.0;
        private bool _placed;
        private bool _pageLoaded;

        public WinFormsWebView2WidgetHost(WebView2 webView, Form form, WidgetSurface surface)
        {
            _webView = webView;
            _form = form;
            Surface = surface;
        }

        public async Task InitializeAsync()
        {
            await _webView.EnsureCoreWebView2Async(null);
            _webView.DefaultBackgroundColor = System.Drawing.Color.White;

            try {
                // Control.DeviceDpi is net4.7+; on net462 (system-DPI only) read it from a device context.
#if NET462
                double hostScale;
                using (System.Drawing.Graphics g = _form.CreateGraphics()) { hostScale = g.DpiX / 96f; }
#else
                double hostScale = _form.DeviceDpi / 96f;
#endif
                string dprJson = await _webView.CoreWebView2.ExecuteScriptAsync("window.devicePixelRatio");
                double.TryParse(dprJson, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double dpr);
                if (dpr > 0 && hostScale > 0) { _scale = dpr / hostScale; }
            } catch { /* keep _scale = 1.0 */ }

            await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                "(function(){var w=window.chrome&&window.chrome.webview;if(!w)return;" +
                "window.addEventListener('message',function(ev){try{var d=typeof ev.data==='string'?JSON.parse(ev.data):ev.data;" +
                "if(d&&d.cly_widget_command){w.postMessage(JSON.stringify(d));}}catch(e){}});})();");

            _webView.CoreWebView2.WebMessageReceived += (s, e) => {
                string json;
                try { json = e.TryGetWebMessageAsString(); } catch { return; }
                if (WidgetMessageParser.TryParse(json, out WidgetAction a) && a.HasResize) {
                    ResizeRequested?.Invoke(a);
                }
            };

            _webView.CoreWebView2.NavigationStarting += (s, e) => NavigationStarting?.Invoke(e.Uri);
            _webView.CoreWebView2.NavigationCompleted += async (s, e) => {
                if (!e.IsSuccess) {
                    // Initial widget navigation failed (offline/404/error) before any successful load:
                    // dismiss the invisible 1x1 card. Later failures (post-load) are ignored here.
                    if (!_pageLoaded) { LoadFailed?.Invoke(); }
                    return;
                }
                _pageLoaded = true;
                PageLoaded?.Invoke();
                await Task.Delay(900);
                if (!_placed) { PlaceByMeasuredContent(); }
            };
        }

        public void Navigate(string url) { _webView.CoreWebView2.Navigate(url); }

        public void ReportSurfaceSize(int width, int height)
        {
            _ = _webView.CoreWebView2.ExecuteScriptAsync("window.postMessage({type:'resize',width:" + width + ",height:" + height + "},'*');");
        }

        public void PlaceAndShow(WidgetRect r)
        {
            _placed = true;
            int w = (int)(r.W * _scale);
            int h = (int)(r.H * _scale);
            int localX = r.X - Surface.X;
            int localY = r.Y - Surface.Y;
            int x = Surface.X + Math.Max(0, Math.Min(localX, Math.Max(0, Surface.Width - w)));
            int y = Surface.Y + Math.Max(0, Math.Min(localY, Math.Max(0, Surface.Height - h)));
            _form.Location = new System.Drawing.Point(x, y);
            _form.ClientSize = new System.Drawing.Size(w, h);
            if (!_form.Visible) { _form.Show(); }
        }

        private async void PlaceByMeasuredContent()
        {
            _placed = true;
            try {
                const int cssW = 400;
                int w = (int)(cssW * _scale);
                _form.Location = new System.Drawing.Point(Surface.X, Surface.Y);
                _form.ClientSize = new System.Drawing.Size(w, Surface.Height);
                if (!_form.Visible) { _form.Show(); }
                await Task.Delay(400);

                string js = "(function(){var e=document.getElementById('widget-body');" +
                    "return Math.ceil(e?e.scrollHeight:document.body.scrollHeight);})()";
                int.TryParse(await _webView.CoreWebView2.ExecuteScriptAsync(js),
                    System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out int cssH);
                if (cssH <= 0) { cssH = 500; }
                int h = (int)(cssH * _scale);
                if (h > Surface.Height) { h = Surface.Height; }

                int x = Surface.X + Math.Max(0, (Surface.Width - w) / 2);
                int y = Surface.Y + Math.Max(0, (Surface.Height - h) / 2);
                _form.Location = new System.Drawing.Point(x, y);
                _form.ClientSize = new System.Drawing.Size(w, h);
            } catch { /* best-effort fallback */ }
        }

        public void CloseHost() { _form.Close(); }
    }
}
