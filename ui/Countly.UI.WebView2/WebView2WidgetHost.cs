using System;
using System.Threading.Tasks;
using System.Windows;
using CountlySDK.CountlyCommon;
using Microsoft.Web.WebView2.Wpf;

namespace CountlySDK.UI
{
    /// <summary>
    /// Thin WPF adapter mapping <see cref="IWidgetWebHost"/> onto a WebView2 in an opaque, borderless
    /// caller-owned card <see cref="Window"/>. Sizes the window to the widget's <c>resize_me</c> rect
    /// (scaled by the measured window-units-per-CSS-px); if no <c>resize_me</c> arrives (legacy rating
    /// template), it falls back to measuring the rendered content.
    /// </summary>
    internal sealed class WebView2WidgetHost : IWidgetWebHost
    {
        public event Action<string> NavigationStarting;
        public event Action<WidgetAction> ResizeRequested;
        public event Action PageLoaded;
        public event Action LoadFailed;
        public WidgetSurface Surface { get; }

        private readonly WebView2 _webView;
        private readonly Window _window;
        // window-units per CSS-px (= devicePixelRatio / host DPI scale). 1.0 until measured at init.
        private double _scale = 1.0;
        private bool _placed;
        private bool _pageLoaded;

        public WebView2WidgetHost(WebView2 webView, Window window, WidgetSurface surface)
        {
            _webView = webView;
            _window = window;
            Surface = surface;
            // WPF (unlike WinForms) does not dispose child controls when a Window closes, so the
            // WebView2's CoreWebView2 browser host would leak (an orphaned msedgewebview2.exe).
            // Dispose it on close, covering every teardown path (CloseHost, load failure, user close).
            _window.Closed += (_, __) => { try { _webView.Dispose(); } catch { /* teardown best-effort */ } };
        }

        public async Task InitializeAsync()
        {
            await _webView.EnsureCoreWebView2Async(null);
            _webView.DefaultBackgroundColor = System.Drawing.Color.White;

            // Measure window-units-per-CSS-px = devicePixelRatio / host DPI scale. On a DPI-unaware
            // host this is the monitor scale (e.g. 1.25); on a DPI-aware host it is 1.0. Read on the
            // blank page (already on the target monitor) so it's ready before the first resize_me.
            try {
                double wpfDpi = System.Windows.Media.VisualTreeHelper.GetDpi(_window).DpiScaleX;
                string dprJson = await _webView.CoreWebView2.ExecuteScriptAsync("window.devicePixelRatio");
                double.TryParse(dprJson, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double dpr);
                if (dpr > 0 && wpfDpi > 0) { _scale = dpr / wpfDpi; }
            } catch { /* keep _scale = 1.0 */ }

            // Bridge: the widget posts resize_me/close to window.parent; forward those to the host.
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
                    // The very first navigation is the widget URL. If it fails before any successful
                    // load (offline/404/error page), the card would stay an invisible, undismissed
                    // 1x1 window — dismiss it. Later failures (e.g. the cancelled countly_action_event
                    // close navigation) happen after _pageLoaded and are ignored here.
                    if (!_pageLoaded) { LoadFailed?.Invoke(); }
                    return;
                }
                _pageLoaded = true;
                PageLoaded?.Invoke();
                // Fallback: if no resize_me placed the widget shortly after load (e.g. the rating
                // template, which never sends one), measure the rendered content and place it.
                await Task.Delay(900);
                if (!_placed) { PlaceByMeasuredContent(); }
            };
        }

        public void Navigate(string url) { _webView.CoreWebView2.Navigate(url); }

        public void ReportSurfaceSize(int width, int height)
        {
            string script = "window.postMessage({type:'resize',width:" + width + ",height:" + height + "},'*');";
            _ = _webView.CoreWebView2.ExecuteScriptAsync(script);
        }

        public void PlaceAndShow(WidgetRect r)
        {
            _placed = true;
            // Scale the window by the measured window-units-per-CSS-px so the webview's CSS viewport
            // equals the card's CSS size -> content fits. Re-clamp to stay on-screen.
            int w = (int)(r.W * _scale);
            int h = (int)(r.H * _scale);
            int localX = r.X - Surface.X;
            int localY = r.Y - Surface.Y;
            int x = Surface.X + Math.Max(0, Math.Min(localX, Math.Max(0, Surface.Width - w)));
            int y = Surface.Y + Math.Max(0, Math.Min(localY, Math.Max(0, Surface.Height - h)));
            _window.Left = x;
            _window.Top = y;
            _window.Width = w;
            _window.Height = h;
            if (!_window.IsVisible) { _window.Show(); }
        }

        // Fallback for widgets that send no resize_me: lay the content out at a default width,
        // measure its height, then center-place the window sized to it.
        private async void PlaceByMeasuredContent()
        {
            _placed = true;
            try {
                const int cssW = 400;
                int w = (int)(cssW * _scale);
                _window.Left = Surface.X;
                _window.Top = Surface.Y;
                _window.Width = w;
                _window.Height = Surface.Height;
                if (!_window.IsVisible) { _window.Show(); }
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
                _window.Left = x;
                _window.Top = y;
                _window.Width = w;
                _window.Height = h;
            } catch (Exception ex) { System.Diagnostics.Debug.WriteLine("[CountlyWebView] widget content-measure fallback failed: " + ex); }
        }

        public void CloseHost() { _window.Close(); }
    }
}
