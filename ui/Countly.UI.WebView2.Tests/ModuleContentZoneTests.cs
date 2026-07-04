using System;
using System.Threading;
using CountlySDK.CountlyCommon;
using CountlySDK.Entities;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    internal sealed class FakeContentDisplay : IContentDisplay
    {
        public ContentScreen Screen = new ContentScreen { Width = 1000, Height = 800 };
        public volatile string PresentedUrl;
        public volatile ContentPlacement PresentedPortrait;
        public Action LastOnClosed;

        public ContentScreen GetScreen() { return Screen; }
        public void Present(ContentPlacement portrait, ContentPlacement landscape, string url, Action onClosed)
        {
            PresentedPortrait = portrait;
            PresentedUrl = url;
            LastOnClosed = onClosed;
        }
    }

    public class ModuleContentZoneTests
    {
        [Fact]
        public void EnterContentZone_FetchesAndPresents()
        {
            using LocalMockServer server = new LocalMockServer((rawUrl, body) =>
                (rawUrl + body).Contains("method=queue")
                    ? "{\"geo\":{\"p\":{\"x\":0,\"y\":0,\"w\":320,\"h\":480},\"l\":{\"x\":0,\"y\":0,\"w\":800,\"h\":600}},\"html\":\"https://s/content/1\"}"
                    : null);

            CountlySDK.Countly.Halt();
            CountlyConfig cc = new CountlyConfig { serverUrl = server.Url, appKey = "APP_KEY", appVersion = "1.0" };
            cc.ContentZoneTimerInterval = 16;
            CountlySDK.Countly.Instance.Init(cc).Wait();

            FakeContentDisplay display = new FakeContentDisplay();
            CountlySDK.Countly.Instance.SetContentDisplay(display);
            CountlySDK.Countly.Instance.Content().EnterContentZone();

            for (int i = 0; i < 120 && display.PresentedUrl == null; i++) { Thread.Sleep(100); }

            Assert.Equal("https://s/content/1", display.PresentedUrl);
            Assert.Equal(320, display.PresentedPortrait.W);

            CountlySDK.Countly.Instance.Content().ExitContentZone();
            CountlySDK.Countly.Halt();
        }

        [Fact]
        public void PreviewContent_OneShotFetchesAndPresents()
        {
            using LocalMockServer server = new LocalMockServer((rawUrl, body) =>
                (rawUrl + body).Contains("preview=true")
                    ? "{\"geo\":{\"p\":{\"x\":0,\"y\":0,\"w\":300,\"h\":300},\"l\":{\"x\":0,\"y\":0,\"w\":300,\"h\":300}},\"html\":\"https://s/content/preview\"}"
                    : null);

            CountlySDK.Countly.Halt();
            CountlyConfig cc = new CountlyConfig { serverUrl = server.Url, appKey = "APP_KEY", appVersion = "1.0" };
            CountlySDK.Countly.Instance.Init(cc).Wait();

            FakeContentDisplay display = new FakeContentDisplay();
            CountlySDK.Countly.Instance.SetContentDisplay(display);
            CountlySDK.Countly.Instance.Content().PreviewContent("cid1");

            for (int i = 0; i < 50 && display.PresentedUrl == null; i++) { Thread.Sleep(100); }
            Assert.Equal("https://s/content/preview", display.PresentedUrl);
            CountlySDK.Countly.Halt();
        }
    }
}
