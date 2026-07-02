using System;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon;

namespace CountlySDK.UI
{
    /// <summary>
    /// Drives a feedback widget in an <see cref="IWidgetWebHost"/>: builds the display URL,
    /// interprets the widget's <c>countly_action_event</c> navigations (resize / close), reports
    /// the result, and notifies on close. Contains ALL the display logic and no WebView2 types,
    /// so it is fully unit-testable with a fake host.
    /// </summary>
    public class FeedbackWidgetPresenter
    {
        private readonly IWidgetWebHost _host;
        private readonly Feedback _feedback;
        private readonly bool _isLandscape;
        private readonly Action _onClosed;
        private CountlyFeedbackWidget _widget;

        public FeedbackWidgetPresenter(IWidgetWebHost host, Feedback feedback, bool isLandscape, Action onClosed)
        {
            _host = host;
            _feedback = feedback;
            _isLandscape = isLandscape;
            _onClosed = onClosed;
            _host.NavigationStarting += OnNavigationStarting;
        }

        /// <summary>Fetches the widget's display URL and navigates the host to it.</summary>
        public async Task StartAsync(CountlyFeedbackWidget widget)
        {
            _widget = widget;
            string url = await _feedback.ConstructFeedbackWidgetUrl(widget);
            _host.Navigate(url);
        }

        private void OnNavigationStarting(string url)
        {
            WidgetAction action = WidgetActionParser.Parse(url);
            if (!action.IsActionEvent) { return; }

            if (action.HasResize) {
                WidgetRect r = _isLandscape ? (action.Landscape ?? action.Portrait) : (action.Portrait ?? action.Landscape);
                if (r != null) { _host.ResizeTo(r.W, r.H); }
            }

            if (action.Close) {
                // Report the widget as closed/cancelled (null result), then dismiss.
                _ = _feedback.ReportFeedbackWidgetManually(_widget, null, null);
                _host.CloseHost();
                _onClosed?.Invoke();
            }
        }
    }
}
