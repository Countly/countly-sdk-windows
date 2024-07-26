using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.CountlyCommon.Server.Responses;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using static CountlySDK.CountlyCommon.Helpers.RequestHelper;
using static CountlySDK.Entities.EntityBase.DeviceBase;
using static CountlySDK.Helpers.TimeHelper;

namespace CountlySDK.CountlyCommon
{
    public abstract class CountlyBase
    {
        internal class IRequestHelperImpl : IRequestHelper
        {
            CountlyBase _base = null;
            internal IRequestHelperImpl(CountlyBase instance)
            {
                _base = instance;
            }

            public string GetAppKey()
            {
                return _base.Configuration.appKey;
            }

            public string GetSDKName()
            {
                return _base.sdkName();
            }

            public string GetSDKVersion() { return sdkVersion; }

            async Task<DeviceId> IRequestHelper.GetDeviceId()
            {
                DeviceId deviceId = await _base.DeviceData.GetDeviceId();
                return deviceId;
            }

            public TimeInstant GetTimeInstant() { return _base.timeHelper.GetUniqueInstant(); }

            public string GetAppVersion()
            {
                return _base.AppVersion;
            }
        }

        // Current version of the Count.ly SDK as a displayable string.
        protected const string sdkVersion = "24.1.1";

        public enum LogLevel { VERBOSE, DEBUG, INFO, WARNING, ERROR };

        internal CountlyConfig Configuration;
        internal ModuleBackendMode moduleBackendMode;

        public abstract string sdkName();

        // How often update session is sent
        protected int sessionUpdateInterval = 60;

        // Server url provided by a user
        protected string ServerUrl = null;

        // Application key provided by a user
        protected string AppKey = null;

        // Application version provided by a user
        protected string AppVersion = null;

        // Indicates sync process with a server
        internal bool uploadInProgress = false;

        internal TimeHelper timeHelper;
        internal RequestHelper requestHelper;

        internal readonly IDictionary<string, DateTime> TimedEvents = new Dictionary<string, DateTime>();

        //if stored event/sesstion/exception upload should be defered to a later time
        //if set to true, upload will not happen, but will just return "true"
        //data will still be saved in their respective files
        internal bool deferUpload = false;

        // File that stores events objects
        internal const string eventsFilename = "events.xml";
        // File that stores sessions objects
        internal const string sessionsFilename = "sessions.xml";
        // File that stores exceptions objects
        internal const string exceptionsFilename = "exceptions.xml";
        // File that stores temporary stored unhandled exception objects (currently used only for the windows target)
        internal const string unhandledExceptionFilename = "unhandled_exceptions.xml";
        // File that stores user details object
        internal const string userDetailsFilename = "userdetails.xml";
        // File that stores unsent stored requests
        internal const string storedRequestsFilename = "storedRequests.xml";

        // Events queue
        internal List<CountlyEvent> Events { get; set; }

        // Session queue
        internal List<SessionEvent> Sessions { get; set; }

        // Exceptions queue
        internal List<ExceptionEvent> Exceptions { get; set; }

        private static CountlyUserDetails userDetails;
        // User details info
        public static CountlyUserDetails UserDetails
        {
            get {
                lock (Countly.Instance.sync) {
                    if (userDetails == null) {
                        userDetails = Storage.Instance.LoadFromFile<CountlyUserDetails>(userDetailsFilename).Result;

                        if (userDetails == null) {
                            userDetails = new CountlyUserDetails();
                        } else {
                            userDetails.isNotificationEnabled = true;
                        }

                        userDetails.UserDetailsChanged += Countly.Instance.OnUserDetailsChanged;
                    }
                }
                return userDetails;
            }
        }

        // StoredRequests queue
        internal Queue<StoredRequest> StoredRequests { get; set; }

        // Used for thread-safe operations
        protected object sync = new object();

        internal readonly Queue<string> CrashBreadcrumbs = new Queue<string>();

        // Start session timestamp
        protected DateTime startTime;

        // When the last session update was sent
        internal DateTime lastSessionUpdateTime;

        // If this is true, then a "begin session" has been called and there hasn't been an "end session"
        // this would indicate that we have to resume session tracking once consent is given
        internal bool automaticSessionTrackingStarted = false;

        //holds device info
        internal Device DeviceData = new Device();

        /// <summary>
        /// Determines if Countly debug messages are displayed to Output window
        /// </summary>
        public static bool IsLoggingEnabled { get; set; }

        /// <summary>
        /// Saves events to the storage
        /// </summary>
        /// <returns>True if success, otherwise - False</returns>
        protected abstract bool SaveEvents();

        /// <summary>
        /// Saves sessions to the storage
        /// </summary>
        /// <returns>True if success, otherwise - False</returns>
        protected abstract bool SaveSessions();

        /// <summary>
        /// Saves exceptions to the storage
        /// </summary>
        protected abstract bool SaveExceptions();

        /// <summary>
        /// Saves the given unhandled exception to storage
        /// </summary>
        internal abstract bool SaveUnhandledException(ExceptionEvent exceptionEvent);

        /// <summary>
        /// Saves user details info to the storage
        /// </summary>
        protected abstract bool SaveUserDetails();

        internal bool consentRequired = false;
        internal Dictionary<ConsentFeatures, bool> givenConsent = new Dictionary<ConsentFeatures, bool>();

        internal enum ConsentChangedAction
        {
            Initialization,
            ConsentUpdated,
            DeviceIDChangedNotMerged,
        }
        public enum ConsentFeatures
        {
            Sessions, Events, Location, Crashes, Users, Views, Push, Feedback, StarRating, RemoteConfig
        };

        internal abstract Metrics GetSessionMetrics();
        internal abstract void InformSessionEvent();

        internal async Task<bool> SaveStoredRequests()
        {
            lock (sync) {
                return Storage.Instance.SaveToFile<Queue<StoredRequest>>(storedRequestsFilename, StoredRequests).Result;
            }
        }

        protected async Task UpdateSessionInternal(int? elapsedTime = null)
        {
            UtilityHelper.CountlyLogging("[CountlyBase] Session Update happening'");
            if (Configuration.backendMode) {
                moduleBackendMode.OnTimer();
                Upload();
                return;
            }

            if (elapsedTime == null) {
                //calculate elapsed time from the last time update was sent (includes manual calls)
                elapsedTime = (int)DateTime.Now.Subtract(lastSessionUpdateTime).TotalSeconds;
            }
            UtilityHelper.CountlyLogging("Session Update elapsed time: [" + elapsedTime + "]");

            Debug.Assert(elapsedTime != null);
            lastSessionUpdateTime = DateTime.Now;

            Dictionary<string, object> requestParams =
               new Dictionary<string, object>();

            requestParams.Add("session_duration", elapsedTime.Value);
            string request = await requestHelper.BuildRequest(requestParams);
            await AddRequest(request);
            await Upload();
        }

        protected async Task EndSessionInternal()
        {
            if (Configuration.backendMode) {
                UtilityHelper.CountlyLogging("[CountlyBase] SessionEnd, Backend Mode enabled, returning");
                return;
            }
            UtilityHelper.CountlyLogging("[CountlyBase] EndSessionInternal'");

            //report the duration of current view
            reportViewDuration();

            SessionTimerStop();
            double elapsedTime = DateTime.Now.Subtract(lastSessionUpdateTime).TotalSeconds;


            if (elapsedTime > int.MaxValue) {
                UtilityHelper.CountlyLogging("[EndSessionInternal] about to be reported duration exceed max data type size. Setting to 0", LogLevel.ERROR);
                //assume something has gone wrong
                elapsedTime = 0;
            }

            int elapsedTimeSeconds = (int)elapsedTime;

            if (elapsedTimeSeconds < 0) {
                UtilityHelper.CountlyLogging("[EndSessionInternal] session duration is about to be reported as negative. Setting to 0", LogLevel.ERROR);
                elapsedTimeSeconds = 0;
            }

            Dictionary<string, object> requestParams = new Dictionary<string, object>();

            requestParams.Add("end_session", 1);
            requestParams.Add("session_duration", elapsedTimeSeconds);
            string request = await requestHelper.BuildRequest(requestParams);
            await AddRequest(request);
            await Upload();
        }

