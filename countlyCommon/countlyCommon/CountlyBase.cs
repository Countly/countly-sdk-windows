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
        protected bool uploadInProgress;

        //if stored event/sesstion/exception upload should be defered to a later time
        //if set to true, upload will not happen, but will just return "true"
        //data will still be saved in their respective files
        internal bool deferUpload = false;

        // File that stores events objects
        protected const string eventsFilename = "events.xml";
        // File that stores sessions objects
        protected const string sessionsFilename = "sessions.xml";
        // File that stores exceptions objects
        protected const string exceptionsFilename = "exceptions.xml";
        // File that stores temporary stored unhandled exception objects (currently used only for the windows target)
        protected const string unhandledExceptionFilename = "unhandled_exceptions.xml";
        // File that stores user details object
        protected const string userDetailsFilename = "userdetails.xml";

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

        // Used for thread-safe operations
        protected object sync = new object();

        protected String breadcrumb = String.Empty;

        // Start session timestamp
        protected DateTime startTime;

        //holds device info
        internal Device DeviceData = new Device();

        /// <summary>
        /// Determines if Countly debug messages are displayed to Output window
        /// </summary>
        public static bool IsLoggingEnabled { get; set; }

        /// <summary>
        ///  Adds session event to queue and uploads
        /// </summary>
        /// <param name="sessionEvent">session event object</param>
        /// <param name="uploadImmediately">indicates when start to upload, by default - immediately after event was added</param>
        internal async Task AddSessionEvent(SessionEvent sessionEvent, bool uploadImmediately = true)
        {
            try
            {
                if (!Countly.Instance.IsServerURLCorrect(ServerUrl))
                {
                    return;
                }

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
        protected async Task<bool> Upload()
        {
            if (deferUpload) return true;

            bool success = await UploadSessions();

            if (success)
            {
                success = await UploadEvents();
            }

            if (success)
            {
                success = await UploadExceptions();
            }

            return success;
        }

        /// <summary>
        /// Uploads sessions queue to Countly server
        /// </summary>
        /// <returns></returns>
        private async Task<bool> UploadSessions()
        {
            lock (sync)
            {
                if (uploadInProgress) return true;

                uploadInProgress = true;
            }

            SessionEvent sessionEvent = null;

            lock (sync)
            {
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
            return Countly.Instance.RecordEventInternal(Key, 1, null, null);
        }

        /// <summary>
        /// Records a custom event with no segmentation values, the specified count, and a sum of zero.
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        public static Task<bool> RecordEvent(string Key, int Count)
        {
            return Countly.Instance.RecordEventInternal(Key, Count, null, null);
        }

        /// <summary>
        /// Records a custom event with no segmentation values, and the specified count and sum.
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <param name="Sum">Sum to associate with the event</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        public static Task<bool> RecordEvent(string Key, int Count, double Sum)
        {
            return Countly.Instance.RecordEventInternal(Key, Count, Sum, null);
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
            return Countly.Instance.RecordEventInternal(Key, Count, null, Segmentation);
        }

        /// <summary>
        /// Records a custom event with the specified segmentation values, count and a sum
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <param name="Sum">Sum to associate with the event</param>
        /// <param name="Segmentation">Segmentation object to associate with the event, can be null</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        public static Task<bool> RecordEvent(string Key, int Count, double Sum, Segmentation Segmentation)
        {
            return Countly.Instance.RecordEventInternal(Key, Count, Sum, Segmentation);
        }

        /// <summary>
        /// Records a custom event with the specified values
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <param name="Sum">Sum to associate with the event</param>
        /// <param name="Segmentation">Segmentation object to associate with the event, can be null</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        protected async Task<bool> RecordEventInternal(string Key, int Count, double? Sum, Segmentation Segmentation)
        {
            if (!Countly.Instance.IsServerURLCorrect(ServerUrl))
            {
                return false;
            }

            CountlyEvent cEvent = new CountlyEvent(Key, Count, Sum, Segmentation);

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
            return await RecordException(error, stackTrace, null);
        }

        /// <summary>
        /// Records unhandled exception with stacktrace
        /// </summary>
        /// <param name="error">exception title</param>
        /// <param name="stackTrace">exception stacktrace</param>
        protected async Task<bool> RecordUnhandledException(string error, string stackTrace)
        {
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
            if(!IsServerURLCorrect(ServerUrl))
            {
                return false;
            }            

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
            if (!IsServerURLCorrect(ServerUrl))
            {
                return false;
            }

            ResultResponse resultResponse = await Api.Instance.UploadUserDetails(ServerUrl, AppKey, await DeviceData.GetDeviceId(), UserDetails);

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

            await UploadUserDetails();
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
        /// Adds log breadcrumb
        /// </summary>
        /// <param name="log">log string</param>
        public static void AddBreadCrumb(string log)
        {
            Countly.Instance.breadcrumb += log + "\r\n";
        }

        public static async Task<String> GetDeviceId()
        {
            if (!Countly.Instance.IsServerURLCorrect(Countly.Instance.ServerUrl))
            {
                if (Countly.IsLoggingEnabled)
                {
                    Debug.WriteLine("GetDeviceId cannot be called before StartingSession");
                }
                return "";
            }

            return await Countly.Instance.DeviceData.GetDeviceId();
        }

    }
}