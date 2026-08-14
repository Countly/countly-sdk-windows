# Countly.UI.WebView2

WebView2-based WPF / WinForms UI for the [Countly Windows SDK](https://github.com/Countly/countly-sdk-windows).
It renders **Feedback Widgets** (Surveys, NPS, Ratings) and the **Content** feature on the desktop.
It is a thin companion to the core `Countly` package — the core drives analytics; this package only
displays.

## Requirements

- **Microsoft Edge WebView2 Evergreen Runtime** must be installed on the end-user machine. It ships
  with recent Windows 10/11 and Microsoft Edge, but is not guaranteed on every machine — distribute
  the [Evergreen Bootstrapper/Standalone installer](https://developer.microsoft.com/microsoft-edge/webview2/)
  if you need to guarantee it. The SDK degrades gracefully (no-ops) when the runtime is absent.
- `Countly` core package (a matching version is a dependency of this package).
- .NET Framework 4.6.2+ or .NET 8 (`net462` / `net8.0-windows`), WPF or WinForms.

## DPI awareness

For crisp, correctly-placed widgets and content, make the host process **per-monitor DPI aware**
(an `app.manifest` `dpiAwareness` entry is the standard way). The SDK measures and compensates for
DPI at runtime, so it also works on DPI-unaware hosts, but per-monitor awareness is recommended.

## Usage

Initialize the core SDK as usual, then:

### Feedback widgets

```csharp
using CountlySDK.UI;

// Fetch available widgets via the core, then present one:
var widgets = await Countly.Instance.Feedback().GetAvailableFeedbackWidgets();

// WPF (call on the UI thread; owner is your Window):
CountlyWebView.PresentFeedbackWidget(this, widgets[0], onClosed: () => { /* dismissed */ });
```

The card sizes and positions itself where the widget asks (a screen corner by default, or within the
app window if `CountlyWebView.ShowWidgetsWithinApp = true`).

### Content

```csharp
using CountlySDK.UI;

// Registers the display bridge and starts polling for content (experimental):
CountlyWebView.EnableContentZone();
// ...
CountlyWebView.DisableContentZone();
```

Content interactions (events, external links, resize, close) are handled automatically; emitted
events are recorded through the core and the queue is flushed so the server can react immediately.

## License

MIT — see the [LICENSE](https://github.com/Countly/countly-sdk-windows/blob/master/LICENSE).
