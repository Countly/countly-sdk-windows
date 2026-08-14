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
        public event Action<WidgetAction> ResizeRequested;
        public event Action PageLoaded;
        public event Action LoadFailed;
        public WidgetSurface Surface { get; set; } = new WidgetSurface { X = 0, Y = 0, Width = 1920, Height = 1080 };
        public string NavigatedUrl;
        public (int w, int h)? ReportedSize;
        public WidgetRect Placed;
        public bool Closed;

        public void Navigate(string url) { NavigatedUrl = url; }
        public void ReportSurfaceSize(int w, int h) { ReportedSize = (w, h); }
        public void PlaceAndShow(WidgetRect r) { Placed = r; }
        public void CloseHost() { Closed = true; }

        public void FireNav(string url) { NavigationStarting?.Invoke(url); }
        public void FireResize(WidgetAction a) { ResizeRequested?.Invoke(a); }
        public void FirePageLoaded() { PageLoaded?.Invoke(); }
        public void FireLoadFailed() { LoadFailed?.Invoke(); }
    }

    internal sealed class FakeFeedback : Feedback
    {
        public bool ReportedClose;
        public bool NullUrl;   // simulate an unavailable widget (e.g. MockFeedback returns null)
        public Task<CountlyFeedbackWidget[]> GetAvailableFeedbackWidgets() { return Task.FromResult(new CountlyFeedbackWidget[0]); }
        public Task<JObject> GetFeedbackWidgetData(CountlyFeedbackWidget w) { return Task.FromResult<JObject>(null); }
        public Task ReportFeedbackWidgetManually(CountlyFeedbackWidget w, JObject d, Dictionary<string, object> r)
        {
            if (r == null) { ReportedClose = true; }
            return Task.FromResult(0);
        }
        public Task<string> ConstructFeedbackWidgetUrl(CountlyFeedbackWidget w)
        {
            return Task.FromResult(NullUrl ? null : "https://s/feedback/nps?widget_id=" + w.widgetId);
        }
    }

    public class FeedbackWidgetPresenterTests
    {
        [Fact]
        public async Task NavigatesOnStart_ReportsSurfaceOnPageLoad()
        {
            FakeHost host = new FakeHost { Surface = new WidgetSurface { Width = 1920, Height = 1080 } };
            FeedbackWidgetPresenter p = new FeedbackWidgetPresenter(host, new FakeFeedback(), () => { });
            await p.StartAsync(new CountlyFeedbackWidget { widgetId = "w1", type = FeedbackWidgetType.nps });
            Assert.Equal("https://s/feedback/nps?widget_id=w1", host.NavigatedUrl);
            Assert.Null(host.ReportedSize); // not until the page loads

            host.FirePageLoaded();
            Assert.Equal((1920, 1080), host.ReportedSize);

            host.FirePageLoaded(); // report once only
            Assert.Equal((1920, 1080), host.ReportedSize);
        }

        [Fact]
        public async Task ResizeRequested_PlacesCardAtResolvedRect()
        {
            FakeHost host = new FakeHost { Surface = new WidgetSurface { X = 0, Y = 0, Width = 1920, Height = 1080 } };
            FeedbackWidgetPresenter p = new FeedbackWidgetPresenter(host, new FakeFeedback(), () => { });
            await p.StartAsync(new CountlyFeedbackWidget { widgetId = "w1", type = FeedbackWidgetType.nps });

            host.FireResize(new WidgetAction {
                IsActionEvent = true, HasResize = true,
                Landscape = new WidgetRect { X = 1420, Y = 460, W = 500, H = 620 },
                Portrait = new WidgetRect { X = 0, Y = 0, W = 300, H = 400 }
            });

            Assert.Equal(1420, host.Placed.X);
            Assert.Equal(500, host.Placed.W);
        }

        [Fact]
        public async Task ResizeRequested_WithoutUsableRect_DoesNotPlace()
        {
            FakeHost host = new FakeHost();
            FeedbackWidgetPresenter p = new FeedbackWidgetPresenter(host, new FakeFeedback(), () => { });
            await p.StartAsync(new CountlyFeedbackWidget { widgetId = "w1", type = FeedbackWidgetType.nps });

            host.FireResize(new WidgetAction { IsActionEvent = true, HasResize = false });
            Assert.Null(host.Placed);
        }

        [Fact]
        public async Task Close_ReportsClosedAndClosesHost()
        {
            FakeHost host = new FakeHost();
            FakeFeedback fb = new FakeFeedback();
            bool closedCb = false;
            FeedbackWidgetPresenter p = new FeedbackWidgetPresenter(host, fb, () => closedCb = true);
            await p.StartAsync(new CountlyFeedbackWidget { widgetId = "w1", type = FeedbackWidgetType.nps });

            host.FireNav("https://countly_action_event/?cly_widget_command=1&close=1");

            Assert.True(fb.ReportedClose);
            Assert.True(host.Closed);
            Assert.True(closedCb);
        }

        [Fact]
        public async Task NullUrl_TearsDownWithoutNavigating()
        {
            FakeHost host = new FakeHost();
            bool closedCb = false;
            FeedbackWidgetPresenter p = new FeedbackWidgetPresenter(host, new FakeFeedback { NullUrl = true }, () => closedCb = true);

            await p.StartAsync(new CountlyFeedbackWidget { widgetId = "w1", type = FeedbackWidgetType.nps });

            Assert.Null(host.NavigatedUrl);   // never navigated to a null URL
            Assert.True(host.Closed);
            Assert.True(closedCb);
        }

        [Fact]
        public async Task LoadFailed_ClosesHostAndFiresOnClosed()
        {
            FakeHost host = new FakeHost();
            bool closedCb = false;
            FeedbackWidgetPresenter p = new FeedbackWidgetPresenter(host, new FakeFeedback(), () => closedCb = true);
            await p.StartAsync(new CountlyFeedbackWidget { widgetId = "w1", type = FeedbackWidgetType.nps });

            host.FireLoadFailed();

            Assert.True(host.Closed);
            Assert.True(closedCb);
        }
    }
}
