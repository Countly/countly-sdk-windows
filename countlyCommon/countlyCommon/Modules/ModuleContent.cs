using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.CountlyCommon.Server.Responses;
using CountlySDK.Helpers;
using Newtonsoft.Json.Linq;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace CountlySDK.CountlyCommon
{
    internal class ModuleContent : Content, IDisposable
    {
        private readonly RequestHelper requestHelper;
        private readonly string ServerUrl;
        internal IContentDisplay display;   // set via Countly.Instance.SetContentDisplay(...)

        // Fully-qualified System.Threading.Timer to avoid the netstd CountlySDK.Helpers.TimerCallback collision.
        private System.Threading.Timer _timer;
        private readonly object _lock = new object();
        private bool _shouldFetch;
        private bool _inZone;
        private int _waitForDelay;
        private volatile bool _fetching;
        private int _generation;   // bumped on ExitContentZone so an in-flight fetch can detect it was cancelled
        private string[] _categories;   // remembered so RefreshContentZone keeps the caller's category filter
        private const int StartDelayMs = 4000;

        public ModuleContent(RequestHelper requestHelper, string serverUrl)
        {
            this.requestHelper = requestHelper;
            ServerUrl = serverUrl;
        }

        public void EnterContentZone(string[] categories = null)
        {
            if (display == null) {
                UtilityHelper.CountlyLogging("[ModuleContent] EnterContentZone, no display registered; ignoring", LogLevel.WARNING);
                return;
            }
            lock (_lock) {
                if (_inZone) { return; }
                _shouldFetch = true;
                _waitForDelay = 0;
                _categories = categories;   // remember for RefreshContentZone
                int periodMs = Math.Max(1000, Countly.Instance.Configuration.ContentZoneTimerInterval * 1000);
                if (_timer == null) {
                    _timer = new System.Threading.Timer(OnTick, categories, StartDelayMs, periodMs);
                } else {
                    _timer.Change(StartDelayMs, periodMs);
                }
            }
        }

        public void ExitContentZone()
        {
            lock (_lock) {
                _shouldFetch = false;
                _inZone = false;
                _waitForDelay = 0;
                _generation++;   // invalidate any fetch already in flight so it won't present after we exit
                if (_timer != null) { _timer.Dispose(); _timer = null; }
            }
        }

        public void Dispose()
        {
            // Standard disposal entry point (satisfies CA1001 for the owned System.Threading.Timer);
            // ExitContentZone stops polling and disposes the timer.
            ExitContentZone();
        }

        public void RefreshContentZone()
        {
            string[] categories = _categories;   // capture before ExitContentZone so the filter survives the refresh
            ExitContentZone();
            EnterContentZone(categories);
        }

        public void PreviewContent(string contentId)
        {
            if (display == null || string.IsNullOrEmpty(contentId)) { return; }
            lock (_lock) {
                // Respect the shared fetch guard so a preview can't run concurrently with a timer
                // fetch (which would stack two overlay windows on screen). FetchAndPresent's finally
                // clears _fetching.
                if (_inZone || _fetching) { return; }
                _fetching = true;
            }
            FetchAndPresent(new string[0], contentId);   // fire-and-forget; errors handled inside
        }

        private void OnTick(object state)
        {
            // Stop if Content consent was revoked while the zone was active (defense in depth; the
            // consent-change handler also calls ExitContentZone, which disposes this timer).
            if (!Countly.Instance.IsConsentGiven(ConsentFeatures.Content)) { ExitContentZone(); return; }
            lock (_lock) {
                if (_waitForDelay > 0) { _waitForDelay--; return; }
                if (!_shouldFetch || _fetching) { return; }
                _fetching = true;
            }
            FetchAndPresent((string[])state, null);      // fire-and-forget
        }

        private async Task FetchAndPresent(string[] categories, string contentId)
        {
            int gen;
            lock (_lock) { gen = _generation; }
            try {
                ContentScreen screen = display.GetScreen();
                string language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                IDictionary<string, object> parameters = ContentRequestBuilder.BuildParams(screen, categories, language, "desktop", contentId);

                RequestResult rr = await Api.Instance.SendDirectRequest(
                    ServerUrl, await requestHelper.BuildRequest(parameters), "/o/sdk/content");

                ContentData content = (rr != null && rr.responseCode == 200) ? ContentParser.ParseContentResponse(rr.responseText) : null;
                if (content == null) { return; }

                lock (_lock) {
                    // Zone was exited/refreshed/halted (or consent revoked) while this fetch was in
                    // flight -> discard the result instead of presenting stale content.
                    if (gen != _generation) { return; }
                    _shouldFetch = false; _inZone = true;
                }
                display.Present(content.Portrait, content.Landscape, content.Url, OnContentClosed);
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("[ModuleContent] FetchAndPresent failed: " + ex.Message, LogLevel.ERROR);
            } finally {
                _fetching = false;
            }
        }

        private void OnContentClosed()
        {
            lock (_lock) { _waitForDelay = 2; _shouldFetch = true; _inZone = false; }
            Action cb = Countly.Instance.Configuration != null ? Countly.Instance.Configuration.GlobalContentCallback : null;
            if (cb != null) { cb(); }
        }

        // Records events the content emitted (action=event) and force-flushes the queue so the
        // server can react immediately (journey processing). eventJson is a raw JSON array of
        // { key, segmentation|sg } objects. Called by the content display on an action-event.
        public void RecordContentEvents(string eventJson)
        {
            if (string.IsNullOrEmpty(eventJson)) { return; }
            try {
                JArray arr = JArray.Parse(eventJson);
                bool recorded = false;
                foreach (JToken t in arr) {
                    string key = (string)t["key"];
                    if (string.IsNullOrEmpty(key)) { continue; }
                    Segmentation seg = new Segmentation();
                    JToken sg = t["sg"] ?? t["segmentation"];   // "sg" takes precedence
                    if (sg is JObject sgObj) {
                        foreach (JProperty p in sgObj.Properties()) {
                            seg.Add(p.Name, TokenToInvariantString(p.Value));
                        }
                    }
                    _ = Countly.RecordEvent(key, 1, seg);
                    recorded = true;
                }
                if (recorded) { _ = Countly.Instance.Upload(); }
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("[ModuleContent] RecordContentEvents failed: " + ex.Message, LogLevel.ERROR);
            }
        }

        // Stringifies a segmentation value locale-independently: JToken.ToString() formats numbers
        // with the current culture (e.g. "4,5" on de-DE), which would corrupt the wire value.
        private static string TokenToInvariantString(JToken t)
        {
            if (t == null || t.Type == JTokenType.Null) { return null; }
            switch (t.Type) {
                case JTokenType.Float:   return ((double)t).ToString(System.Globalization.CultureInfo.InvariantCulture);
                case JTokenType.Integer: return ((long)t).ToString(System.Globalization.CultureInfo.InvariantCulture);
                default:                 return t.ToString();   // strings/bools are already invariant
            }
        }
    }

    internal class MockContent : Content
    {
        public void EnterContentZone(string[] categories = null) { }
        public void ExitContentZone() { }
        public void RefreshContentZone() { }
        public void PreviewContent(string contentId) { }
        public void RecordContentEvents(string eventJson) { }
    }

    /// <summary>Retrieves and displays Countly content (experimental).</summary>
    public interface Content
    {
        void EnterContentZone(string[] categories = null);
        void ExitContentZone();
        void RefreshContentZone();
        void PreviewContent(string contentId);
        void RecordContentEvents(string eventJson);
    }

    /// <summary>
    /// Bridge the UI package implements so the UI-less core can size the fetch to the screen and
    /// display server-placed content. Registered via <c>Countly.Instance.SetContentDisplay(...)</c>.
    /// </summary>
    public interface IContentDisplay
    {
        ContentScreen GetScreen();
        void Present(ContentPlacement portrait, ContentPlacement landscape, string url, Action onClosed);
    }
}
