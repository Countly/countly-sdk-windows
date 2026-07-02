using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon;
using CountlySDK.UI;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    internal sealed class FakeHost : IWidgetWebHost
    {
        public event Action<string> NavigationStarting;
        public string NavigatedUrl;
        public (int w, int h)? Resized;
        public bool Closed;

        public void Navigate(string url) { NavigatedUrl = url; }
        public void ResizeTo(int w, int h) { Resized = (w, h); }
        public void CloseHost() { Closed = true; }
        public void Fire(string url) { NavigationStarting?.Invoke(url); }
    }

    internal sealed class FakeFeedback : Feedback
    {
        public bool ReportedClose;
        public Task<CountlyFeedbackWidget[]> GetAvailableFeedbackWidgets() { return Task.FromResult(new CountlyFeedbackWidget[0]); }
        public Task<JObject> GetFeedbackWidgetData(CountlyFeedbackWidget w) { return Task.FromResult<JObject>(null); }
        public Task ReportFeedbackWidgetManually(CountlyFeedbackWidget w, JObject d, Dictionary<string, object> r)
        {
            if (r == null) { ReportedClose = true; }
            return Task.FromResult(0);
        }
        public Task<string> ConstructFeedbackWidgetUrl(CountlyFeedbackWidget w) { return Task.FromResult("https://s/feedback/nps?widget_id=" + w.widgetId); }
    }

    public class FeedbackWidgetPresenterTests
    {
        [Fact]
        public async Task Start_NavigatesToConstructedUrl()
        {
            FakeHost host = new FakeHost();
            FeedbackWidgetPresenter p = new FeedbackWidgetPresenter(host, new FakeFeedback(), isLandscape: false, onClosed: () => { });
            await p.StartAsync(new CountlyFeedbackWidget { widgetId = "w1", type = FeedbackWidgetType.nps });
            Assert.Equal("https://s/feedback/nps?widget_id=w1", host.NavigatedUrl);
        }

        [Fact]
        public async Task Resize_PicksPortraitRect_WhenPortrait()
        {
            FakeHost host = new FakeHost();
            FeedbackWidgetPresenter p = new FeedbackWidgetPresenter(host, new FakeFeedback(), isLandscape: false, onClosed: () => { });
            await p.StartAsync(new CountlyFeedbackWidget { widgetId = "w1", type = FeedbackWidgetType.nps });

            host.Fire("https://countly_action_event?cly_widget_command=1&action=resize_me&resize_me=" +
                Uri.EscapeDataString("{\"p\":{\"x\":0,\"y\":0,\"w\":320,\"h\":480},\"l\":{\"x\":0,\"y\":0,\"w\":800,\"h\":600}}"));

            Assert.Equal((320, 480), host.Resized);
        }

        [Fact]
        public async Task Close_ReportsClosedAndClosesHost()
        {
            FakeHost host = new FakeHost();
            FakeFeedback fb = new FakeFeedback();
            bool closedCb = false;
            FeedbackWidgetPresenter p = new FeedbackWidgetPresenter(host, fb, isLandscape: false, onClosed: () => closedCb = true);
            await p.StartAsync(new CountlyFeedbackWidget { widgetId = "w1", type = FeedbackWidgetType.nps });

            host.Fire("https://countly_action_event?cly_widget_command=1&close=1");

            Assert.True(fb.ReportedClose);
            Assert.True(host.Closed);
            Assert.True(closedCb);
        }
    }
}
