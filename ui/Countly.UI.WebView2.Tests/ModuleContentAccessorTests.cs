using CountlySDK.Entities;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    // Runtime verification of the Content() accessor wiring. 'Countly' is fully qualified because
    // the test's 'Countly.UI.WebView2.Tests' namespace shadows the CountlySDK.Countly class.
    public class ModuleContentAccessorTests
    {
        [Fact]
        public void Content_ReturnsMock_WhenConsentRequiredAndNotGiven()
        {
            CountlySDK.Countly.Halt();
            CountlyConfig cc = new CountlyConfig {
                serverUrl = "https://no-server.count.ly",
                appKey = "APP_KEY",
                appVersion = "1.0",
                consentRequired = true
            };
            CountlySDK.Countly.Instance.Init(cc).Wait();

            // No Content consent -> MockContent -> operations are safe no-ops.
            CountlySDK.Countly.Instance.Content().EnterContentZone();
            CountlySDK.Countly.Instance.Content().ExitContentZone();
            CountlySDK.Countly.Instance.Content().RefreshContentZone();
            CountlySDK.Countly.Instance.Content().PreviewContent("cid");

            CountlySDK.Countly.Halt();
        }

        [Fact]
        public void Accessors_WhenNotInitialized_ReturnMocksWithoutThrowing()
        {
            // Repro of the reported crash: calling an accessor before Init (Configuration == null)
            // NRE'd at CountlyBase.Content() 'if (Configuration.backendMode)'. Configuration is
            // internal and never nulled by Halt, so force the not-initialized state via reflection.
            CountlySDK.Countly.Halt();
            System.Reflection.FieldInfo cfg = typeof(CountlySDK.CountlyCommon.CountlyBase).GetField(
                "Configuration", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(cfg);
            cfg.SetValue(CountlySDK.Countly.Instance, null);

            // Each accessor must return a no-op mock, not throw NullReferenceException.
            Assert.NotNull(CountlySDK.Countly.Instance.Content());
            CountlySDK.Countly.Instance.Content().EnterContentZone();
            Assert.NotNull(CountlySDK.Countly.Instance.Feedback());
            Assert.NotNull(CountlySDK.Countly.Instance.RemoteConfig());
        }
    }
}
