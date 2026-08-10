## 26.1.0
* Initial release.
* Added "CountlyWebView.PresentFeedbackWidget(...)" for displaying Countly feedback widgets (Surveys, NPS, Ratings) in a WebView2 host:
  * WPF overload: "PresentFeedbackWidget(System.Windows.Window owner, CountlyFeedbackWidget widget, Action onClosed = null)"
  * WinForms overload: "PresentFeedbackWidget(System.Windows.Forms.IWin32Window owner, CountlyFeedbackWidget widget, Action onClosed = null)"
  * Dynamically resizes the host window to the widget's requested size and auto-reports the result when the widget is closed.
* Added "CountlyWebView.EnableContentZone()" / "DisableContentZone()" for displaying Countly content (experimental) as a borderless, top-most WebView2 overlay positioned by the server on the primary screen (WPF).
* Added "WebView2Runtime.IsAvailable(out string version)" to detect the WebView2 Evergreen Runtime; when the runtime is missing, presentation is a graceful no-op.
* Targets .NET Framework 4.6.2 and .NET 8 (Windows). Depends on the "Countly" SDK package and "Microsoft.Web.WebView2".
* Requires the WebView2 Evergreen Runtime to be installed on the end-user machine.