        /// <summary>
        /// Upload sessions, events & exception queues
        /// </summary>
        /// <returns>True if success</returns>
        internal async Task<bool> Upload()
        {
            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'Upload'");
            bool success = false;
            bool shouldContinue = false;

            do {
                if (deferUpload) {
                    return true;
                }

                success = await UploadSessions();

                if (success) {
                    success = await UploadEvents();
                }

                if (success) {
                    success = await UploadExceptions();
                }

                if (success) {
                    success = await UploadUserDetails();
                }

                if (success) {
                    success = await UploadStoredRequests();
                }

                if (success && !uploadInProgress) {
                    int sC, exC, evC, rC;
                    bool isChanged;

                    lock (sync) {
                        sC = Sessions.Count;
                        exC = Exceptions.Count;
                        evC = Events.Count;
                        rC = StoredRequests.Count;
                        isChanged = UserDetails.isChanged;
                    }

                    UtilityHelper.CountlyLogging("[CountlyBase] Upload, after one loop, " + sC + " " + exC + " " + evC + " " + rC + " " + isChanged);

                    if (sC > 0 || exC > 0 || evC > 0 || rC > 0 || isChanged) {
                        //work still needs to be done
                        return await Upload();
                    }
                } else {
                    UtilityHelper.CountlyLogging("[CountlyBase] Upload, after one loop, in progress");
                }
            } while (success && shouldContinue);


            return success;
        }

        private async Task<bool> UploadStoredRequests()
        {
            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'UploadStoredRequests'");
            StoredRequest sr = null;

            lock (sync) {
                if (uploadInProgress) {
                    return true;
                }
                uploadInProgress = true;

                if (StoredRequests.Count > 0) {
                    sr = StoredRequests.Peek();
                    Debug.Assert(sr != null);
                }
            }

            if (sr != null) {
                RequestResult requestResult = await Api.Instance.SendStoredRequest(ServerUrl, sr, GetRemainingRequestCount());

                if (requestResult != null && requestResult.IsSuccess()) {
                    //if it's a successful or bad request, remove it from the queue
                    lock (sync) {
                        uploadInProgress = false;

                        //remove the executed request
                        StoredRequest srd = null;
                        try {
                            srd = StoredRequests.Dequeue();
                        } catch {
                            UtilityHelper.CountlyLogging("[CountlyBase] UploadStoredRequests, failing 'StoredRequests.Dequeue()'");
                        }
                        Debug.Assert(srd != null);
                        if (srd != sr) {
                            UtilityHelper.CountlyLogging("[CountlyBase] UploadStoredRequests, the head request is not equal to sent request", LogLevel.ERROR);

                        }

                        if (!Configuration.backendMode) {
                            bool success = SaveStoredRequests().Result;//todo, handle this in the future
                        }

                    }
                    return true;
                } else {
                    lock (sync) { uploadInProgress = false; }
                    return false;
                }
            } else {
                lock (sync) { uploadInProgress = false; }
                return true;
            }
        }

        /// <summary>
        /// Uploads sessions queue to Countly server
        /// </summary>
        /// <returns></returns>
        private async Task<bool> UploadSessions()
        {
            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'UploadSessions'");
            SessionEvent sessionEvent = null;

            lock (sync) {
                if (uploadInProgress) {
                    return true;
                }
                uploadInProgress = true;

                if (Sessions.Count > 0) {
                    sessionEvent = Sessions[0];
                }
            }

            if (sessionEvent != null) {
                RequestResult requestResult = await Api.Instance.SendSession(ServerUrl, GetRemainingRequestCount(), sessionEvent, (UserDetails.isChanged) ? UserDetails : null);

                if (requestResult != null && requestResult.IsSuccess()) {
                    //if it's a successful or bad request, remove it from the queue
                    lock (sync) {
                        UserDetails.isChanged = false;
                    }

                    if (!Configuration.backendMode) {
                        SaveUserDetails();
                    }

                    lock (sync) {
                        uploadInProgress = false;

                        try {
                            Sessions.RemoveAt(0);
                        } catch (Exception ex) {
                            UtilityHelper.CountlyLogging("[UploadSessions] Failed at removing session." + ex.ToString());
                        }

                        if (!Configuration.backendMode) {
                            bool success = SaveSessions();//todo, handle this in the future

                        }
                    }

                    int sessionCount = 0;
                    lock (sync) {
                        sessionCount = Sessions.Count;
                    }

                    if (sessionCount > 0) {
                        return await UploadSessions();
                    } else {
                        return true;
                    }
                } else {
                    uploadInProgress = false;
                    return false;
                }
            } else {
                uploadInProgress = false;
                return true;
            }
        }

        /// <summary>
        /// Start a timed event.
        /// </summary>
        /// <param name="key">event key</param>
        /// <returns></returns>
        public void StartEvent(string key)
        {
            if (Configuration.backendMode) {
                UtilityHelper.CountlyLogging("[CountlyBase] StartEvent, Backend Mode enabled, returning");
                return;
            }

            UtilityHelper.CountlyLogging("[CountlyBase] StartEvent : key = " + key);

            if (!Countly.Instance.IsInitialized()) {
                UtilityHelper.CountlyLogging("[CountlyBase] StartEvent: SDK must initialized before calling 'StartEvent'");
                return;
            }


            if (!IsConsentGiven(ConsentFeatures.Events)) {
                return;
            }

            if (string.IsNullOrEmpty(key) || key == " ") {
                UtilityHelper.CountlyLogging("[CountlyBase] StartEvent : The event key '" + key + "' isn't valid.");
                return;
            }

            if (TimedEvents.ContainsKey(key)) {
                UtilityHelper.CountlyLogging("[CountlyBase] StartEvent : Event with key '" + key + "' has already started.");
                return;
            }

            TimedEvents.Add(key, DateTime.Now);

        }

        /// <summary>
        /// Cancel a timed event.
        /// </summary>
        /// <param name="key">event key</param>
        /// <returns></returns>
        public void CancelEvent(string key)
        {
            if (Configuration.backendMode) {
                UtilityHelper.CountlyLogging("[CountlyBase] CancelEvent, Backend Mode enabled, returning");
                return;
            }

            UtilityHelper.CountlyLogging("[CountlyBase] CancelEvent : key = " + key);

            if (!IsConsentGiven(ConsentFeatures.Events)) {
                return;
            }

            if (string.IsNullOrEmpty(key) || key == " ") {
                UtilityHelper.CountlyLogging("[CountlyBase] CancelEvent : The event key '" + key + "' isn't valid.");
                return;
            }

            if (!TimedEvents.ContainsKey(key)) {
                UtilityHelper.CountlyLogging("[CountlyBase] CancelEvent : Time event with key '" + key + "' doesn't exist.");
                return;
            }

            TimedEvents.Remove(key);
        }

        /// <summary>
        /// Add all recorded events to request queue
        /// </summary>
        private void CancelAllTimedEvents()
        {
            TimedEvents.Clear();
        }

