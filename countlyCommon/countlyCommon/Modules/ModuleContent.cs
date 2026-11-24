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
        private readonly Func<string> ServerUrl;
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

        private const int TriggerContentRequests = 3;
        private const int TriggerContentRetryMs = 1000;
        private const int TriggerEarlyLimit = 50;
        internal const string ContentTriggerGapsFilename = "contentTriggerGaps.xml";

        // set after the app or the 'ecz' setting entered the content zone, until the zone is left
        private volatile bool _zoneWanted;
        // set after the app left the content zone, until it enters it again
        private volatile bool _zoneExited;
        // set once the behavior settings answer of this run arrived, or once none will arrive
        private volatile bool _settingsSettled;
        private volatile bool _triggersHalted;
        private readonly object _triggerLock = new object();
        private bool _roundRunning;
        private bool _roundPending;
        private int _contentTriggerChain;
        private string _contentTriggerGaps;
        private System.Threading.Timer _retryTimer;
        private readonly List<ContentTrigger> _earlyTriggers = new List<ContentTrigger>();

        public ModuleContent(RequestHelper requestHelper, Func<string> serverUrl)
        {
            this.requestHelper = requestHelper;
            ServerUrl = serverUrl;
        }

        public void EnterContentZone(string[] categories = null)
        {
            if (display == null) {
                UtilityHelper.CountlyLogging("[ModuleContent] EnterContentZone, no display registered; ignoring", LogLevel.WARNING);
                ModuleHealthCheck.Track("w700");
                return;
            }
            _zoneWanted = true;
            _zoneExited = false;
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
            _zoneWanted = false;
            _zoneExited = true;
            lock (_lock) {
                _shouldFetch = false;
                _inZone = false;
                _waitForDelay = 0;
                _generation++;   // invalidate any fetch already in flight so it won't present after we exit
                if (_timer != null) { _timer.Dispose(); _timer = null; }
            }
        }

        /// <summary>
        /// Stops the content zone and the content triggers for good, when the SDK is halted.
        /// </summary>
        internal void Halt()
        {
            _triggersHalted = true;
            lock (_triggerLock) {
                if (_retryTimer != null) { _retryTimer.Dispose(); _retryTimer = null; }
                _earlyTriggers.Clear();
            }
            ExitContentZone();
        }

        public void Dispose()
        {
            // Standard disposal entry point (satisfies CA1001 for the owned System.Threading.Timer);
            // ExitContentZone stops polling and disposes the timer.
            ExitContentZone();
        }

        public void RefreshContentZone()
        {
            ModuleServerConfig serverConfig = Countly.Instance.moduleServerConfig;
            if (serverConfig != null && !serverConfig.GetRefreshContentZoneEnabled()) {
                UtilityHelper.CountlyLogging("[ModuleContent] RefreshContentZone, disabled by server config, ignoring");
                return;
            }
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

        /// <summary>
        /// Asks the server for content and shows what it answers with.
        /// </summary>
        /// <returns>true when content was shown; false for no content, a failed request or an answer without content</returns>
        private async Task<bool> FetchAndPresent(string[] categories, string contentId)
        {
            int gen;
            lock (_lock) { gen = _generation; }
            try {
                ContentScreen screen = display.GetScreen();
                string language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                IDictionary<string, object> parameters = ContentRequestBuilder.BuildParams(screen, categories, language, "desktop", contentId);

                RequestResult rr = await Api.Instance.SendDirectRequest(
                    ServerUrl(), await requestHelper.BuildRequest(parameters), "/o/sdk/content");

                ContentData content = (rr != null && rr.responseCode == 200) ? ContentParser.ParseContentResponse(rr.responseText) : null;
                if (content == null) { return false; }

                lock (_lock) {
                    // Zone was exited/refreshed/halted (or consent revoked) while this fetch was in
                    // flight -> discard the result instead of presenting stale content.
                    if (gen != _generation) { return false; }
                    _shouldFetch = false; _inZone = true;
                }
                display.Present(content.Portrait, content.Landscape, content.Url, OnContentClosed);
                return true;
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("[ModuleContent] FetchAndPresent failed: " + ex.Message, LogLevel.ERROR);
                return false;
            } finally {
                _fetching = false;
            }
        }

        /// <summary>
        /// Something the SDK recorded that can be a content trigger. A match outside its gap sends the
        /// queued requests and then asks for content.
        /// </summary>
        internal void OnContentTrigger(ContentTrigger trigger)
        {
            if (_triggersHalted || trigger == null) { return; }
            ContentTriggerMatch match;
            lock (_triggerLock) {
                match = MatchOf(trigger);
                if (match == null) {
                    RememberEarlyTrigger(trigger);
                    return;
                }
                if (!ClaimRound(trigger, match)) { return; }
            }
            RunRound();
        }

        /// <summary>
        /// The behavior settings answer of this run arrived and was applied, or none will arrive. The
        /// first remembered trigger the answer lists starts a round.
        /// </summary>
        internal void OnBehaviorSettingsSettled(bool answered)
        {
            if (_triggersHalted) { return; }
            List<ContentTrigger> early;
            lock (_triggerLock) {
                bool firstAnswer = answered && !_settingsSettled;
                _settingsSettled = true;
                early = new List<ContentTrigger>(_earlyTriggers);
                _earlyTriggers.Clear();
                if (!firstAnswer) { return; }
            }
            foreach (ContentTrigger trigger in early) {
                bool start;
                lock (_triggerLock) {
                    ContentTriggerMatch match = MatchOf(trigger);
                    if (match == null) { continue; }
                    UtilityHelper.CountlyLogging("[ModuleContent] OnBehaviorSettingsSettled, [" + trigger + "] was recorded before the behavior settings arrived");
                    start = ClaimRound(trigger, match);
                }
                if (start) { RunRound(); }
                return;
            }
        }

        private ContentTriggerMatch MatchOf(ContentTrigger trigger)
        {
            ModuleServerConfig serverConfig = Countly.Instance.moduleServerConfig;
            if (serverConfig == null) { return null; }
            return ContentTriggers.Match(serverConfig.GetContentTriggers(), _zoneWanted, serverConfig.IsJourneyTriggerEvent, serverConfig.IsJourneyTriggerView, trigger);
        }

        // under _triggerLock: keeps what was recorded before the behavior settings answer of this run
        private void RememberEarlyTrigger(ContentTrigger trigger)
        {
            if (_settingsSettled || _earlyTriggers.Count >= TriggerEarlyLimit) { return; }
            foreach (ContentTrigger early in _earlyTriggers) {
                if (early.SameAs(trigger)) { return; }
            }
            _earlyTriggers.Add(trigger);
        }

        // under _triggerLock: whether a matched trigger starts a round now; a trigger during a running
        // round only makes one more round follow it
        private bool ClaimRound(ContentTrigger trigger, ContentTriggerMatch match)
        {
            if (_zoneExited || display == null || !Countly.Instance.IsConsentGiven(ConsentFeatures.Content)) {
                UtilityHelper.CountlyLogging("[ModuleContent] OnContentTrigger, [" + trigger + "] is a content trigger, but content can not be shown now, so it is not asked for");
                return false;
            }
            if (_contentTriggerGaps == null) {
                ServerConfigEntity stored = Storage.Instance.LoadFromFile<ServerConfigEntity>(ContentTriggerGapsFilename).Result;
                _contentTriggerGaps = stored == null || stored.Json == null ? string.Empty : stored.Json;
            }
            string gaps = ContentTriggers.ClaimGap(match, _contentTriggerGaps, NowMs());
            if (gaps == null) {
                UtilityHelper.CountlyLogging("[ModuleContent] OnContentTrigger, [" + trigger + "] started a round less than [" + (match.GapMs / 1000) + "] seconds ago");
                return false;
            }
            _contentTriggerGaps = gaps;
            Storage.Instance.SaveToFile<ServerConfigEntity>(ContentTriggerGapsFilename, new ServerConfigEntity { Json = gaps });
            if (_roundRunning) {
                UtilityHelper.CountlyLogging("[ModuleContent] OnContentTrigger, [" + trigger + "] matched while a round is running, one more round follows it");
                _roundPending = true;
                return false;
            }
            UtilityHelper.CountlyLogging("[ModuleContent] OnContentTrigger, [" + trigger + "] matched, sending the queued requests and asking for content");
            _roundRunning = true;
            return true;
        }

        private static long NowMs()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        /// <summary>
        /// Sends the queued requests and, once they are sent, asks for content. When they can not be
        /// sent, the round ends without a content request, and the content zone timer picks it up.
        /// </summary>
        private async void RunRound()
        {
            while (true) {
                lock (_triggerLock) { _roundPending = false; }
                bool sent = await Countly.Instance.Upload();
                int chain = 0;
                lock (_triggerLock) {
                    if (sent && !_triggersHalted) { chain = ++_contentTriggerChain; }
                }
                if (chain != 0) {
                    RequestTriggeredContent(chain, 1);
                } else {
                    UtilityHelper.CountlyLogging("[ModuleContent] RunRound, the queued requests could not be sent, the round ends without a content request");
                }
                lock (_triggerLock) {
                    if (!_roundPending || _triggersHalted) {
                        _roundRunning = false;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Sends a content request of a round. When it shows nothing, it is sent again a second later,
        /// up to three requests, as the server may still be processing what triggered it. A newer
        /// round's content request stops the ones left of an older round.
        /// </summary>
        private async void RequestTriggeredContent(int chain, int attempt)
        {
            lock (_triggerLock) {
                if (chain != _contentTriggerChain || _triggersHalted) { return; }
            }
            ModuleServerConfig serverConfig = Countly.Instance.moduleServerConfig;
            bool zoneNeeded = serverConfig != null && serverConfig.GetContentTriggers() != null;
            if (_zoneExited || (zoneNeeded && !_zoneWanted) || display == null || !Countly.Instance.IsConsentGiven(ConsentFeatures.Content)
                || (serverConfig != null && !serverConfig.GetNetworkingEnabled())) {
                UtilityHelper.CountlyLogging("[ModuleContent] RequestTriggeredContent, the content zone is not wanted now, no content request is sent");
                return;
            }

            bool claimed = false;
            bool shownAlready = false;
            lock (_lock) {
                if (_inZone) {
                    shownAlready = true;
                } else if (!_fetching) {
                    _fetching = true;
                    claimed = true;
                }
            }
            if (shownAlready) {
                UtilityHelper.CountlyLogging("[ModuleContent] RequestTriggeredContent, content is already shown, no content request is sent");
                return;
            }

            bool shown = false;
            if (claimed) {
                UtilityHelper.CountlyLogging("[ModuleContent] RequestTriggeredContent, sending content request [" + attempt + "] of [" + TriggerContentRequests + "]");
                shown = await FetchAndPresent(_categories ?? new string[0], null);
            }
            if (shown || attempt >= TriggerContentRequests) { return; }

            lock (_triggerLock) {
                if (chain != _contentTriggerChain || _triggersHalted) { return; }
                if (_retryTimer != null) { _retryTimer.Dispose(); }
                _retryTimer = new System.Threading.Timer(_ => RequestTriggeredContent(chain, attempt + 1), null, TriggerContentRetryMs, System.Threading.Timeout.Infinite);
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

    /// <summary>
    /// The content triggers ("jt") the server sends next to the SDK behavior settings ("c"): event keys
    /// ("e"), view names ("v") and user properties ("p") whose recording makes the SDK send the queued
    /// requests and ask for content right away. A list the server leaves out is an empty one.
    /// </summary>
    internal class ContentTriggers
    {
        internal const string KeySession = "[CLY]_session";
        internal const string KeyUserProperties = "[CLY]_property_update";
        internal const string KeyCrash = "[CLY]_crash";
        internal const string KeyConsent = "[CLY]_consent";
        internal const string KeyView = "[CLY]_view";
        // least time before the same trigger starts another round: a listed key, view name or property
        internal const long GapSpecificMs = 10000;
        // least time before the same trigger starts another round: any view, any property or any error
        internal const long GapNonSpecificMs = 60000;

        internal readonly HashSet<string> Keys;
        internal readonly HashSet<string> Views;
        internal readonly HashSet<string> Properties;

        private ContentTriggers(HashSet<string> keys, HashSet<string> views, HashSet<string> properties)
        {
            Keys = keys;
            Views = views;
            Properties = properties;
        }

        /// <summary>Reads the "jt" value of a behavior settings answer.</summary>
        /// <returns>the content triggers, or null when the answer has none or an invalid value, so the journey trigger events and views apply</returns>
        internal static ContentTriggers Parse(JToken value)
        {
            if (value == null || value.Type == JTokenType.Null) { return null; }
            JObject triggers = value as JObject;
            if (triggers == null) {
                UtilityHelper.CountlyLogging("[ContentTriggers] Parse, content triggers (jt) must be an object, ignoring them", LogLevel.WARNING);
                return null;
            }
            return new ContentTriggers(ReadList(triggers, "e"), ReadList(triggers, "v"), ReadList(triggers, "p"));
        }

        private static HashSet<string> ReadList(JObject triggers, string name)
        {
            HashSet<string> values = new HashSet<string>();
            JToken list = triggers[name];
            if (list == null || list.Type == JTokenType.Null) { return values; }
            JArray array = list as JArray;
            if (array == null) {
                UtilityHelper.CountlyLogging("[ContentTriggers] Parse, content triggers (jt) list [" + name + "] must be an array, ignoring it", LogLevel.WARNING);
                return values;
            }
            foreach (JToken item in array) {
                if (item.Type == JTokenType.String && !string.IsNullOrEmpty((string)item)) { values.Add((string)item); }
            }
            return values;
        }

        /// <summary>
        /// Whether what was recorded is a content trigger. When the server sent content triggers, only
        /// they decide, and only while the content zone is wanted. Otherwise an event of the app whose
        /// key the journey trigger events list, or a view, when it starts and when it ends, whose name
        /// the journey trigger views list, decides, also while the content zone is not wanted.
        /// </summary>
        /// <returns>the match, or null when it is not a content trigger</returns>
        internal static ContentTriggerMatch Match(ContentTriggers triggers, bool zoneWanted, Func<string, bool> isJourneyTriggerEvent, Func<string, bool> isJourneyTriggerView, ContentTrigger trigger)
        {
            if (triggers != null) {
                return zoneWanted ? triggers.MatchLists(trigger) : null;
            }
            if (trigger.IsView) {
                return trigger.ViewName != null && isJourneyTriggerView(trigger.ViewName) ? ViewMatch(trigger) : null;
            }
            return trigger.CustomEvent && isJourneyTriggerEvent(trigger.Key) ? new ContentTriggerMatch(new List<string> { trigger.Key }, GapSpecificMs) : null;
        }

        private ContentTriggerMatch MatchLists(ContentTrigger trigger)
        {
            if (trigger.IsView && trigger.ViewName != null && Views.Contains(trigger.ViewName)) {
                return ViewMatch(trigger);
            }
            if (trigger.Key == KeyUserProperties && trigger.Properties != null) {
                List<string> gapKeys = new List<string>();
                foreach (string property in trigger.Properties) {
                    if (Properties.Contains(property)) { gapKeys.Add("p:" + property); }
                }
                if (gapKeys.Count > 0) { return new ContentTriggerMatch(gapKeys, GapSpecificMs); }
            }
            if (!Keys.Contains(trigger.Key)) { return null; }
            bool nonSpecific = trigger.IsView || trigger.Key == KeyUserProperties || trigger.Key == KeyCrash;
            return new ContentTriggerMatch(new List<string> { trigger.Key + trigger.GapKeySuffix }, nonSpecific ? GapNonSpecificMs : GapSpecificMs);
        }

        private static ContentTriggerMatch ViewMatch(ContentTrigger trigger)
        {
            return new ContentTriggerMatch(new List<string> { "v:" + trigger.ViewName + trigger.GapKeySuffix }, GapSpecificMs);
        }

        /// <summary>
        /// Whether a matched trigger may start a round now. Times older than the longest gap are
        /// dropped, and a time in the future is ignored.
        /// </summary>
        /// <param name="storedGaps">when each trigger last started a round: a JSON object of gap key to milliseconds, may be empty</param>
        /// <returns>the times to store when the trigger may start a round, or null when every one of its triggers is inside its gap</returns>
        internal static string ClaimGap(ContentTriggerMatch match, string storedGaps, long nowMs)
        {
            JObject times;
            try {
                times = string.IsNullOrEmpty(storedGaps) ? new JObject() : JObject.Parse(storedGaps);
            } catch (Exception) {
                times = new JObject();
            }

            bool allInGap = true;
            foreach (string gapKey in match.GapKeys) {
                if (!InGap(times[gapKey], nowMs, match.GapMs)) { allInGap = false; break; }
            }
            if (allInGap) { return null; }

            JObject kept = new JObject();
            foreach (JProperty stored in times.Properties()) {
                if (InGap(stored.Value, nowMs, GapNonSpecificMs)) { kept[stored.Name] = stored.Value; }
            }
            foreach (string gapKey in match.GapKeys) { kept[gapKey] = nowMs; }
            return kept.ToString(Newtonsoft.Json.Formatting.None);
        }

        private static bool InGap(JToken time, long nowMs, long gapMs)
        {
            if (time == null || (time.Type != JTokenType.Integer && time.Type != JTokenType.Float)) { return false; }
            long timeMs = (long)time;
            return nowMs >= timeMs && nowMs - timeMs < gapMs;
        }
    }

    /// <summary>Something the SDK recorded, as content triggers compare it.</summary>
    internal class ContentTrigger
    {
        internal readonly string Key;
        internal readonly string ViewName;
        internal readonly bool ViewEnd;
        internal readonly List<string> Properties;
        internal readonly bool CustomEvent;

        private ContentTrigger(string key, string viewName, bool viewEnd, List<string> properties, bool customEvent)
        {
            Key = key;
            ViewName = viewName;
            ViewEnd = viewEnd;
            Properties = properties;
            CustomEvent = customEvent;
        }

        /// <summary>A recorded event, by its key as the app passed it.</summary>
        internal static ContentTrigger Event(string key, bool customEvent) { return new ContentTrigger(key, null, false, null, customEvent); }

        /// <summary>A recorded view event, by its name as the app passed it, when the view starts or when it ends.</summary>
        internal static ContentTrigger View(string viewName, bool viewEnd) { return new ContentTrigger(ContentTriggers.KeyView, viewName, viewEnd, null, false); }

        /// <summary>The user properties a user details request sends: a user detail ("email"), or "custom." and the key of a custom property.</summary>
        internal static ContentTrigger UserProperties(List<string> properties) { return new ContentTrigger(ContentTriggers.KeyUserProperties, null, false, properties, false); }

        /// <summary>What the SDK records besides events: a session, an error or a consent change.</summary>
        internal static ContentTrigger Of(string key) { return new ContentTrigger(key, null, false, null, false); }

        internal bool IsView { get { return Key == ContentTriggers.KeyView; } }

        // the start and the end of a view keep separate gaps
        internal string GapKeySuffix { get { return IsView && ViewEnd ? ":end" : string.Empty; } }

        internal bool SameAs(ContentTrigger other)
        {
            return Key == other.Key && ViewName == other.ViewName && ViewEnd == other.ViewEnd && CustomEvent == other.CustomEvent
                && (Properties == null ? other.Properties == null : other.Properties != null && string.Join(",", Properties.ToArray()) == string.Join(",", other.Properties.ToArray()));
        }

        public override string ToString()
        {
            return Key + (ViewName != null ? " [" + ViewName + (ViewEnd ? ", end" : ", start") + "]" : string.Empty);
        }
    }

    /// <summary>A recorded trigger that starts a round: the triggers its gap is kept for, and the gap.</summary>
    internal class ContentTriggerMatch
    {
        internal readonly List<string> GapKeys;
        internal readonly long GapMs;

        internal ContentTriggerMatch(List<string> gapKeys, long gapMs)
        {
            GapKeys = gapKeys;
            GapMs = gapMs;
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
