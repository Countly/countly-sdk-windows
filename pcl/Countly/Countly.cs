/*
Copyright (c) 2012, 2013, 2014 Countly

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using CountlySDK.Server.Responses;
using System.IO;
using PCLStorage;

namespace CountlySDK
{
    /// <summary>
    /// This class is the public API for the Countly Windows Phone SDK.
    /// </summary>
    public static class Countly
    {
        // Current version of the Count.ly SDK as a displayable string.
        private const string sdkVersion = "18.01";

        // How often update session is sent
        private const int updateInterval = 60;

        // Server url provided by a user
        private static string ServerUrl;

        // Application key provided by a user
        private static string AppKey;

        // Application version provided by a user
        private static string AppVersion;

        // Indicates sync process with a server
        private static bool uploadInProgress;

        // File that stores events objects
        private const string eventsFilename = "events.xml";
        // File that stores sessions objects
        private const string sessionsFilename = "sessions.xml";
        // File that stores exceptions objects
        private const string exceptionsFilename = "exceptions.xml";
        // File that stores user details object
        private const string userDetailsFilename = "userdetails.xml";

        // Used for thread-safe operations
        private static object sync = new object();

        // Events queue
        private static List<CountlyEvent> Events { get; set; }

        // Session queue
        private static List<SessionEvent> Sessions { get; set; }

        // Exceptions queue
        private static List<ExceptionEvent> Exceptions { get; set; }

        // Raised when the async session is established
        public static event EventHandler SessionStarted;

        // User details info
        public static CountlyUserDetails UserDetails { get; set; }

        private static string breadcrumb = String.Empty;

        // Start session timestamp
        private static DateTime startTime;
        // Update session timer
        private static TimerHelper Timer;
        //holds device info
        private static Device DeviceData = new Device();

        /// <summary>
        /// Determines if Countly debug messages are displayed to Output window
        /// </summary>
        public static bool IsLoggingEnabled { get; set; }

        /// <summary>
        /// Saves collection to the storage
        /// </summary>
        /// <returns>True if success, otherwise - False</returns>
        private static async Task<bool> SaveCollection<T>(List<T> collection, string path)
        {
            List<T> collection_;

            lock (sync)
            {
                collection_ = collection.ToList();
            }

            bool success = await Storage.SaveToFile<List<T>>(path, collection_);

            if (success)
            {
                if (collection_.Count != collection.Count)
                {
                    // collection was changed during saving, save it again
                    return await SaveCollection<T>(collection, path);
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Saves events to the storage
        /// </summary>
        /// <returns>True if success, otherwise - False</returns>
        private static Task<bool> SaveEvents()
        {
            return SaveCollection<CountlyEvent>(Events, eventsFilename);
        }

        /// <summary>
        /// Saves sessions to the storage
        /// </summary>
        /// <returns>True if success, otherwise - False</returns>
        private static Task<bool> SaveSessions()
        {
            return SaveCollection<SessionEvent>(Sessions, sessionsFilename);
        }

        /// <summary>
        /// Saves exceptions to the storage
        /// </summary>
        /// <returns>True if success, otherwise - False</returns>
        private static Task<bool> SaveExceptions()
        {
            return SaveCollection<ExceptionEvent>(Exceptions, exceptionsFilename);
        }

        /// <summary>
        /// Saves user details info to the storage
        /// </summary>
        private static async Task SaveUserDetails()
        {
            await Storage.SaveToFile<CountlyUserDetails>(userDetailsFilename, UserDetails);
        }

        /// <summary>
        /// Starts Countly tracking session.
        /// Call from your App.xaml.cs Application_Launching and Application_Activated events.
        /// Must be called before other SDK methods can be used.
        /// </summary>
        /// <param name="serverUrl">URL of the Countly server to submit data to; use "https://cloud.count.ly" for Countly Cloud</param>
        /// <param name="appKey">app key for the application being tracked; find in the Countly Dashboard under Management > Applications</param>
        /// <param name="appVersion">Application version</param>
        public static async Task StartSession(string serverUrl, string appKey, string appVersion, IFileSystem fileSystem)
        {
            if (ServerUrl != null)
            {
                // session already active
                return;
            }

            if (String.IsNullOrWhiteSpace(serverUrl))
            {
                throw new ArgumentException("invalid server url");
            }

            if (String.IsNullOrWhiteSpace(appKey))
            {
                throw new ArgumentException("invalid application key");
            }

            ServerUrl = serverUrl;
            AppKey = appKey;
            AppVersion = appVersion;

            Storage.fileSystem = fileSystem;

            Events = await Storage.LoadFromFile<List<CountlyEvent>>(eventsFilename) ?? new List<CountlyEvent>();

            Sessions = await Storage.LoadFromFile<List<SessionEvent>>(sessionsFilename) ?? new List<SessionEvent>();

            Exceptions = await Storage.LoadFromFile<List<ExceptionEvent>>(exceptionsFilename) ?? new List<ExceptionEvent>();

            UserDetails = await Storage.LoadFromFile<CountlyUserDetails>(userDetailsFilename) ?? new CountlyUserDetails();

            UserDetails.UserDetailsChanged += OnUserDetailsChanged;

            startTime = DateTime.Now;

            Timer = new TimerHelper(UpdateSession, null, updateInterval * 1000, updateInterval * 1000);

            await AddSessionEvent(new BeginSession(AppKey, await DeviceData.GetDeviceId(), sdkVersion, new Metrics("Windows (PCL)", null, null, null, null, appVersion)));

            if (null != SessionStarted)
            {
                SessionStarted(null, EventArgs.Empty);
            }
        }        

        /// <summary>
        /// Sends session duration. Called automatically each <updateInterval> seconds
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void UpdateSession(object sender, object e)
        {
            await AddSessionEvent(new UpdateSession(AppKey, await DeviceData.GetDeviceId(), (int)DateTime.Now.Subtract(startTime).TotalSeconds));
        }

        /// <summary>
        /// End Countly tracking session.
        /// Call from your App.xaml.cs Application_Deactivated and Application_Closing events.
        /// </summary>
        public static async Task EndSession()
        {
            if (Timer != null)
            {
                Timer.Dispose();
                Timer = null;
            }

            await AddSessionEvent(new EndSession(AppKey, await DeviceData.GetDeviceId()), true);

            ServerUrl = null;
            AppKey = null;

            if (UserDetails != null)
            {
                UserDetails.UserDetailsChanged -= OnUserDetailsChanged;
            }
        }

        /// <summary>
        ///  Adds session event to queue and uploads
        /// </summary>
        /// <param name="sessionEvent">session event object</param>
        /// <param name="uploadImmediately">indicates when start to upload, by default - immediately after event was added</param>
        private static async Task AddSessionEvent(SessionEvent sessionEvent, bool uploadImmediately = true)
        {
            if (String.IsNullOrWhiteSpace(ServerUrl))
            {
                return;
            }
            
            lock (sync)
            {
                Sessions.Add(sessionEvent);
            }

            bool success = await SaveSessions();

            if (uploadImmediately && success)
            {
                await Upload();
            }
        }

        /// <summary>
        /// Uploads sessions queue to Countly server
        /// </summary>
        /// <returns></returns>
        private static async Task<bool> UploadSessions()
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
                ResultResponse resultResponse = await Api.SendSession(ServerUrl, sessionEvent, (UserDetails.isChanged) ? UserDetails : null);

                if (resultResponse != null && resultResponse.IsSuccess)
                {
                    UserDetails.isChanged = false;

                    await SaveUserDetails();

                    lock (sync)
                    {
                        uploadInProgress = false;

                        try
                        {
                            Sessions.RemoveAt(0);
                        }
                        catch { }
                    }

                    bool success = await SaveSessions();

                    if (!success)
                    {
                        return false;
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
        /// Immediately disables session, event, exceptions & user details tracking and clears any stored sessions, events, exceptions & user details data.
        /// This API is useful if your app has a tracking opt-out switch, and you want to immediately
        /// disable tracking when a user opts out. Call StartSession to enable logging again
        /// </summary>
        public static async void Halt()
        {
            lock (sync)
            {
                ServerUrl = null;
                AppKey = null;

                if (Timer != null)
                {
                    Timer.Dispose();
                    Timer = null;
                }

                if (UserDetails != null)
                {
                    UserDetails.UserDetailsChanged -= OnUserDetailsChanged;
                }

                Events.Clear();
                Sessions.Clear();
                Exceptions.Clear();
                breadcrumb = String.Empty;
                UserDetails = new CountlyUserDetails();
            }

            await Storage.DeleteFile(eventsFilename);
            await Storage.DeleteFile(sessionsFilename);
            await Storage.DeleteFile(exceptionsFilename);
            await Storage.DeleteFile(userDetailsFilename);
        }

        /// <summary>
        /// Records a custom event with no segmentation values, a count of one and a sum of zero
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        public static Task<bool> RecordEvent(string Key)
        {
            return RecordCountlyEvent(Key, 1, null, null);
        }

        /// <summary>
        /// Records a custom event with no segmentation values, the specified count, and a sum of zero.
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        public static Task<bool> RecordEvent(string Key, int Count)
        {
            return RecordCountlyEvent(Key, Count, null, null);
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
            return RecordCountlyEvent(Key, Count, Sum, null);
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
            return RecordCountlyEvent(Key, Count, null, Segmentation);
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
            return RecordCountlyEvent(Key, Count, Sum, Segmentation);
        }

        /// <summary>
        /// Records a custom event with the specified values
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <param name="Sum">Sum to associate with the event</param>
        /// <param name="Segmentation">Segmentation object to associate with the event, can be null</param>
        /// <returns>True if event is uploaded successfully, False - queued for delayed upload</returns>
        private static Task<bool> RecordCountlyEvent(string Key, int Count, double? Sum, Segmentation Segmentation)
        {
            return AddEvent(new CountlyEvent(Key, Count, Sum, Segmentation));
        }

        /// <summary>
        /// Adds event to queue and uploads
        /// </summary>
        /// <param name="countlyEvent">event object</param>
        /// <returns>True if success</returns>
        private async static Task<bool> AddEvent(CountlyEvent countlyEvent)
        {
            if (String.IsNullOrWhiteSpace(ServerUrl))
            {
                return false;
            }
                        
            lock (sync)
            {
                Events.Add(countlyEvent);
            }

            bool success = await SaveEvents();

            if (success)
            {
                success = await Upload();
            }

            return success;
        }

        /// <summary>
        /// Uploads event queue to Countly server
        /// </summary>
        /// <returns>True if success</returns>
        private static async Task<bool> UploadEvents()
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
                ResultResponse resultResponse = await Api.SendEvents(ServerUrl, AppKey, await DeviceData.GetDeviceId(), eventsToSend, (UserDetails.isChanged) ? UserDetails : null);

                if (resultResponse != null && resultResponse.IsSuccess)
                {
                    int eventsCountToUploadAgain = 0;

                    UserDetails.isChanged = false;

                    await SaveUserDetails();

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

                        eventsCountToUploadAgain = Events.Count;
                    }

                    bool success = await SaveEvents();

                    if (!success)
                    {
                        return false;
                    }

                    if (eventsCountToUploadAgain > 0)
                    {
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
        /// Raised when user details propery is changed
        /// </summary>
        private static async void OnUserDetailsChanged()
        {
            UserDetails.isChanged = true;

            await SaveUserDetails();

            await UploadUserDetails();
        }

        /// <summary>
        /// Uploads user details
        /// </summary>
        /// <returns>true if details are successfully uploaded, false otherwise</returns>
        internal static async Task<bool> UploadUserDetails()
        {
            if (String.IsNullOrWhiteSpace(Countly.ServerUrl))
            {
                return false;
            }

            ResultResponse resultResponse = await Api.UploadUserDetails(Countly.ServerUrl, Countly.AppKey, await DeviceData.GetDeviceId(), UserDetails);

            if (resultResponse != null && resultResponse.IsSuccess)
            {
                UserDetails.isChanged = false;

                await SaveUserDetails();

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Uploads user picture. Accepted picture formats are .png, .gif and .jpeg and picture will be resized to maximal 150x150 dimensions
        /// </summary>
        /// <param name="stream">Image stream</param>
        /// <returns>true if image is successfully uploaded, false otherwise</returns>
        internal static async Task<bool> UploadUserPicture(Stream imageStream)
        {
            if (String.IsNullOrWhiteSpace(Countly.ServerUrl))
            {
                return false;
            }

            ResultResponse resultResponse = await Api.UploadUserPicture(Countly.ServerUrl, Countly.AppKey, await DeviceData.GetDeviceId(), imageStream, (UserDetails.isChanged) ? UserDetails : null);

            return (resultResponse != null && resultResponse.IsSuccess);
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
        public static async Task<bool> RecordException(string error, string stackTrace)
        {
            return await RecordException(error, stackTrace, null);
        }

        /// <summary>
        /// Records unhandled exception with stacktrace
        /// </summary>
        /// <param name="error">exception title</param>
        /// <param name="stackTrace">exception stacktrace</param>
        private static async Task RecordUnhandledException(string error, string stackTrace)
        {
            await RecordException(error, stackTrace, null, true);
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
            if (String.IsNullOrWhiteSpace(ServerUrl))
            {
                return false;
            }

            TimeSpan run = DateTime.Now.Subtract(startTime);

            lock (sync)
            {
                Exceptions.Add(new ExceptionEvent(error, stackTrace ?? string.Empty, unhandled, breadcrumb, run, AppVersion, customInfo));
            }

            bool success = await SaveExceptions();

            if (!unhandled && success)
            {
                return await Upload();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Uploads exceptions queue to Countly server
        /// </summary>
        /// <returns>True if success</returns>
        private static async Task<bool> UploadExceptions()
        {
            lock (sync)
            {
                // Allow uploading in one thread only
                if (uploadInProgress) return true;

                uploadInProgress = true;
            }

            int exceptionsCount;

            lock (sync)
            {
                exceptionsCount = Exceptions.Count;
            }

            if (exceptionsCount > 0)
            {
                ExceptionEvent exEvent;
                lock (sync)
                {
                    exEvent = Exceptions[0];
                }
                ResultResponse resultResponse = await Api.SendException(ServerUrl, AppKey, await DeviceData.GetDeviceId(), exEvent);

                if (resultResponse != null && resultResponse.IsSuccess)
                {
                    int exceptionsCountToUploadAgain = 0;

                    lock (sync)
                    {
                        uploadInProgress = false;

                        try
                        {
                            Exceptions.RemoveAt(0);
                        }
                        catch { }

                        exceptionsCountToUploadAgain = Exceptions.Count;
                    }

                    bool success = await SaveExceptions();

                    if (!success)
                    {
                        return false;
                    }

                    if (exceptionsCountToUploadAgain > 0)
                    {
                        // Upload next exception
                        return await UploadExceptions();
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

                return false;
            }
        }

        /// <summary>
        /// Adds log breadcrumb
        /// </summary>
        /// <param name="log">log string</param>
        public static void AddBreadCrumb(string log)
        {
            breadcrumb += log + "\r\n";
        }

        /// <summary>
        /// Upload sessions, events & exception queues
        /// </summary>
        /// <returns>True if success</returns>
        private static async Task<bool> Upload()
        {
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
        public static async Task<String> GetDeviceId()
        {
            return await DeviceData.GetDeviceId();
        }
    }
}