        /// <summary>
        /// End a timed event.
        /// </summary>
        /// <param name="key">event key</param>
        /// <param name="segmentation">custom segmentation you want to set, leave null if you don't want to add anything</param>
        /// <param name="count">how many of these events have occurred, default value is "1"</param>
        /// <param name="sum">set sum if needed, default value is "0"</param>
        /// <returns></returns>
        public async Task EndEvent(string key, Segmentation segmentation = null, int count = 1, double? sum = 0)
        {
            if (Configuration.backendMode) {
                UtilityHelper.CountlyLogging("[CountlyBase] EndEvent, Backend Mode enabled, returning");
                return;
            }

            UtilityHelper.CountlyLogging("[CountlyBase] EndEvent : key = " + key + ", segmentation = " + segmentation + ", count = " + count + ", sum = " + sum);

            if (!IsConsentGiven(ConsentFeatures.Events)) {
                return;
            }

            if (string.IsNullOrEmpty(key) || key == " ") {
                UtilityHelper.CountlyLogging("[CountlyBase] EndEvent : The event key '" + key + "' isn't valid.");
                return;
            }

            if (!TimedEvents.ContainsKey(key)) {
                UtilityHelper.CountlyLogging("[CountlyBase] EndEvent : Time event with key '" + key + "' doesn't exist.");
                return;
            }

            DateTime startTime = TimedEvents[key];
            double duration = (DateTime.Now - startTime).TotalSeconds;

            await Countly.RecordEvent(key, count, sum, duration, segmentation);

            TimedEvents.Remove(key);
        }

        /// <summary>
        /// Records a custom event with no segmentation values, a count of one and a sum of zero
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        public static Task<bool> RecordEvent(string Key)
        {
            UtilityHelper.CountlyLogging("[CountlyBase] RecordEvent(k)");
            return Countly.RecordEvent(Key, 1, null, null, null);
        }

        /// <summary>
        /// Records a custom event with no segmentation values, the specified count, and a sum of zero.
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        public static Task<bool> RecordEvent(string Key, int Count)
        {
            UtilityHelper.CountlyLogging("[CountlyBase] RecordEvent(kc)");
            return Countly.RecordEvent(Key, Count, null, null, null);
        }

        /// <summary>
        /// Records a custom event with no segmentation values, and the specified count and sum.
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <param name="Sum">Sum to associate with the event</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        public static Task<bool> RecordEvent(string Key, int Count, double? Sum)
        {
            UtilityHelper.CountlyLogging("[CountlyBase] RecordEvent(kcs)");
            return Countly.RecordEvent(Key, Count, Sum, null, null);
        }

        /// <summary>
        /// Records a custom event with the specified segmentation values and count, and a sum of zero.
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <param name="Segmentation">Segmentation object to associate with the event, can be null</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        public static Task<bool> RecordEvent(string Key, int Count, Segmentation Segmentation)
        {
            UtilityHelper.CountlyLogging("[CountlyBase] RecordEvent(kcS)");
            return Countly.RecordEvent(Key, Count, null, null, Segmentation);
        }

        /// <summary>
        /// Records a custom event with the specified segmentation values, count and a sum
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <param name="Sum">Sum to associate with the event</param>
        /// <param name="Segmentation">Segmentation object to associate with the event, can be null</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        public static Task<bool> RecordEvent(string Key, int Count, double? Sum, Segmentation Segmentation)
        {
            UtilityHelper.CountlyLogging("[CountlyBase] RecordEvent(kcsS)");
            return Countly.RecordEvent(Key, Count, Sum, null, Segmentation);
        }

        /// <summary>
        /// Records a custom event with the specified segmentation values, count and a sum
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <param name="Sum">Sum to associate with the event</param>
        /// <param name="Duration">Event duration</param>
        /// <param name="Segmentation">Segmentation object to associate with the event, can be null</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        public static Task<bool> RecordEvent(string Key, int Count, double? Sum, double? Duration, Segmentation Segmentation)
        {
            if (Countly.Instance.Configuration.backendMode) {
                UtilityHelper.CountlyLogging("[CountlyBase] RecordEvent, Backend Mode enabled, returning false");
                return Task.Factory.StartNew(() => { return false; });
            }

            if (!Countly.Instance.IsInitialized()) {
                UtilityHelper.CountlyLogging("SDK must initialized before calling 'RecordEvent'");
                return Task.Factory.StartNew(() => { return false; });
            }

            CountlyConfig config = Countly.Instance.Configuration;
            if (Key.Length > config.MaxKeyLength) {
                UtilityHelper.CountlyLogging("[CountlyBase] RecordEvent : Max allowed key length is " + Countly.Instance.Configuration.MaxKeyLength);
                Key = Key.Substring(0, Countly.Instance.Configuration.MaxKeyLength);
            }

            Segmentation segments = UtilityHelper.RemoveExtraSegments(Segmentation, config.MaxSegmentationValues);
            segments = UtilityHelper.FixSegmentKeysAndValues(segments, config.MaxKeyLength, config.MaxValueSize);
            return Countly.Instance.RecordEventInternal(Key, Count, Sum, Duration, segments);
        }

        /// <summary>
        /// Records a custom event with the specified values
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <param name="Sum">Sum to associate with the event</param>
        /// <param name="Segmentation">Segmentation object to associate with the event, can be null</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        protected async Task<bool> RecordEventInternal(string Key, int Count, double? Sum, double? Duration, Segmentation Segmentation)
        {
            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'RecordEventInternal'");
            if (!Countly.Instance.IsServerURLCorrect(ServerUrl)) { return false; }
            if (!CheckConsentOnKey(Key)) { return true; }

            TimeInstant timeInstant = timeHelper.GetUniqueInstant();
            CountlyEvent cEvent = new CountlyEvent(Key, Count, Sum, Duration, Segmentation, timeInstant.Timestamp);

            bool saveSuccess = false;
            lock (sync) {
                Events.Add(cEvent);
                saveSuccess = SaveEvents();
            }

            if (saveSuccess) {
                //todo rework this
                saveSuccess = await Upload();
            }

            return saveSuccess;
        }

        private bool CheckConsentOnKey(string key)
        {
            if (key.Equals(VIEW_EVENT_KEY)) {
                return IsConsentGiven(ConsentFeatures.Views);
            } else if (key.Equals(NPS_EVENT_KEY)) {
                return IsConsentGiven(ConsentFeatures.Feedback);
            } else if (key.Equals(SURVEY_EVENT_KEY)) {
                return IsConsentGiven(ConsentFeatures.Feedback);
            } else if (key.Equals(STAR_RATING_EVENT_KEY)) {
                return IsConsentGiven(ConsentFeatures.StarRating);
            } else if (key.Equals(PUSH_ACTION_EVENT_KEY)) {
                return IsConsentGiven(ConsentFeatures.Push);
            } else if (key.Equals(ORIENTATION_EVENT_KEY)) {
                return IsConsentGiven(ConsentFeatures.Users);
            } else {
                return IsConsentGiven(ConsentFeatures.Events);
            }

        }

        private int GetRemainingRequestCount()
        {
            int sC, exC, evC, rC, uC;

            lock (sync) {
                sC = Sessions.Count;
                exC = Exceptions.Count;
                evC = (int)Math.Floor((double)(Events.Count / 15)); // in UploadEvents() function Math.Min is used to send events by 15 chunks
                rC = StoredRequests.Count;
                uC = UserDetails.isChanged ? 1 : 0;
            }

            return Math.Max(sC + exC + evC + rC + uC - 1, 0);
        }

