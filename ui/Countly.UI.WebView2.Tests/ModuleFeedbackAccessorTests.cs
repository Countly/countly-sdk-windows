using CountlySDK.CountlyCommon;
using CountlySDK.Entities;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    // Runtime verification of the Feedback() accessor wiring (T2). Uses only PUBLIC API, so it
    // works from this external harness (no SDK internals needed). Confirms Countly.Init runs
    // under the net8.0-windows host and that consent gating returns the no-op MockFeedback.
    // Note: 'Countly' must be fully qualified as CountlySDK.Countly here, because the test's own
    // 'Countly.UI.WebView2.Tests' namespace shadows the CountlySDK.Countly class name.
    public class ModuleFeedbackAccessorTests
    {
        [Fact]
        public void Feedback_ReturnsMock_WhenConsentRequiredAndNotGiven()
        {
            CountlySDK.Countly.Halt();
            CountlyConfig cc = new CountlyConfig {
                serverUrl = "https://no-server.count.ly",
                appKey = "APP_KEY",
                appVersion = "1.0",
                consentRequired = true
            };
            CountlySDK.Countly.Instance.Init(cc).Wait();

            // Consent for Feedback not granted -> accessor returns MockFeedback -> empty, never null, no throw.
            CountlyFeedbackWidget[] widgets = CountlySDK.Countly.Instance.Feedback().GetAvailableFeedbackWidgets().Result;

            Assert.NotNull(widgets);
            Assert.Empty(widgets);
            CountlySDK.Countly.Halt();
        }
    }
}
