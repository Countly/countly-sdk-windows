using System;

namespace CountlySDK.UI
{
    /// <summary>
    /// Abstraction over the embedded browser that hosts a widget. Keeps all presentation logic
    /// in <see cref="FeedbackWidgetPresenter"/> testable without instantiating a real WebView2
    /// (which requires a UI thread + the WebView2 runtime). The concrete adapters
    /// (WebView2WidgetHost / WinFormsWebView2WidgetHost) are thin implementations.
    /// </summary>
    public interface IWidgetWebHost
    {
        /// <summary>Raised for each navigation the widget initiates; the argument is the target URI.</summary>
        event Action<string> NavigationStarting;

        /// <summary>Loads the given URL in the host.</summary>
        void Navigate(string url);

        /// <summary>Resizes the owning window to the widget's requested content size (CSS px).</summary>
        void ResizeTo(int cssWidth, int cssHeight);

        /// <summary>Closes/dismisses the host window.</summary>
        void CloseHost();
    }
}
