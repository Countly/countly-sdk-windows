using System;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon;

namespace CountlySDK.UI
{
    /// <summary>
    /// Drives a feedback widget: navigates the URL, reports the surface size once the page has
    /// loaded (so the widget can compute its corner rect), places the card window at the widget's
    /// requested rect, and handles close. No WebView2 types — unit-testable with a fake host.
    /// </summary>
    public class FeedbackWidgetPresenter
    {
        private readonly IWidgetWebHost _host;
        private readonly Feedback _feedback;
        private readonly Action _onClosed;
        private CountlyFeedbackWidget _widget;
        private bool _surfaceReported;

        public FeedbackWidgetPresenter(IWidgetWebHost host, Feedback feedback, Action onClosed)
        {
            _host = host;
            _feedback = feedback;
            _onClosed = onClosed;
            _host.NavigationStarting += OnNavigationStarting;
            _host.ResizeRequested += OnResizeRequested;
            _host.PageLoaded += OnPageLoaded;
            _host.LoadFailed += OnLoadFailed;
        }

        // The widget URL failed to load; dismiss the (invisible) card and notify the caller so the
        // presentation doesn't wedge with a stuck window and no onClosed callback.
        private void OnLoadFailed()
        {
            _host.CloseHost();
            _onClosed?.Invoke();
        }

        /// <summary>Fetches the widget's display URL and navigates the host to it.</summary>
        public async Task StartAsync(CountlyFeedbackWidget widget)
        {
            _widget = widget;
            string url = await _feedback.ConstructFeedbackWidgetUrl(widget);
            // A null/empty URL means the widget is unavailable (e.g. Feedback consent not given, so
            // the core returns MockFeedback). Tear down cleanly rather than navigating to null,
            // which would throw and leave a 1x1 window flashing on screen.
            if (string.IsNullOrEmpty(url)) {
                _host.CloseHost();
                _onClosed?.Invoke();
                return;
            }
            _host.Navigate(url);
        }

        // The widget only computes a correct rect once it knows the viewport, and it only accepts
        // our resize message after its own listener is attached — i.e. after the page has loaded.
        private void OnPageLoaded()
        {
            if (_surfaceReported) { return; }
            _surfaceReported = true;
            _host.ReportSurfaceSize(_host.Surface.Width, _host.Surface.Height);
            // Placement happens when the widget posts resize_me (OnResizeRequested). Widgets that
            // never post one (e.g. the rating template) are handled by the host's own fallback.
        }

        private void OnResizeRequested(WidgetAction action)
        {
            WidgetRect rect = WidgetPlacement.Resolve(action, _host.Surface);
            if (rect == null) { return; }
            _host.PlaceAndShow(rect);
        }

        private void OnNavigationStarting(string url)
        {
            WidgetAction action = WidgetActionParser.Parse(url);
            if (action.IsActionEvent && action.Close) {
                _ = _feedback.ReportFeedbackWidgetManually(_widget, null, null);
                _host.CloseHost();
                _onClosed?.Invoke();
            }
        }
    }
}
