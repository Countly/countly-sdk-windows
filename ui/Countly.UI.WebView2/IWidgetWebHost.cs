using System;
using CountlySDK.CountlyCommon;

namespace CountlySDK.UI
{
    /// <summary>
    /// Abstraction over the embedded browser that hosts a widget. Keeps the presentation logic in
    /// <see cref="FeedbackWidgetPresenter"/> testable without a real WebView2. The presenter reports
    /// the surface size to the widget, then places the card window at the rect the widget requests
    /// (its <c>resize_me</c>). The concrete adapters (WPF / WinForms) are thin.
    /// </summary>
    public interface IWidgetWebHost
    {
        /// <summary>Raised for each navigation the widget initiates; the argument is the target URI (used for close).</summary>
        event Action<string> NavigationStarting;

        /// <summary>Raised when the widget posts a usable <c>resize_me</c> (bridged from the page).</summary>
        event Action<WidgetAction> ResizeRequested;

        /// <summary>Raised once the widget page has finished loading (safe to post the surface size).</summary>
        event Action PageLoaded;

        /// <summary>Raised when the initial widget navigation fails (offline/404/error) before any
        /// successful load, so the presenter can dismiss the otherwise-invisible card and fire onClosed.</summary>
        event Action LoadFailed;

        /// <summary>The coordinate space handed to the widget (DIPs, screen-absolute origin).</summary>
        WidgetSurface Surface { get; }

        /// <summary>Loads the given URL in the host.</summary>
        void Navigate(string url);

        /// <summary>Tells the widget how much room it has by posting a <c>{type:'resize',width,height}</c> message.</summary>
        void ReportSurfaceSize(int width, int height);

        /// <summary>Positions + sizes the card window to the widget's requested rect (DIPs) and shows it.</summary>
        void PlaceAndShow(WidgetRect screenRect);

        /// <summary>Closes/dismisses the host window.</summary>
        void CloseHost();
    }
}