        /// <summary>
        /// Uploads event queue to Countly server
        /// </summary>
        /// <returns>True if success</returns>
        private async Task<bool> UploadEvents()
        {
            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'UploadEvents'");
            lock (sync) {
                // Allow uploading in one thread only
                if (uploadInProgress) {
                    return true;
                }

                uploadInProgress = true;
            }

            int eventsCount;

            lock (sync) {
                eventsCount = Math.Min(15, Events.Count);
            }

            if (eventsCount > 0) {
                List<CountlyEvent> eventsToSend = null;
                lock (sync) {
                    eventsToSend = Events.Take(eventsCount).ToList();
                }

                TimeInstant timeInstant = timeHelper.GetUniqueInstant();
                RequestResult requestResult = await Api.Instance.SendEvents(ServerUrl, requestHelper, GetRemainingRequestCount(), eventsToSend, (UserDetails.isChanged) ? UserDetails : null);

                if (requestResult != null && requestResult.IsSuccess()) {
                    //if it's a successful or bad request, remove it from the queue
                    int eventsCountToUploadAgain = 0;

                    UserDetails.isChanged = false;

                    if (!Configuration.backendMode) {
                        SaveUserDetails();
                    }

                    lock (sync) {
                        uploadInProgress = false;

                        try {
                            for (int i = eventsCount - 1; i >= 0; i--) {
                                Events.RemoveAt(i);
                            }
                        } catch (Exception ex) {
                            UtilityHelper.CountlyLogging("[UploadEvents] Failed at removing events." + ex.ToString());
                        }

                        if (!Configuration.backendMode) {
                            bool success = SaveEvents();//todo, react to this in the future

                        }
                        eventsCountToUploadAgain = Events.Count;
                    }

                    if (eventsCountToUploadAgain > 0) {
                        // Upload events added during sync
                        return await UploadEvents();
                    } else {
                        return true;
                    }
                } else {
                    uploadInProgress = false;
                    return false;
                }
            } else {
                uploadInProgress = false;
                return true;
            }
        }

        /// <summary>
        /// Records exception
        /// </summary>
        /// <param name="error">exception title</param>
        /// <returns>True if exception successfully uploaded, False - queued for delayed upload</returns>
        public static async Task<bool> RecordException(string error)
        {
            if (!Countly.Instance.IsInitialized()) {
                UtilityHelper.CountlyLogging("[CountlyBase] RecordException: SDK must initialized before calling 'RecordException(error)'");
                return false;
            }

            return await RecordException(error, null, null);
        }

        /// <summary>
        /// Records exception with stacktrace
        /// </summary>
        /// <param name="error">exception title</param>
        /// <param name="stackTrace">exception stacktrace</param>
        /// <returns>True if exception successfully uploaded, False - queued for delayed upload</returns>
        public static async Task<bool> RecordException(string error, string stackTrace = null)
        {
            if (!Countly.Instance.IsInitialized()) {
                UtilityHelper.CountlyLogging("[CountlyBase] RecordException: SDK must initialized before calling 'RecordExceptionRecordException(error, stackTrace)'");
                return false;
            }

            return await RecordException(error, stackTrace, null);
        }

        /// <summary>
        /// Records unhandled exception with stacktrace
        /// </summary>
        /// <param name="error">exception title</param>
        /// <param name="stackTrace">exception stacktrace</param>
        protected async Task<bool> RecordUnhandledException(string error, string stackTrace)
        {
            if (!Countly.Instance.IsInitialized()) {
                UtilityHelper.CountlyLogging("[CountlyBase] RecordException: SDK must initialized before calling 'RecordException(error, stackTrace)'");
                return false;
            }

            return await RecordException(error, stackTrace, null, true);
        }

        /// <summary>
        /// Records exception with stacktrace and custom info
        /// </summary>
        /// <param name="error">exception title</param>
        /// <param name="stackTrace">exception stacktrace</param>
        /// <param name="customInfo">exception custom info</param>
        /// <returns>True if exception successfully uploaded, False - queued for delayed upload</returns>
        public static async Task<bool> RecordException(string error, string stackTrace, Dictionary<string, string> customInfo)
        {
            if (!Countly.Instance.IsInitialized()) {
                UtilityHelper.CountlyLogging("[CountlyBase] RecordException: SDK must initialized before calling 'RecordException(error, stackTrace, customInfo)'");
                return false;
            }

            return await RecordException(error, stackTrace, customInfo, false);
        }

        /// <summary>
        /// Records exception with stacktrace and custom info
        /// </summary>
        /// <param name="error">exception title</param>
        /// <param name="stackTrace">exception stacktrace</param>
        /// <param name="customInfo">exception custom info</param>
        /// <param name="unhandled">bool indicates is exception is fatal or not</param>
        /// <returns>True if exception successfully uploaded, False - queued for delayed upload</returns>
        public static async Task<bool> RecordException(string error, string stackTrace, Dictionary<string, string> customInfo, bool unhandled)
        {
            if (Countly.Instance.Configuration.backendMode) {
                UtilityHelper.CountlyLogging("[CountlyBase] RecordException, Backend Mode enabled, returning false");
                return false;
            }

            if (!Countly.Instance.IsInitialized()) {
                UtilityHelper.CountlyLogging("[CountlyBase] RecordException: SDK must initialized before calling 'RecordException(error, stackTrace, customInfo, unhandled)'");
                return false;
            }

            return await Countly.Instance.RecordExceptionInternal(error, stackTrace, customInfo, unhandled);
        }

        /// <summary>
        /// Records exception with stacktrace and custom info
        /// </summary>
        /// <param name="error">exception title</param>
        /// <param name="stackTrace">exception stacktrace</param>
        /// <param name="customInfo">exception custom info</param>
        /// <param name="unhandled">bool indicates is exception is fatal or not</param>
        /// <returns>True if exception successfully uploaded, False - queued for delayed upload</returns>
        internal async Task<bool> RecordExceptionInternal(string error, string stackTrace, Dictionary<string, string> customInfo, bool unhandled)
        {
            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'RecordException'");
            if (!IsServerURLCorrect(ServerUrl)) { return false; }
            if (!IsConsentGiven(ConsentFeatures.Crashes)) { return true; }

            TimeSpan run = DateTime.Now.Subtract(startTime);

            CountlyConfig config = Countly.Instance.Configuration;
            Dictionary<string, string> segmentation = UtilityHelper.RemoveExtraSegments(customInfo, config.MaxSegmentationValues);
            segmentation = UtilityHelper.FixSegmentKeysAndValues(segmentation, config.MaxKeyLength, config.MaxValueSize);

            ExceptionEvent eEvent = new ExceptionEvent(error, UtilityHelper.ManipulateStackTrace(stackTrace, Configuration.MaxStackTraceLinesPerThread, Configuration.MaxStackTraceLineLength) ?? string.Empty, unhandled, string.Join("\n", CrashBreadcrumbs.ToArray()), run, AppVersion, segmentation, DeviceData);

            if (!unhandled) {
                bool saveSuccess = false;
                lock (sync) {
                    Exceptions.Add(eEvent);
                    saveSuccess = SaveExceptions();
                }

                if (saveSuccess) {
                    return await Upload();
                }

                return false;
            } else {
                //since it's unhandled, we assume that the app is gonna crash soon
                //only save the exception and upload it later
                SaveUnhandledException(eEvent);
                return false;
            }
        }

        /// <summary>
        /// Uploads exceptions queue to Countly server
        /// </summary>
        /// <returns>True if success</returns>
        protected async Task<bool> UploadExceptions()
        {
            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'UploadExceptions'");
            lock (sync) {
                // Allow uploading in one thread only
                if (uploadInProgress) {
                    return true;
                }

                uploadInProgress = true;
            }

            int exceptionsCount;//how many exceptions are stored

            lock (sync) {
                exceptionsCount = Exceptions.Count;
            }

            //if there is at least one exception stored, do the upload
            if (exceptionsCount > 0) {
                ExceptionEvent exEvent;//the exception event that will be uploaded
                lock (sync) {
                    exEvent = Exceptions[0];
                }

                //do the exception upload
                TimeInstant timeInstant = timeHelper.GetUniqueInstant();
                RequestResult requestResult = await Api.Instance.SendException(ServerUrl, requestHelper, GetRemainingRequestCount(), exEvent);

                //check if we got a response and that it was a success
                if (requestResult != null && requestResult.IsSuccess()) {
                    //if it's a successful or bad request, remove it from the queue
                    int exceptionsCountToUploadAgain = 0;

                    lock (sync) {
                        try {
                            Exceptions.RemoveAt(0);
                        } catch (Exception ex) {
                            UtilityHelper.CountlyLogging("[UploadExceptions] thrown exception when removing entry, " + ex.ToString());
                        }

                        if (!Configuration.backendMode) {
                            SaveExceptions();//todo, in the future, react to this failing

                        }

                        exceptionsCountToUploadAgain = Exceptions.Count;
                        uploadInProgress = false;//mark that we have stoped upload
                    }

                    if (exceptionsCountToUploadAgain > 0) {
                        // Upload next exception
                        return await UploadExceptions();
                    } else {
                        //no exceptions left to upload
                        return true;
                    }
                } else {
                    //if the received response was not a success
                    uploadInProgress = false;
                    return false;
                }
            } else {
                //if there are no exceptions to upload
                uploadInProgress = false;
                return true;
            }
        }

