using CountlySDK.CountlyCommon.Entities;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using CountlySDK.Server.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CountlySDK.Entities.EntityBase.DeviceBase;

namespace CountlySDK.CountlyCommon
{
    abstract public class CountlyBase
    {
        // Current version of the Count.ly SDK as a displayable string.
        protected const string sdkVersion = "18.01";

        // How often update session is sent
        protected const int updateInterval = 60;

        // Server url provided by a user
        protected string ServerUrl;

        // Application key provided by a user
        protected string AppKey;

        // Application version provided by a user
        protected string AppVersion;

        // Indicates sync process with a server
        internal bool uploadInProgress;

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
            get
            {
                lock (Countly.Instance.sync)
                {
                    if (userDetails == null)
                    {
                        userDetails = Storage.Instance.LoadFromFile<CountlyUserDetails>(userDetailsFilename).Result;
                        if (userDetails == null) { userDetails = new CountlyUserDetails(); }
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

        protected String breadcrumb = String.Empty;

        // Start session timestamp
        protected DateTime startTime;

        // When the last session update was sent
        internal DateTime lastSessionUpdateTime;

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

        internal async Task<bool> SaveStoredRequests()
        {
            lock (sync)
            {
                return Storage.Instance.SaveToFile<Queue<StoredRequest>>(storedRequestsFilename, StoredRequests).Result;
            }
        }

        protected async Task UpdateSessionInternal(int? elapsedTime = null)
        {            
            if(elapsedTime == null)
            {
                //calculate elapsed time from the last time update was sent (includes manual calls)
                elapsedTime = (int)DateTime.Now.Subtract(lastSessionUpdateTime).TotalSeconds;
            }

            Debug.Assert(elapsedTime != null);
            lastSessionUpdateTime = DateTime.Now;

            await AddSessionEvent(new UpdateSession(AppKey, await DeviceData.GetDeviceId(), elapsedTime.Value));
        }

        /// <summary>
        /// End Countly tracking session.
        /// Call from one of these places:
        /// * your closing event
        /// * your App.xaml.cs Application_Deactivated and Application_Closing events.
        /// </summary>
        [Obsolete("static 'EndSession' is deprecated, please use 'Countly.Instance.SessionEnd' in place of this call")]
        public static async Task EndSession()
        {
            if (!Countly.Instance.IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'EndSession'"); }
            await Countly.Instance.EndSessionInternal();
        }

        protected async Task EndSessionInternal()
        {
            SessionTimerStop();
            int elapsedTime = (int)DateTime.Now.Subtract(lastSessionUpdateTime).TotalSeconds;
            await AddSessionEvent(new EndSession(AppKey, await DeviceData.GetDeviceId(), null, elapsedTime), true);
        }

        /// <summary>
        ///  Adds session event to queue and uploads
        /// </summary>
        /// <param name="sessionEvent">session event object</param>
        /// <param name="uploadImmediately">indicates when start to upload, by default - immediately after event was added</param>
        internal async Task AddSessionEvent(SessionEvent sessionEvent, bool uploadImmediately = true)
        {
            try
            {
                if (!Countly.Instance.IsServerURLCorrect(ServerUrl)) { return; }

                if(!IsConsentGiven(ConsentFeatures.Sessions)) { return; }

                lock (sync)
                {
                    Sessions.Add(sessionEvent);
                }

                bool success = SaveSessions();

                if (uploadImmediately && success)
                {
                    await Upload();
                }
            }
            catch (Exception ex)
            {
                if (IsLoggingEnabled)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Upload sessions, events & exception queues
        /// </summary>
        /// <returns>True if success</returns>
        internal async Task<bool> Upload()
        {
            bool success = false;
            bool shouldContinue = false;

            do
            {
                if (deferUpload) return true;

                success = await UploadSessions();

                if (success)
                {
                    success = await UploadEvents();
                }

                if (success)
                {
                    success = await UploadExceptions();
                }

                if (success)
                {
                    success = await UploadUserDetails();
                }

                if (success)
                {
                    success = await UploadStoredRequests();
                }

                if (success && !uploadInProgress)
                {
                    int sC, exC, evC, rC;
                    bool isChanged;

                    lock (sync)
                    {
                        sC = Sessions.Count;
                        exC = Exceptions.Count;
                        evC = Events.Count;
                        rC = StoredRequests.Count;
                        isChanged = UserDetails.isChanged;
                    }                

                    if (sC > 0 || exC > 0 || evC > 0 || isChanged)
                    {
                        //work still needs to be done
                        return await Upload();
                    }
                }
            } while (success && shouldContinue);


            return success;
        }

        private async Task<bool> UploadStoredRequests()
        {
            StoredRequest sr = null;

            lock (sync)
            {
                if (uploadInProgress) return true;
                uploadInProgress = true;

                if (StoredRequests.Count > 0)
                {
                    sr = StoredRequests.Peek();
                    Debug.Assert(sr != null);
                }
            }

            if(sr != null)
            {
                ResultResponse resultResponse = await Api.Instance.SendStoredRequest(ServerUrl, sr);

                if (resultResponse != null && resultResponse.IsSuccess)
                {
                    lock (sync)
                    {
                        uploadInProgress = false;

                        //remove the excecuted request
                        StoredRequest srd = null;
                        try { srd = StoredRequests.Dequeue(); } catch { }
                        Debug.Assert(srd != null);
                        Debug.Assert(srd == sr);

                        bool success = SaveStoredRequests().Result;//todo, handle this in the future                        
                    }
                    return true;
                }
                else
                {
                    lock (sync) { uploadInProgress = false; }
                    return false;
                }
            }
            else
            {
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
            SessionEvent sessionEvent = null;

            lock (sync)
            {
                if (uploadInProgress) return true;
                uploadInProgress = true;
            
                if (Sessions.Count > 0)
                {
                    sessionEvent = Sessions[0];
                }
            }

            if (sessionEvent != null)
            {
                ResultResponse resultResponse = await Api.Instance.SendSession(ServerUrl, sessionEvent, (UserDetails.isChanged) ? UserDetails : null);

                if (resultResponse != null && resultResponse.IsSuccess)
                {
                    lock (sync)
                    {
                        UserDetails.isChanged = false;
                    }

                    SaveUserDetails();

                    lock (sync)
                    {
                        uploadInProgress = false;

                        try
                        {
                            Sessions.RemoveAt(0);
                        }
                        catch { }
                        bool success = SaveSessions();//todo, handle this in the future
                    }

                    int sessionCount = 0;
                    lock (sync)
                    {
                        sessionCount = Sessions.Count;
                    }

                    if (sessionCount > 0)
                    {
                        return await UploadSessions();
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    uploadInProgress = false;
                    return false;
                }
            }
            else
            {
                uploadInProgress = false;
                return true;
            }
        }

        /// <summary>
        /// Records a custom event with no segmentation values, a count of one and a sum of zero
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        public static Task<bool> RecordEvent(string Key)
        {
            if (!Countly.Instance.IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'RecordEvent'"); }
            return Countly.Instance.RecordEventInternal(Key, 1, null, null, null);
        }

        /// <summary>
        /// Records a custom event with no segmentation values, the specified count, and a sum of zero.
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        public static Task<bool> RecordEvent(string Key, int Count)
        {
            if (!Countly.Instance.IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'RecordEvent'"); }
            return Countly.Instance.RecordEventInternal(Key, Count, null, null, null);
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
            if (!Countly.Instance.IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'RecordEvent'"); }
            return Countly.Instance.RecordEventInternal(Key, Count, Sum, null, null);
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
            if (!Countly.Instance.IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'RecordEvent'"); }
            return Countly.Instance.RecordEventInternal(Key, Count, null, null, Segmentation);
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
            if (!Countly.Instance.IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'RecordEvent'"); }
            return Countly.Instance.RecordEventInternal(Key, Count, Sum, null, Segmentation);
        }

        /// <summary>
        /// Records a custom event with the specified segmentation values, count and a sum
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <param name="Sum">Sum to associate with the event</param>
        /// /// <param name="Sum">Event duration</param>
        /// <param name="Segmentation">Segmentation object to associate with the event, can be null</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        public static Task<bool> RecordEvent(string Key, int Count, double? Sum, double? Duration, Segmentation Segmentation)
        {
            if (!Countly.Instance.IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'RecordEvent'"); }
            return Countly.Instance.RecordEventInternal(Key, Count, Sum, Duration, Segmentation);
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
            if (!Countly.Instance.IsServerURLCorrect(ServerUrl)) { return false; }
            if (!IsConsentGiven(ConsentFeatures.Events)) { return true; }

            CountlyEvent cEvent = new CountlyEvent(Key, Count, Sum, Duration, Segmentation);

            bool saveSuccess = false;
            lock (sync)
            {
                Events.Add(cEvent);
                saveSuccess = SaveEvents();
            }

            if (saveSuccess)
            {
                saveSuccess = await Upload();
            }

            return saveSuccess;
        }

        /// <summary>
        /// Uploads event queue to Countly server
        /// </summary>
        /// <returns>True if success</returns>
        private async Task<bool> UploadEvents()
        {
            lock (sync)
            {
                // Allow uploading in one thread only
                if (uploadInProgress) return true;

                uploadInProgress = true;
            }

            int eventsCount;

            lock (sync)
            {
                eventsCount = Math.Min(15, Events.Count);
            }

            if (eventsCount > 0)
            {
                List<CountlyEvent> eventsToSend = null;
                lock (sync)
                {
                    eventsToSend = Events.Take(eventsCount).ToList();
                }
                ResultResponse resultResponse = await Api.Instance.SendEvents(ServerUrl, AppKey, await DeviceData.GetDeviceId(), eventsToSend, (UserDetails.isChanged) ? UserDetails : null);

                if (resultResponse != null && resultResponse.IsSuccess)
                {
                    int eventsCountToUploadAgain = 0;

                    UserDetails.isChanged = false;

                    SaveUserDetails();

                    lock (sync)
                    {
                        uploadInProgress = false;

                        try
                        {
                            for (int i = eventsCount - 1; i >= 0; i--)
                            {
                                Events.RemoveAt(i);
                            }
                        }
                        catch { }

                        bool success = SaveEvents();//todo, react to this in the future
                        eventsCountToUploadAgain = Events.Count;
                    }

                    if (eventsCountToUploadAgain > 0)
                    {
                        // Upload events added during sync
                        return await UploadEvents();
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    uploadInProgress = false;
                    return false;
                }
            }
            else
            {
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
            if (!Countly.Instance.IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'RecordException'"); }
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
            if (!Countly.Instance.IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'RecordException'"); }
            return await RecordException(error, stackTrace, null);
        }

        /// <summary>
        /// Records unhandled exception with stacktrace
        /// </summary>
        /// <param name="error">exception title</param>
        /// <param name="stackTrace">exception stacktrace</param>
        protected async Task<bool> RecordUnhandledException(string error, string stackTrace)
        {
            if (!Countly.Instance.IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'RecordException'"); }
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
            if (!Countly.Instance.IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'RecordException'"); }
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
            if (!Countly.Instance.IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'RecordException'"); }
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
            if(!IsServerURLCorrect(ServerUrl)) { return false; }
            if (!IsConsentGiven(ConsentFeatures.Crashes)) { return true; }

            TimeSpan run = DateTime.Now.Subtract(startTime);

            ExceptionEvent eEvent = new ExceptionEvent(error, stackTrace ?? string.Empty, unhandled, breadcrumb, run, AppVersion, customInfo, DeviceData);

            if (!unhandled)
            {
                bool saveSuccess = false;
                lock (sync)
                {
                    Exceptions.Add(eEvent);
                    saveSuccess = SaveExceptions();
                }

                if (saveSuccess)
                {
                    return await Upload();
                }

                return false;
            }
            else
            {
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
            lock (sync)
            {
                // Allow uploading in one thread only
                if (uploadInProgress) return true;

                uploadInProgress = true;
            }

            int exceptionsCount;//how many exceptions are stored

            lock (sync)
            {
                exceptionsCount = Exceptions.Count;
            }

            //if there is at least one exception stored, do the upload
            if (exceptionsCount > 0)
            {
                ExceptionEvent exEvent;//the exception event that will be uploaded
                lock (sync)
                {
                    exEvent = Exceptions[0];
                }

                //do the exception upload
                ResultResponse resultResponse = await Api.Instance.SendException(ServerUrl, AppKey, await DeviceData.GetDeviceId(), exEvent);

                //check if we got a response and that it was a success
                if (resultResponse != null && resultResponse.IsSuccess)
                {
                    int exceptionsCountToUploadAgain = 0;

                    lock (sync)
                    {
                        try
                        {
                            Exceptions.RemoveAt(0);
                        }
                        catch { }

                        var res = SaveExceptions();//todo, in the future, react to this failing

                        exceptionsCountToUploadAgain = Exceptions.Count;
                        uploadInProgress = false;//mark that we have stoped upload
                    }

                    if (exceptionsCountToUploadAgain > 0)
                    {
                        // Upload next exception
                        return await UploadExceptions();
                    }
                    else
                    {
                        //no exceptions left to upload
                        return true;
                    }
                }
                else
                {
                    //if the received response was not a success
                    uploadInProgress = false;
                    return false;
                }
            }
            else
            {
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
            if (!IsServerURLCorrect(ServerUrl)) { return false; }
            if (!IsConsentGiven(ConsentFeatures.Users)) { return true; }

            lock (sync)
            {
                //upload only when needed
                if (!UserDetails.isChanged) return true;

                // Allow uploading in one thread only
                if (uploadInProgress) return true;

                uploadInProgress = true;
            }

            ResultResponse resultResponse = await Api.Instance.UploadUserDetails(ServerUrl, AppKey, await DeviceData.GetDeviceId(), UserDetails);

            lock (sync)
            {
                uploadInProgress = false;
            }

            if (resultResponse != null && resultResponse.IsSuccess)
            {                
                UserDetails.isChanged = false;

                SaveUserDetails();

                return true;
            }
            else
            {                
                return false;
            }
        }

        /// <summary>
        /// Raised when user details propery is changed
        /// </summary>
        protected async void OnUserDetailsChanged()
        {
            UserDetails.isChanged = true;

            SaveUserDetails();

            await Upload();
        }

        /// <summary>
        /// Uploads user picture. Accepted picture formats are .png, .gif and .jpeg and picture will be resized to maximal 150x150 dimensions
        /// </summary>
        /// <param name="stream">Image stream</param>
        /// <returns>true if image is successfully uploaded, false otherwise</returns>
        internal async Task<bool> UploadUserPicture(Stream imageStream)
        {
            if (!IsServerURLCorrect(ServerUrl))
            {
                return false;
            }

            ResultResponse resultResponse = await Api.Instance.UploadUserPicture(ServerUrl, AppKey, await DeviceData.GetDeviceId(), imageStream, (UserDetails.isChanged) ? UserDetails : null);

            return (resultResponse != null && resultResponse.IsSuccess);
        }

        /// <summary>
        /// Immediately disables session, event, exceptions & user details tracking and clears any stored sessions, events, exceptions & user details data.
        /// This API is useful if your app has a tracking opt-out switch, and you want to immediately
        /// disable tracking when a user opts out. Call StartSession to enable logging again
        /// </summary>
        public static async void Halt()
        {
            await Countly.Instance.HaltInternal();
        }

        protected async Task HaltInternal()
        {
            lock (sync)
            {
                ServerUrl = null;
                AppKey = null;

                SessionTimerStop();

                Events?.Clear();
                Sessions?.Clear();
                Exceptions?.Clear();
                breadcrumb = String.Empty;
                DeviceData = new Device();

                if (UserDetails != null)
                {
                    UserDetails.UserDetailsChanged -= OnUserDetailsChanged;
                }
                userDetails = null;//set it null so that it can be loaded from the file system (if needed)

                consentRequired = false;
                givenConsent = new Dictionary<ConsentFeatures, bool>();
            }
            await Storage.Instance.DeleteFile(eventsFilename);
            await Storage.Instance.DeleteFile(sessionsFilename);
            await Storage.Instance.DeleteFile(exceptionsFilename);
            await Storage.Instance.DeleteFile(userDetailsFilename);
            await Storage.Instance.DeleteFile(Device.deviceFilename);
        }

        /// <summary>
        /// Adds log breadcrumb
        /// </summary>
        /// <param name="log">log string</param>
        public static void AddBreadCrumb(string log)
        {
            if (!Countly.Instance.IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'AddBreadCrumb'"); }
            Debug.Assert(log != null);
            Countly.Instance.breadcrumb += log + "\r\n";
        }

        public static async Task<String> GetDeviceId()
        {
            if (!Countly.Instance.IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'GetDeviceId'"); }

            return await Countly.Instance.DeviceData.GetDeviceId();
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

        public async Task<bool> SetLocation(String gpsLocation, String ipAddress = null, String country_code = null, String city = null)
        {
            if (!IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'SetLocation'"); }
            if (!IsConsentGiven(ConsentFeatures.Location)) { return true; }

            if (gpsLocation == null && ipAddress == null && country_code == null && city == null)
            {
                return false;
            }

            //create the required request
            String br = RequestHelper.CreateBaseRequest(AppKey, await DeviceData.GetDeviceId());
            String lr = RequestHelper.CreateLocationRequest(br, gpsLocation, ipAddress, country_code, city);

            //add the request to queue and upload it
            await AddRequest(lr);
            return await Upload();
        }

        public async Task<bool> DisableLocation()
        {
            if (!IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'DisableLocation'"); }
            if (!IsConsentGiven(ConsentFeatures.Location)) { return true; }

            return await SetLocation("", "", "", "");
        }

        internal bool IsInitialized()
        {
            if(ServerUrl != null && AppKey != null)
            {
                return true;
            }
            return false;
        }

        internal async Task AddRequest(String networkRequest, bool isIdMerge = false)
        {
            Debug.Assert(networkRequest != null);

            if(networkRequest == null) { return; }

            lock (sync)
            {
                StoredRequest sr = new StoredRequest(networkRequest, isIdMerge);
                StoredRequests.Enqueue(sr);
                SaveStoredRequests();
            }
        }

        public abstract Task Init(CountlyConfig config);

        protected async Task InitBase(CountlyConfig config)
        {
            if (!IsServerURLCorrect(config.serverUrl)) { throw new ArgumentException("invalid server url"); }
            if (!IsAppKeyCorrect(config.appKey)) { throw new ArgumentException("invalid application key"); }

            ServerUrl = config.serverUrl;
            AppKey = config.appKey;
            AppVersion = config.appVersion;

            if(config.developerProvidedDeviceId?.Length == 0) { throw new ArgumentException("'developerProvidedDeviceId' cannot be empty string"); }
            await DeviceData.SetPreferredDeviceIdMethod((DeviceIdMethodInternal)config.deviceIdMethod, config.developerProvidedDeviceId);

            lock (sync)
            {
                StoredRequests = Storage.Instance.LoadFromFile<Queue<StoredRequest>>(eventsFilename).Result ?? new Queue<StoredRequest>();
                Events = Storage.Instance.LoadFromFile<List<CountlyEvent>>(eventsFilename).Result ?? new List<CountlyEvent>();
                Sessions = Storage.Instance.LoadFromFile<List<SessionEvent>>(sessionsFilename).Result ?? new List<SessionEvent>();
                Exceptions = Storage.Instance.LoadFromFile<List<ExceptionEvent>>(exceptionsFilename).Result ?? new List<ExceptionEvent>();
            }

            //consent related
            consentRequired = config.consentRequired;
            if (config.givenConsent != null) { await SetConsent(config.givenConsent); }
        }

        protected abstract Task SessionBeginInternal();

        /// <summary>
        /// Start tracking a session
        /// Should be called only once
        /// </summary>
        /// <returns></returns>
        public async Task SessionBegin()
        {
            if (!IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'SessionBegin'"); }

            await SessionBeginInternal();
        }

        /// <summary>
        /// Manually update session
        /// </summary>
        /// <returns></returns>
        public async Task SessionUpdate(int elapsedTimeSeconds)
        {
            if (!IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'SessionUpdate'"); }
            if (elapsedTimeSeconds < 0) { throw new ArgumentException("Elapsed time can not be negative"); }

            await UpdateSessionInternal(elapsedTimeSeconds);
        }

        /// <summary>
        /// End tracking a session
        /// </summary>
        /// <returns></returns>
        public async Task SessionEnd()
        {
            if (!IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'SessionEnd'"); }

            await Countly.Instance.EndSessionInternal();
        }


        /// <summary>
        /// Change this devices Id
        /// </summary>
        /// <param name="newDeviceId">New Id that should be used</param>
        /// <param name="serverSideMerge">If set to true, old user id's data will be merged into new user</param>
        /// <returns></returns>
        public async Task ChangeDeviceId(String newDeviceId, bool serverSideMerge = false)
        {
            if (!IsInitialized()) { throw new InvalidOperationException("SDK must initialized before calling 'ChangeDeviceId'"); }
            if (newDeviceId == null) { throw new ArgumentException("New device id cannot be null"); }
            if (newDeviceId.Length == 0) { throw new ArgumentException("New device id cannot be empty string"); }

            if (!serverSideMerge)
            {
                //if no server side merge is needed, we just end the previous session and start a new session with the new id
                await SessionEnd();
                await DeviceData.SetPreferredDeviceIdMethod(DeviceIdMethodInternal.developerSupplied, newDeviceId);
                await SessionBegin();
            }
            else
            {
                //need server merge, therefore send special request
                String oldId = await DeviceData.GetDeviceId();

                //create the required merge request
                String br = RequestHelper.CreateBaseRequest(AppKey, newDeviceId);
                String dimr = RequestHelper.CreateDeviceIdMergeRequest(br, oldId);

                //change device ID
                await DeviceData.SetPreferredDeviceIdMethod(DeviceIdMethodInternal.developerSupplied, newDeviceId);

                //add the request to queue and upload it
                await AddRequest(dimr, true);
                await Upload();
            }
        }

        internal bool consentRequired = false;
        internal Dictionary<ConsentFeatures, bool> givenConsent = new Dictionary<ConsentFeatures, bool>();

        public enum ConsentFeatures { Sessions, Events, Location, Crashes, Users };

        internal bool IsConsentGiven(ConsentFeatures feature)
        {
            Debug.Assert(givenConsent != null);
            //if consent is not required, all is fine
            if (!consentRequired) { return true; };

            //if it's required
            //check if it's given
            if(givenConsent == null) { return false; }

            //nothing set for a feature, pressume that it's denied
            if (!givenConsent.ContainsKey(feature)) { return false; }

            //if a feature's consent is set, return it
            return givenConsent[feature];
        }

        public async Task SetConsent(Dictionary<ConsentFeatures, bool> consentChanges)
        {
            Debug.Assert(consentChanges != null);
            if (consentChanges == null) { throw new ArgumentException("'consentChanges' cannot be null"); }
            //if we don't need consent, no need to track it
            if (!consentRequired) { return; }

            Dictionary<ConsentFeatures, bool> valuesToUpdate = new Dictionary<ConsentFeatures, bool>();

            //filter out those values that are changed
            foreach (KeyValuePair<ConsentFeatures, bool> entry in consentChanges)
            {
                bool oldV = IsConsentGiven(entry.Key);
                bool containsOld = givenConsent.ContainsKey(entry.Key);
                bool newV = entry.Value;

                if(!containsOld || oldV != newV)
                {                 
                    //if there is no entry about this feature, of the consent has changed, update the value
                    valuesToUpdate[entry.Key] = newV;
                }
            }

            if(valuesToUpdate.Count > 0)
            {
                //send request of the consent changes
                await SendConsentChanges(valuesToUpdate);

                //react to consent changes locally
                foreach (KeyValuePair<ConsentFeatures, bool> entryChanges in valuesToUpdate)
                {
                    bool isGiven = entryChanges.Value;
                    ConsentFeatures feature = entryChanges.Key;

                    //mark changes locally
                    givenConsent[feature] = isGiven;

                    //do special actions
                    switch (feature)
                    {
                        case ConsentFeatures.Crashes:
                            break;
                        case ConsentFeatures.Events:
                            break;
                        case ConsentFeatures.Location:
                            if (!isGiven) { await DisableLocation(); }
                            break;
                        case ConsentFeatures.Sessions:
                            if (isGiven)
                            {
                                if(!startTime.Equals(DateTime.MinValue))
                                {
                                    //if it's not null then we had already tried tracking a session
                                    await SessionBegin();
                                }                                
                            }
                            else { await SessionEnd(); }
                            break;
                        case ConsentFeatures.Users:
                            break;
                    }
                }
            }
        }

        internal async Task SendConsentChanges(Dictionary<ConsentFeatures, bool> updatedConsentChanges)
        {
            //create the required merge request
            String br = RequestHelper.CreateBaseRequest(AppKey, await DeviceData.GetDeviceId());
            String cur = RequestHelper.CreateCosnentUpdateRequest(br, updatedConsentChanges);

            //add the request to queue and upload it
            await AddRequest(cur);
            await Upload();
        }
    }
}