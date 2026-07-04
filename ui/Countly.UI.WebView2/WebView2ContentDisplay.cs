using System;
using System.Windows;
using System.Windows.Threading;
using CountlySDK.CountlyCommon;
using Microsoft.Web.WebView2.Wpf;

namespace CountlySDK.UI
{
    /// <summary>
    /// WPF implementation of the core <see cref="IContentDisplay"/> bridge: reports the primary
    /// screen size and shows server-placed content in a borderless, top-most, non-modal WebView2
    /// window at the given coordinates. Constructed on the UI thread (captures its Dispatcher).
    /// </summary>
    internal sealed class WebView2ContentDisplay : IContentDisplay
    {
        private readonly Dispatcher _dispatcher;
        private readonly int _screenW, _screenH;

        public WebView2ContentDisplay()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;                 // captured on the UI thread
            _screenW = (int)SystemParameters.WorkArea.Width;
            _screenH = (int)SystemParameters.WorkArea.Height;
        }

        public ContentScreen GetScreen()
        {
            return new ContentScreen { Width = _screenW, Height = _screenH };
        }

        public void Present(ContentPlacement portrait, ContentPlacement landscape, string url, Action onClosed)
        {
            _dispatcher.BeginInvoke(new Action(() => {
                try {
                    ContentPlacement r = (_screenW >= _screenH ? landscape : portrait) ?? portrait ?? landscape;
                    if (r == null) { onClosed?.Invoke(); return; }

                    Window host = new Window {
                        WindowStyle = WindowStyle.None,
                        ResizeMode = ResizeMode.NoResize,
                        Topmost = true,
                        ShowInTaskbar = false,
                        ShowActivated = false,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        Left = r.X, Top = r.Y, Width = r.W, Height = r.H
                    };
                    WebView2 webView = new WebView2();
                    host.Content = webView;

                    host.Loaded += async (s, e) => {
                        try {
                            await webView.EnsureCoreWebView2Async(null);
                            webView.CoreWebView2.NavigationStarting += (s2, e2) => {
                                WidgetAction a = WidgetActionParser.Parse(e2.Uri);
                                if (!a.IsActionEvent) { return; }

                                if (a.HasResize) {
                                    WidgetRect rect = _screenW >= _screenH ? (a.Landscape ?? a.Portrait) : (a.Portrait ?? a.Landscape);
                                    if (rect != null) {
                                        host.Left = rect.X; host.Top = rect.Y;
                                        host.Width = rect.W; host.Height = rect.H;
                                    }
                                }
                                if (a.Close) {
                                    e2.Cancel = true;
                                    host.Close();
                                    onClosed?.Invoke();
                                }
                            };
                            webView.CoreWebView2.Navigate(url);   // 'html' from the server is a URL
                        } catch (Exception ex) {
                            // async void: a display failure must never crash the host app.
                            System.Diagnostics.Debug.WriteLine("[CountlyWebView] content overlay failed: " + ex);
                            try { host.Close(); } catch { }
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