        /// <summary>
        /// Uploads user details
        /// </summary>
        /// <returns>true if details are successfully uploaded, false otherwise</returns>
        internal async Task<bool> UploadUserDetails()
        {
            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'UploadUserDetails'");
            if (!IsServerURLCorrect(ServerUrl)) { return false; }
            if (!IsConsentGiven(ConsentFeatures.Users)) { return true; }

            lock (sync) {
                //upload only when needed
                if (!UserDetails.isChanged) {
                    return true;
                }

                // Allow uploading in one thread only
                if (uploadInProgress) {
                    return true;
                }

                uploadInProgress = true;
            }

            TimeInstant timeInstant = timeHelper.GetUniqueInstant();
            RequestResult requestResult = await Api.Instance.UploadUserDetails(ServerUrl, requestHelper, GetRemainingRequestCount(), UserDetails);

            lock (sync) {
                uploadInProgress = false;
            }

            if (requestResult != null && requestResult.IsSuccess()) {
                //if it's a successful or bad request, remove it from the queue              
                UserDetails.isChanged = false;

                if (!Configuration.backendMode) {
                    SaveUserDetails();

                }

                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// Raised when user details property is changed
        /// </summary>
        protected async void OnUserDetailsChanged()
        {
            UserDetails.isChanged = true;
            UserDetails.isNotificationEnabled = false;

            UserDetails.Picture = UtilityHelper.TrimUrl(UserDetails.Picture);
            UserDetails.Name = UtilityHelper.TrimValue("Name", UserDetails.Name, Configuration.MaxValueSize);
            UserDetails.Email = UtilityHelper.TrimValue("Email", UserDetails.Email, Configuration.MaxValueSize);
            UserDetails.Phone = UtilityHelper.TrimValue("Phone", UserDetails.Phone, Configuration.MaxValueSize);
            UserDetails.Gender = UtilityHelper.TrimValue("Gender", UserDetails.Gender, Configuration.MaxValueSize);
            UserDetails.Username = UtilityHelper.TrimValue("Username", UserDetails.Username, Configuration.MaxValueSize);
            UserDetails.Organization = UtilityHelper.TrimValue("Organization", UserDetails.Organization, Configuration.MaxValueSize);

            UserDetails._custom = UtilityHelper.FixSegmentKeysAndValues(UserDetails._custom, Configuration.MaxKeyLength, Configuration.MaxValueSize);

            UserDetails.isNotificationEnabled = true;

            if (!Configuration.backendMode) {
                SaveUserDetails();
            }

            await Upload();
        }

        /// <summary>
        /// Uploads user picture. Accepted picture formats are .png, .gif and .jpeg and picture will be resized to maximal 150x150 dimensions
        /// </summary>
        /// <param name="imageStream">Image stream</param>
        /// <returns>true if image is successfully uploaded, false otherwise</returns>
        internal async Task<bool> UploadUserPicture(Stream imageStream)
        {
            if (!IsServerURLCorrect(ServerUrl)) {
                return false;
            }

            TimeInstant timeInstant = timeHelper.GetUniqueInstant();
            RequestResult requestResult = await Api.Instance.UploadUserPicture(ServerUrl, requestHelper, GetRemainingRequestCount(), imageStream, (UserDetails.isChanged) ? UserDetails : null);

            return (requestResult != null && requestResult.IsSuccess());
        }

        /// <summary>
        /// Immediately disables session, event, exceptions & user details tracking and clears any stored sessions, events, exceptions & user details data.
        /// This API is useful if your app has a tracking opt-out switch, and you want to immediately
        /// disable tracking when a user opts out. Call StartSession to enable logging again
        /// </summary>
        public static async void Halt()
        {
            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'Halt'");
            await Countly.Instance.HaltInternal();
        }

        /// <summary>
        /// This should clear all internal state
        /// </summary>
        /// <param name="clearStorage"></param>
        /// <returns></returns>
        internal async Task HaltInternal(bool clearStorage = true)
        {
            lock (sync) {
                SessionTimerStop();

                TimedEvents.Clear();
                Events?.Clear();
                Sessions?.Clear();
                Exceptions?.Clear();
                CrashBreadcrumbs.Clear();
                DeviceData = new Device();
                StoredRequests?.Clear();

                if (UserDetails != null) {
                    UserDetails.UserDetailsChanged -= OnUserDetailsChanged;
                }
                userDetails = null;//set it null so that it can be loaded from the file system (if needed)

                consentRequired = false;
                givenConsent.Clear();

                ServerUrl = null;
                AppKey = null;

                sessionUpdateInterval = 60;
                AppVersion = null;
                uploadInProgress = false;

                //clear session things
                startTime = new DateTime();
                lastSessionUpdateTime = new DateTime();
                automaticSessionTrackingStarted = false;

                //clear view related things
                lastView = null;
                lastViewStart = 0;
                firstView = true;

                // modules
                moduleBackendMode = null;
            }
            if (clearStorage) {
                await ClearStorage();
            }
        }

        protected async Task ClearStorage()
        {
            await Storage.Instance.DeleteFile(eventsFilename);
            await Storage.Instance.DeleteFile(sessionsFilename);
            await Storage.Instance.DeleteFile(exceptionsFilename);
            await Storage.Instance.DeleteFile(userDetailsFilename);
            await Storage.Instance.DeleteFile(storedRequestsFilename);
            await Storage.Instance.DeleteFile(Device.deviceFilename);
        }

        /// <summary>
        /// Adds log breadcrumb
        /// </summary>
        /// <param name="log">log string</param>
        [Obsolete("'AddBreadCrumb' is deprecated, please use non static method 'AddCrashBreadCrumb' instead.")]
        public static void AddBreadCrumb(string log)
        {
            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'AddBreadCrumb'");
            Countly.Instance.AddCrashBreadCrumb(log);

        }

        /// <summary>
        /// Adds log breadcrumb
        /// </summary>
        /// <param name="log">log string</param>
        public void AddCrashBreadCrumb(string breadCrumb)
        {
            if (Configuration.backendMode) {
                UtilityHelper.CountlyLogging("[CountlyBase] AddCrashBreadCrumb, Backend Mode enabled, returning");
                return;
            }

            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'AddBreadCrumbs'");
            if (!Countly.Instance.IsInitialized()) {
                UtilityHelper.CountlyLogging("[CountlyBase] AddBreadCrumb: SDK must initialized before calling 'AddBreadCrumb'");
                return;
            }

            Debug.Assert(breadCrumb != null);
            string validLog = breadCrumb.Length > Countly.Instance.Configuration.MaxValueSize ? breadCrumb.Substring(0, Countly.Instance.Configuration.MaxValueSize) : breadCrumb;

            if (Countly.Instance.CrashBreadcrumbs.Count >= Countly.Instance.Configuration.MaxBreadcrumbCount) {
                Countly.Instance.CrashBreadcrumbs.Dequeue();
            }

            Countly.Instance.CrashBreadcrumbs.Enqueue(validLog);
        }

        public static async Task<string> GetDeviceId()
        {
            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'GetDeviceId'");
            if (!Countly.Instance.IsInitialized()) {
                UtilityHelper.CountlyLogging("[CountlyBase] GetDeviceId: SDK must initialized before calling 'GetDeviceId'");
                return string.Empty;
            }

            DeviceId did = await Countly.Instance.DeviceData.GetDeviceId();
            return did.deviceId;
        }

        protected bool IsServerURLCorrect(String url)
        {
            if (String.IsNullOrEmpty(url))//todo, in future replace with "String.IsNullOrWhiteSpace"
            {
                return false;
            }
            return true;
        }

        protected bool IsAppKeyCorrect(String appKey)
        {
            if (String.IsNullOrEmpty(appKey))//todo, in future replace with "String.IsNullOrWhiteSpace"
            {
                return false;
            }
            return true;
        }

        protected abstract void SessionTimerStart();
        protected abstract void SessionTimerStop();

        public async Task<bool> SetLocation(string gpsLocation, string ipAddress = null, string country_code = null, string city = null)
        {

            if (Configuration.backendMode) {
                UtilityHelper.CountlyLogging("[CountlyBase] SetLocation, Backend Mode enabled, returning false");
                return false;
            }

            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'SetLocation'");
            if (!IsInitialized()) {
                UtilityHelper.CountlyLogging("[CountlyBase] SetLocation: SDK must initialized before calling 'SetLocation'");
                return false;
            }

            if (!IsConsentGiven(ConsentFeatures.Location)) { return true; }

            Dictionary<string, object> requestParams =
                new Dictionary<string, object>();

            /*
             * Empty country code, city and IP address can not be sent.
             */

            if (!string.IsNullOrEmpty(ipAddress)) {
                requestParams.Add("ip_address", ipAddress);
            }

            if (!string.IsNullOrEmpty(country_code)) {
                requestParams.Add("country_code", country_code);
            }

            if (!string.IsNullOrEmpty(city)) {
                requestParams.Add("city", city);
            }

            if (!string.IsNullOrEmpty(gpsLocation)) {
                requestParams.Add("location", gpsLocation);
            }

            if (requestParams.Count > 0) {
                string request = await requestHelper.BuildRequest(requestParams);
                //add the request to queue and upload it
                await AddRequest(request);
                return await Upload();
            }

            return true;
        }

        protected Dictionary<string, object> GetLocationParams()
        {
            Dictionary<string, object> locationParams =
               new Dictionary<string, object>();

            /* If location is disabled or no location consent is given,
            the SDK adds an empty location entry to every "begin_session" request. */
            if (Configuration.IsLocationDisabled || !IsConsentGiven(ConsentFeatures.Location)) {
                locationParams.Add("location", string.Empty);
            } else {
                if (!string.IsNullOrEmpty(Configuration.IPAddress)) {
                    locationParams.Add("ip_address", Configuration.IPAddress);
                }

                if (!string.IsNullOrEmpty(Configuration.CountryCode)) {
                    locationParams.Add("country_code", Configuration.CountryCode);
                }

                if (!string.IsNullOrEmpty(Configuration.City)) {
                    locationParams.Add("city", Configuration.City);
                }

                if (!string.IsNullOrEmpty(Configuration.Location)) {
                    locationParams.Add("location", Configuration.Location);
                }
            }

            return locationParams;
        }

        /// <summary>
        /// Sends a request with an empty "location" parameter.
        /// </summary>
        internal async Task SendRequestWithEmptyLocation()
        {
            Dictionary<string, object> requestParams =
               new Dictionary<string, object> {
                   { "location", string.Empty }
               };

            string request = await requestHelper.BuildRequest(requestParams);
            await AddRequest(request);
        }

        public async Task<bool> DisableLocation()
        {
            if (Configuration.backendMode) {
                UtilityHelper.CountlyLogging("[CountlyBase] DisableLocation, Backend Mode enabled, returning false");
                return false;
            }

            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'DisableLocation'");
            if (!IsInitialized()) {
                UtilityHelper.CountlyLogging("[CountlyBase] DisableLocation: SDK must initialized before calling 'DisableLocation'");
                return false;
            }
            if (!IsConsentGiven(ConsentFeatures.Location)) { return true; }
            await SendRequestWithEmptyLocation();
            return true;
        }

        internal bool IsInitialized()
        {
            if (ServerUrl != null && AppKey != null) {
                return true;
            }
            return false;
        }

        internal async Task AddRequest(string networkRequest, bool isIdMerge = false)
        {
            Debug.Assert(networkRequest != null);

            if (networkRequest == null) { return; }

            lock (sync) {
                StoredRequest sr = new StoredRequest(networkRequest, isIdMerge);
                if (Configuration.backendMode) {
                    if (StoredRequests.Count >= Configuration.RequestQueueMaxSize) {
                        StoredRequests.Dequeue();
                    }
                }

                StoredRequests.Enqueue(sr);
                if (!Configuration.backendMode) {
                    SaveStoredRequests();
                } else {
                    UtilityHelper.CountlyLogging("[CountlyBase] AddRequest, Backend mode enabled, request storage disabled");
                }
            }
        }

        public abstract Task Init(CountlyConfig config);

        protected async Task InitBase(CountlyConfig config)
        {
            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'InitBase' on SDK flavor: " + sdkName());
            if (!IsServerURLCorrect(config.serverUrl)) {
                UtilityHelper.CountlyLogging("[CountlyBase] InitBase: Invalid server url!");
                return;
            }

            if (!IsAppKeyCorrect(config.appKey)) {
                UtilityHelper.CountlyLogging("[CountlyBase] InitBase: Invalid application key!");
                return;
            }

            if (config.sessionUpdateInterval <= 0) {
                UtilityHelper.CountlyLogging("[CountlyBase] InitBase: Session update interval can't be less than 1 second.");
                return;
            }

            if (config.sdkFolderName != null && config.sdkFolderName.Length > 0) {
                Storage.Instance.sdkFolder = config.sdkFolderName;
            } else {
                UtilityHelper.CountlyLogging("[CountlyBase] Provided SDK folder name can't be 'null' or empty string.");
            }

            timeHelper = new TimeHelper();
            IRequestHelperImpl exposed = new IRequestHelperImpl(this);
            requestHelper = new RequestHelper(exposed);

            //remove last backslash
            if (config.serverUrl.EndsWith("/")) {
                config.serverUrl = config.serverUrl.Substring(0, config.serverUrl.Length - 1);
            }

            Configuration = config;

            ServerUrl = config.serverUrl;
            AppKey = config.appKey;
            AppVersion = config.appVersion;
            sessionUpdateInterval = config.sessionUpdateInterval;

            if (config.developerProvidedDeviceId?.Length == 0) {
                UtilityHelper.CountlyLogging("[CountlyBase] InitBase: 'DeveloperProvidedDeviceId' cannot be empty string.");
                return;
            }

            await DeviceData.InitDeviceId((DeviceIdMethodInternal)config.deviceIdMethod, config.developerProvidedDeviceId);

            if (!Configuration.backendMode) {
                lock (sync) {
                    StoredRequests = Storage.Instance.LoadFromFile<Queue<StoredRequest>>(storedRequestsFilename).Result ?? new Queue<StoredRequest>();
                    Events = Storage.Instance.LoadFromFile<List<CountlyEvent>>(eventsFilename).Result ?? new List<CountlyEvent>();
                    Sessions = Storage.Instance.LoadFromFile<List<SessionEvent>>(sessionsFilename).Result ?? new List<SessionEvent>();
                    Exceptions = Storage.Instance.LoadFromFile<List<ExceptionEvent>>(exceptionsFilename).Result ?? new List<ExceptionEvent>();
                }
            } else {
                lock (sync) {
                    StoredRequests = new Queue<StoredRequest>();
                    Events = new List<CountlyEvent>();
                    Sessions = new List<SessionEvent>();
                    Exceptions = new List<ExceptionEvent>();
                }
            }

            //consent related
            consentRequired = config.consentRequired;
            if (config.givenConsent != null) {
                await SetConsentInternal(config.givenConsent, ConsentChangedAction.Initialization);
            }

            UtilityHelper.CountlyLogging("[CountlyBase] Finished 'InitBase'");

            await OnInitComplete();

            if (Configuration.backendMode) {
                moduleBackendMode = new ModuleBackendMode(this);
                SessionTimerStart();
            }
        }

        /// <summary>
        /// Run session startup logic and start timer with the specified interval
        /// </summary>
        internal async Task OnInitComplete()
        {
            if (!IsConsentGiven(ConsentFeatures.Sessions)) {
                /* If location is disabled in init
                and no session consent is given. Send empty location as separate request.*/
                if (Configuration.IsLocationDisabled || !IsConsentGiven(ConsentFeatures.Location)) {
                    await SendRequestWithEmptyLocation();
                } else {
                    /*
                 * If there is no session consent, 
                 * location values set in init should be sent as a separate location request.
                 */
                    await SetLocation(Configuration.Location, Configuration.IPAddress, Configuration.CountryCode, Configuration.City);
                }
            }
        }

        public enum DeviceIdType { DeveloperProvided = 0, SDKGenerated = 1 };

        /// <summary>
        /// Returns the device id type
        /// </summary>
        /// <returns>DeviceIdType</returns>
        public DeviceIdType GetDeviceIDType()
        {
            if (DeviceData.usedIdMethod == DeviceIdMethodInternal.developerSupplied) {
                return DeviceIdType.DeveloperProvided;
            } else {
                return DeviceIdType.SDKGenerated;
            }
        }

        /// <summary>
        /// Start tracking a session
        /// Should be called only once
        /// </summary>
        /// <returns></returns>
        public async Task SessionBegin()
        {
            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'SessionBegin'");
            if (!IsInitialized()) {
                UtilityHelper.CountlyLogging("[CountlyBase] SessionBegin: SDK must initialized before calling 'SessionBegin'");
                return;
            }

            if (Configuration.backendMode) {
                UtilityHelper.CountlyLogging("[CountlyBase] SessionBegin, Backend Mode enabled, returning");
                return;
            }

            automaticSessionTrackingStarted = true;
            startTime = DateTime.Now;
            lastSessionUpdateTime = startTime;
            SessionTimerStart();
            InformSessionEvent();

            Metrics metrics = GetSessionMetrics();

            // Adding location into session request
            Dictionary<string, object> requestParams = new Dictionary<string, object>(GetLocationParams()) {
                { "begin_session", 1 },
                { "metrics", metrics.ToString() }
            };

            string request = await requestHelper.BuildRequest(requestParams);
            await AddRequest(request);
            await Upload();
        }

        /// <summary>
        /// Manually update session
        /// </summary>
        /// <returns></returns>
        public async Task SessionUpdate(int elapsedTimeSeconds)
        {
            if (Configuration.backendMode) {
                UtilityHelper.CountlyLogging("[CountlyBase] SessionUpdate, Backend Mode enabled, returning");
                return;
            }

            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'SessionUpdate'");
            if (!IsInitialized()) {
                UtilityHelper.CountlyLogging("[CountlyBase] SessionUpdate: SDK must initialized before calling 'SessionUpdate'");
                return;
            }
            if (elapsedTimeSeconds < 0) {
                UtilityHelper.CountlyLogging("[CountlyBase] SessionUpdate: Elapsed time can not be negative");
                return;
            }

            await UpdateSessionInternal(elapsedTimeSeconds);
        }

        /// <summary>
        /// End tracking a session
        /// </summary>
        /// <returns></returns>
        public async Task SessionEnd()
        {
            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'SessionEnd'");
            if (!IsInitialized()) {
                UtilityHelper.CountlyLogging("[CountlyBase] SessionEnd: SDK must initialized before calling 'SessionEnd'");
                return;
            }

            await Countly.Instance.EndSessionInternal();
        }


        /// <summary>
        /// Change this devices Id
        /// </summary>
        /// <param name="newDeviceId">New Id that should be used</param>
        /// <param name="serverSideMerge">If set to true, old user id's data will be merged into new user</param>
        /// <returns></returns>
        public async Task ChangeDeviceId(string newDeviceId, bool serverSideMerge = false)
        {

            if (Configuration.backendMode) {
                UtilityHelper.CountlyLogging("[CountlyBase] ChangeDeviceId, Backend Mode enabled, returning");
                return;
            }

            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'ChangeDeviceId'");
            if (!IsInitialized()) {
                UtilityHelper.CountlyLogging("[CountlyBase] ChangeDeviceId: SDK must initialized before calling 'ChangeDeviceId'");
                return;
            }
            if (newDeviceId == null || newDeviceId.Length == 0) {
                UtilityHelper.CountlyLogging("[CountlyBase] ChangeDeviceId: New device id cannot be null or empty.");
                return;
            }

            if (!serverSideMerge) {
                //if no server side merge is needed, we just end the previous session and start a new session with the new id

                //Cancel all timed events
                CancelAllTimedEvents();

                bool sessionWasStarted = automaticSessionTrackingStarted;

                if (sessionWasStarted) {
                    //we need to end the session only if it was started previously
                    await SessionEnd();
                }
                await DeviceData.SetPreferredDeviceIdMethod(DeviceIdMethodInternal.developerSupplied, newDeviceId);
                if (consentRequired) {
                    await RemoveAllConsentInternal();
                }
                if (sessionWasStarted) {
                    //restart the session only if an automatic one was started before
                    await SessionBegin();
                }
            } else {
                //need server merge, therefore send special request
                DeviceId dId = await DeviceData.GetDeviceId();
                string oldId = dId.deviceId;

                //change device ID
                await DeviceData.SetPreferredDeviceIdMethod(DeviceIdMethodInternal.developerSupplied, newDeviceId);

                //create the required merge request
                Dictionary<string, object> requestParams =
                    new Dictionary<string, object> { { "old_device_id", oldId } };

                string request = await requestHelper.BuildRequest(requestParams);



                //add the request to queue and upload it
                await AddRequest(request, true);
                await Upload();
            }
        }

        
        /// <summary>
        /// Set the device id
        /// </summary>
        /// <param name="newDeviceId">New Id that should be used</param>
        /// <returns></returns>
        public async Task SetId(string newDeviceId)
        {
            bool withMerge = true;

            if (GetDeviceIDType().Equals(DeviceIdType.DeveloperProvided)) {
                // an ID was provided by the host app previously
                // we can assume that a device ID change with merge was executed previously
                // now we change it without merging
                withMerge = false;
            }

            await ChangeDeviceId(newDeviceId, withMerge);
        }

        internal bool IsConsentGiven(ConsentFeatures feature)
        {
            Debug.Assert(givenConsent != null);
            //if consent is not required, all is fine
            if (!consentRequired) { return true; };

            //if it's required
            //check if it's given
            if (givenConsent == null) { return false; }

            //nothing set for a feature, pressume that it's denied
            if (!givenConsent.ContainsKey(feature)) { return false; }

            //if a feature's consent is set, return it
            return givenConsent[feature];
        }

        public async Task SetConsent(Dictionary<ConsentFeatures, bool> consentChanges)
        {
            if (Configuration.backendMode) {
                UtilityHelper.CountlyLogging("[CountlyBase] SetConsent, Backend Mode enabled, returning");
                return;
            }

            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'SetConsent'");
            await SetConsentInternal(consentChanges, ConsentChangedAction.ConsentUpdated);
        }

        internal async Task RemoveAllConsentInternal()
        {
            Dictionary<ConsentFeatures, bool> removedConsent = new Dictionary<ConsentFeatures, bool>();
            foreach (KeyValuePair<ConsentFeatures, bool> entry in givenConsent) {
                if (entry.Value) {
                    removedConsent[entry.Key] = false;
                }
            }
            await SetConsentInternal(removedConsent, ConsentChangedAction.DeviceIDChangedNotMerged);
        }

        internal async Task SetConsentInternal(Dictionary<ConsentFeatures, bool> consentChanges, ConsentChangedAction action = ConsentChangedAction.ConsentUpdated)
        {
            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'SetConsentInternal'");
            Debug.Assert(consentChanges != null);
            if (consentChanges == null) {
                UtilityHelper.CountlyLogging("[CountlyBase] SetConsent: 'consentChanges' cannot be null");
                return;
            }

            //if we don't need consent, no need to track it
            if (!consentRequired) { return; }

            Dictionary<ConsentFeatures, bool> valuesToUpdate = new Dictionary<ConsentFeatures, bool>();

            //filter out those values that are changed
            foreach (KeyValuePair<ConsentFeatures, bool> entry in consentChanges) {
                bool oldV = IsConsentGiven(entry.Key);
                bool containsOld = givenConsent.ContainsKey(entry.Key);
                bool newV = entry.Value;

                if (!containsOld || oldV != newV) {
                    //if there is no entry about this feature, of the consent has changed, update the value
                    valuesToUpdate[entry.Key] = newV;
                    //mark changes locally
                    givenConsent[entry.Key] = newV;
                }
            }

            if (valuesToUpdate.Count > 0) {
                //send request of the consent changes
                if (action == ConsentChangedAction.ConsentUpdated || action == ConsentChangedAction.Initialization) {
                    await SendConsentChanges(givenConsent);
                }

                if (action == ConsentChangedAction.ConsentUpdated || action == ConsentChangedAction.DeviceIDChangedNotMerged) {
                    await ActionsOnConsentChanges(valuesToUpdate, action);
                }
            }
        }

        private async Task ActionsOnConsentChanges(Dictionary<ConsentFeatures, bool> updatedConsents, ConsentChangedAction action)
        {
            //react to consent changes locally
            foreach (KeyValuePair<ConsentFeatures, bool> entryChanges in updatedConsents) {
                bool isGiven = entryChanges.Value;
                ConsentFeatures feature = entryChanges.Key;

                //mark changes locally
                givenConsent[feature] = isGiven;

                //do special actions
                switch (feature) {
                    case ConsentFeatures.Crashes:
                        break;
                    case ConsentFeatures.Events:
                        CancelAllTimedEvents();
                        break;
                    case ConsentFeatures.Location:
                        if (!isGiven && action == ConsentChangedAction.ConsentUpdated) { await DisableLocation(); }
                        break;
                    case ConsentFeatures.Sessions:
                        if (isGiven && action == ConsentChangedAction.ConsentUpdated) {
                            //consent is being given

                            //we check if a session had been started before
                            if (automaticSessionTrackingStarted) {
                                //if 'automaticSessionTrackingStarted' is set to true then that means that a session was already started before
                                //this means that the user already had the intention for sessions to run (tried to use 'automatic' sessions
                                //to keep true to this original intention, we automatically start another session
                                await SessionBegin();
                            }
                        } else if (!isGiven) {
                            //session consent is being removed
                            //we would only want to end a session if it had been started
                            if (!startTime.Equals(DateTime.MinValue)) {
                                //if the 'startTime' value is not the min value then that means that a session was already started before
                                await SessionEnd();
                            }
                        }
                        break;
                    case ConsentFeatures.Users:
                        break;
                }
            }
        }

        internal async Task SendConsentChanges(Dictionary<ConsentFeatures, bool> updatedConsentChanges)
        {
            //create the required merge request
            TimeInstant timeInstant = timeHelper.GetUniqueInstant();
            string consentParam = requestHelper.CreateConsentUpdateRequest(updatedConsentChanges);

            Dictionary<string, object> requestParams =
                   new Dictionary<string, object> { { "consent", consentParam } };
            string request = await requestHelper.BuildRequest(requestParams);
            //add the request to queue and upload it
            await AddRequest(request);
            await Upload();
        }


        //track views
        private string lastView = null;
        private long lastViewStart = 0;
        private bool firstView = true;
        private const string NPS_EVENT_KEY = "[CLY]_nps";
        private const string VIEW_EVENT_KEY = "[CLY]_view";
        private const string SURVEY_EVENT_KEY = "[CLY]_survey";
        private const string STAR_RATING_EVENT_KEY = "[CLY]_star_rating";
        private const string PUSH_ACTION_EVENT_KEY = "[CLY]_push_action";
        private const string ORIENTATION_EVENT_KEY = "[CLY]_orientation";

        /// <summary>
        /// Records view
        /// </summary>
        /// <returns></returns>
        public async Task<bool> RecordView(string viewName)
        {
            if (Configuration.backendMode) {
                UtilityHelper.CountlyLogging("[CountlyBase] RecordView, Backend Mode enabled, returning false");
                return false;
            }

            UtilityHelper.CountlyLogging("[CountlyBase] Calling 'RecordView'");
            if (!IsInitialized()) {
                UtilityHelper.CountlyLogging("[CountlyBase] RecordView: SDK must initialized before calling 'RecordView'");
                return false;
            }

            if (viewName == null || viewName.Length == 0) {
                UtilityHelper.CountlyLogging("[CountlyBase] RecordView: 'viewName' cannot be null or empty.");
                return false;
            }


            if (!IsConsentGiven(ConsentFeatures.Views)) {
                //if we don't have consent, do nothing
                return false;
            }

            if (viewName.Length > Configuration.MaxKeyLength) {
                UtilityHelper.CountlyLogging("[CountlyBase] RecordView : Max allowed key length is " + Configuration.MaxKeyLength);
                viewName = viewName.Substring(0, Configuration.MaxKeyLength);
            }

            reportViewDuration();
            lastView = viewName;

            lastViewStart = timeHelper.GetUniqueUnixTime();
            Segmentation segm = new Segmentation();
            segm.Add("name", viewName);
            segm.Add("visit", "1");
            segm.Add("segment", "Windows");

            if (firstView) {
                firstView = false;
                segm.Add("start", "1");
            }
            return await RecordEventInternal(VIEW_EVENT_KEY, 1, null, null, segm);
        }

        /// <summary>
        /// Reports duration of last view
        /// </summary>
        private async void reportViewDuration()
        {
            if (lastView != null && lastViewStart <= 0) {
                UtilityHelper.CountlyLogging("[CountlyBase] Last view start value is not normal: [" + lastViewStart + "]");
            }

            if (!IsConsentGiven(ConsentFeatures.Views)) {
                //if we don't have consent, do nothing
                return;
            }

            //only record view if the view name is not null and if it has a reasonable duration
            //if the lastViewStart is equal to 0, the duration would be set to the current timestamp
            //and therefore will be ignored
            if (lastView != null && lastViewStart > 0) {
                long timestampSeconds = (timeHelper.GetUniqueUnixTime() - lastViewStart) / 1000;
                Segmentation segm = new Segmentation();
                segm.Add("name", lastView);
                segm.Add("dur", "" + timestampSeconds);
                segm.Add("segment", "Windows");

                await RecordEventInternal(VIEW_EVENT_KEY, 1, null, null, segm);

                lastView = null;
                lastViewStart = 0;
            }
        }

        /// <summary>
        /// Backend mode to handle multi application cases and
        /// data migrations
        /// </summary>
        /// <returns>BackendMode interface to use backend mode features</returns>
        public BackendMode BackendMode()
        {
            if (!Configuration.backendMode) {
                UtilityHelper.CountlyLogging("[CountlyBase] BackendMode, is not enabled returning null");
                return null;
            }

            return moduleBackendMode;
        }
    }
}
