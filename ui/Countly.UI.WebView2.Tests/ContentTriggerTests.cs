using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CountlySDK.CountlyCommon;
using CountlySDK.Entities;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    /// <summary>
    /// Content triggers: the "jt" key next to "c" in the behavior settings answer, and the journey
    /// trigger events and views that apply without it. A matched trigger sends the queued requests and
    /// then asks for content, up to three times a second apart while the server has none.
    /// </summary>
    public class ContentTriggerTests
    {
        private const string ContentRequest = "method=queue";

        /// <summary>
        /// The behavior settings request asks for the content triggers with jt=1, unless
        /// DisableAutoContentTriggers is set.
        /// </summary>
        [Fact]
        public void SettingsRequest_AsksForContentTriggers_UnlessTurnedOff()
        {
            using (LocalMockServer server = Start(Answer("{\"tracking\":true}", "{\"e\":[\"purchase\"]}"))) {
                Init(server, null);
                WaitFor(() => Count(server, "method=sc") >= 1, 10);
                Assert.Equal(1, Count(server, "jt=1"));

                Init(server, cc => cc.DisableAutoContentTriggers());
                WaitFor(() => Count(server, "method=sc") >= 2, 10);
                Assert.Equal(1, Count(server, "jt=1"));
                CountlySDK.Countly.Halt();
            }
        }

        /// <summary>
        /// A listed event, while the content zone is entered, sends the queued requests first and then
        /// asks for content three times while there is none.
        /// </summary>
        [Fact]
        public void ListedEvent_SendsTheQueueThenAsksForContent_ThreeTimes()
        {
            using (LocalMockServer server = Start(Answer("{\"tracking\":true}", "{\"e\":[\"purchase\"]}"))) {
                Init(server, null);
                CountlySDK.Countly.Instance.SetContentDisplay(new FakeContentDisplay());
                CountlySDK.Countly.Instance.Content().EnterContentZone();
                WaitFor(() => Count(server, ContentRequest) >= 1, 10);
                int zoneRequests = Count(server, ContentRequest);
                int from = Snapshot(server).Count;

                CountlySDK.Countly.RecordEvent("purchase").Wait();
                WaitFor(() => Count(server, ContentRequest) >= zoneRequests + 3, 10);
                Thread.Sleep(1500);

                Assert.Equal(zoneRequests + 3, Count(server, ContentRequest));
                List<string> sinceTrigger = Snapshot(server).Skip(from).ToList();
                int eventsAt = sinceTrigger.FindIndex(r => r.Contains("events") && r.Contains("purchase"));
                int contentAt = sinceTrigger.FindIndex(r => r.Contains(ContentRequest));
                Assert.True(eventsAt >= 0 && eventsAt < contentAt, "the event goes out before content is asked for");
                CountlySDK.Countly.Halt();
            }
        }

        /// <summary>
        /// Content triggers apply only while the content zone is entered, and an answer with content
        /// triggers, even an empty one, makes the SDK ignore the journey trigger events.
        /// </summary>
        [Fact]
        public void ContentTriggers_NeedTheContentZone_AndHideTheJourneyTriggerEvents()
        {
            using (LocalMockServer server = Start(Answer("{\"jte\":[\"legacy\"]}", "{\"e\":[\"purchase\"]}"))) {
                Init(server, null);
                CountlySDK.Countly.Instance.SetContentDisplay(new FakeContentDisplay());
                WaitFor(() => Count(server, "method=sc") >= 1, 10);

                CountlySDK.Countly.RecordEvent("purchase").Wait();
                CountlySDK.Countly.RecordEvent("legacy").Wait();
                Thread.Sleep(2500);

                Assert.Equal(0, Count(server, ContentRequest));
                CountlySDK.Countly.Halt();
            }
        }

        /// <summary>
        /// Without content triggers, the journey trigger events and views ask for content directly, also
        /// outside the content zone and while the content zone refresh is off: an event of the app whose
        /// key is listed, and a listed view when it starts and again when it ends.
        /// </summary>
        [Fact]
        public void JourneyTriggers_AskForContentDirectly_ForEventsOfTheAppAndBothViewEvents()
        {
            using (LocalMockServer server = Start(Answer("{\"rcz\":false,\"jte\":[\"purchase\",\"[CLY]_star_rating\"],\"jtv\":[\"checkout\"]}", null))) {
                Init(server, null);
                CountlySDK.Countly.Instance.SetContentDisplay(new FakeContentDisplay());
                WaitFor(() => Count(server, "method=sc") >= 1, 10);

                CountlySDK.Countly.RecordEvent("[CLY]_star_rating").Wait();
                Thread.Sleep(2000);
                Assert.Equal(0, Count(server, ContentRequest));

                CountlySDK.Countly.RecordEvent("purchase").Wait();
                WaitFor(() => Count(server, ContentRequest) >= 3, 10);
                Assert.Equal(3, Count(server, ContentRequest));

                CountlySDK.Countly.Instance.RecordView("checkout").Wait();
                WaitFor(() => Count(server, ContentRequest) >= 6, 10);
                Assert.Equal(6, Count(server, ContentRequest));

                CountlySDK.Countly.Instance.RecordView("home").Wait();
                WaitFor(() => Count(server, ContentRequest) >= 9, 10);
                Assert.Equal(9, Count(server, ContentRequest));
                CountlySDK.Countly.Halt();
            }
        }

        /// <summary>
        /// On a first start, without stored or provided settings, the first answer's "ecz" enters the
        /// content zone once a display is registered.
        /// </summary>
        [Fact]
        public void EnterContentZoneSetting_OfTheFirstAnswerOfAFirstStart_EntersTheZone()
        {
            using (LocalMockServer server = Start(Answer("{\"ecz\":true}", null))) {
                Init(server, null);
                WaitFor(() => Count(server, "method=sc") >= 1, 10);
                CountlySDK.Countly.Instance.SetContentDisplay(new FakeContentDisplay());

                WaitFor(() => Count(server, ContentRequest) >= 1, 10);
                Assert.True(Count(server, ContentRequest) >= 1);
                CountlySDK.Countly.Halt();
            }
        }

        /// <summary>
        /// With settings provided at init, their "ecz" decides; a later answer that turns it on does not
        /// enter the content zone.
        /// </summary>
        [Fact]
        public void EnterContentZoneSetting_OfALaterAnswer_DoesNotEnterTheZone()
        {
            using (LocalMockServer server = Start(Answer("{\"ecz\":true}", null))) {
                Init(server, cc => cc.SetSDKBehaviorSettings("{\"v\":1,\"t\":1,\"c\":{\"ecz\":false}}"));
                WaitFor(() => Count(server, "method=sc") >= 1, 10);
                CountlySDK.Countly.Instance.SetContentDisplay(new FakeContentDisplay());

                Thread.Sleep(6000);
                Assert.Equal(0, Count(server, ContentRequest));
                CountlySDK.Countly.Halt();
            }
        }

        private static string Answer(string settings, string contentTriggers)
        {
            return "{\"v\":1,\"t\":1,\"c\":" + settings + (contentTriggers == null ? string.Empty : ",\"jt\":" + contentTriggers) + "}";
        }

        private static LocalMockServer Start(string settingsAnswer)
        {
            return new LocalMockServer((rawUrl, body) => {
                string request = rawUrl + body;
                if (request.Contains("method=sc")) { return settingsAnswer; }
                if (request.Contains(ContentRequest)) { return "{}"; }
                return null;
            });
        }

        private static void Init(LocalMockServer server, Action<CountlyConfig> adjust)
        {
            CountlySDK.Countly.Halt();
            CountlyConfig cc = new CountlyConfig { serverUrl = server.Url, appKey = "APP_KEY", appVersion = "1.0" };
            if (adjust != null) { adjust(cc); }
            CountlySDK.Countly.Instance.Init(cc).Wait();
        }

        private static List<string> Snapshot(LocalMockServer server)
        {
            lock (server.Requests) {
                return server.Requests.Select(r => Uri.UnescapeDataString((r.RawUrl ?? string.Empty) + (r.Body ?? string.Empty))).ToList();
            }
        }

        private static int Count(LocalMockServer server, string text)
        {
            return Snapshot(server).Count(r => r.Contains(text));
        }

        private static void WaitFor(Func<bool> condition, int seconds)
        {
            for (int i = 0; i < seconds * 10 && !condition(); i++) { Thread.Sleep(100); }
        }
    }
}
