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
using System.Windows.Threading;
using CountlySDK.Entities;
using CountlySDK.Entitites;
using CountlySDK.Helpers;
using CountlySDK.Server.Responses;
using System.IO;
using System.Windows;

namespace CountlySDK
{
    /// <summary>
    /// This class is the public API for the Countly Windows Phone SDK.
    /// </summary>
    public static class Countly
    {
        // Current version of the Count.ly Windows Phone SDK as a displayable string.
        private const string sdkVersion = "1.0";

        // How often update session is sent
        private const int updateInterval = 60;

        // Server url provided by a user
        private static string ServerUrl;

        // Application key provided by a user
        private static string AppKey;

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

        private static List<CountlyEvent> events;
        // Events queue
        private static List<CountlyEvent> Events
        {
            get
            {
                lock (sync)
                {
                    if (events == null)
                    {
                        events = Storage.LoadFromFile<List<CountlyEvent>>(eventsFilename);

                        if (events == null)
                        {
                            events = new List<CountlyEvent>();
                        }
                    }
                }

                return events;
            }
        }

        private static List<SessionEvent> sessions;
        // Session queue
        private static List<SessionEvent> Sessions
        {
            get
            {
                lock (sync)
                {
                    if (sessions == null)
                    {
                        sessions = Storage.LoadFromFile<List<SessionEvent>>(sessionsFilename);

                        if (sessions == null)
                        {
                            sessions = new List<SessionEvent>();
                        }
                    }
                }

                return sessions;
            }
        }

        private static List<ExceptionEvent> exceptions;
        // Exceptions queue
        private static List<ExceptionEvent> Exceptions
        {
            get
            {
                lock (sync)
                {
                    if (exceptions == null)
                    {
                        exceptions = Storage.LoadFromFile<List<ExceptionEvent>>(exceptionsFilename);

                        if (exceptions == null)
                        {
                            exceptions = new List<ExceptionEvent>();
                        }
                    }
                }

                return exceptions;
            }
        }

        private static CountlyUserDetails userDetails;
        // User details info
        public static CountlyUserDetails UserDetails
        {
            get
            {
                lock (sync)
                {
                    if (userDetails == null)
                    {
                        userDetails = Storage.LoadFromFile<CountlyUserDetails>(userDetailsFilename);

                        if (userDetails == null)
                        {
                            userDetails = new CountlyUserDetails();
                        }

                        userDetails.UserDetailsChanged += OnUserDetailsChanged;
                    }
                }

                return userDetails;
            }
        }

        private static string breadcrumb = String.Empty;

        // Start session timestamp
        private static DateTime startTime;
        // Update session timer
        private static DispatcherTimer Timer;

        /// <summary>
        /// Determines if Countly debug messages are displayed to Output window
        /// </summary>
        public static bool IsLoggingEnabled { get; set; }

        /// <summary>
        /// Determines if exception autotracking is enabled
        /// </summary>
        public static bool IsExceptionsLoggingEnabled { get; set; }

        /// <summary>
        /// Saves events to the storage
        /// </summary>
        private static void SaveEvents()
        {
            Storage.SaveToFile(eventsFilename, Events);
        }

        /// <summary>
        /// Saves sessions to the storage
        /// </summary>
        private static void SaveSessions()
        {
            Storage.SaveToFile(sessionsFilename, Sessions);
        }

        /// <summary>
        /// Saves exceptions to the storage
        /// </summary>
        private static void SaveExceptions()
        {
            Storage.SaveToFile(exceptionsFilename, Exceptions);
        }

        /// <summary>
        /// Saves user details info to the storage
        /// </summary>
        private static void SaveUserDetails()
        {
            Storage.SaveToFile(userDetailsFilename, UserDetails);
        }

        /// <summary>
        /// Starts Countly tracking session.
        /// Call from your App.xaml.cs Application_Launching and Application_Activated events.
        /// Must be called before other SDK methods can be used.
        /// </summary>
        /// <param name="serverUrl">URL of the Countly server to submit data to; use "https://cloud.count.ly" for Countly Cloud</param>
        /// <param name="appKey">app key for the application being tracked; find in the Countly Dashboard under Management > Applications</param>
        /// <param name="application">Application object that allows SDK track unhandled exceptions</param>
        public static void StartSession(string serverUrl, string appKey, Application application = null)
        {
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

            if (application != null)
            {
                IsExceptionsLoggingEnabled = true;

                application.UnhandledException -= OnApplicationUnhandledException;
                application.UnhandledException += OnApplicationUnhandledException;
            }

            startTime = DateTime.Now;

            Timer = new DispatcherTimer();
            Timer.Interval = TimeSpan.FromSeconds(updateInterval);
            Timer.Tick += UpdateSession;
            Timer.Start();

            AddSessionEvent(new BeginSession(AppKey, Device.DeviceId, sdkVersion, new Metrics(Device.OS, Device.OSVersion, Device.DeviceName, Device.Resolution, Device.Carrier, Device.AppVersion)));
        }

        /// <summary>
        /// Starts Countly background tracking session.
        /// Call from your background agent OnInvoke method.
        /// Must be called before other SDK methods can be used.
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <param name="appKey"></param>
        public static void StartBackgroundSession(string serverUrl, string appKey)
        {
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
        }

        /// <summary>
        /// Sends session duration. Called automatically each <updateInterval> seconds
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void UpdateSession(object sender, EventArgs e)
        {
            AddSessionEvent(new UpdateSession(AppKey, Device.DeviceId, (int)DateTime.Now.Subtract(startTime).TotalSeconds));
        }

        /// <summary>
        /// End Countly tracking session.
        /// Call from your App.xaml.cs Application_Deactivated and Application_Closing events.
        /// </summary>
        public static void EndSession()
        {
            if (Timer != null)
            {
                Timer.Stop();
                Timer.Tick -= UpdateSession;
                Timer = null;
            }

            AddSessionEvent(new EndSession(AppKey, Device.DeviceId), true);
        }

        /// <summary>
        ///  Adds session event to queue and uploads
        /// </summary>
        /// <param name="sessionEvent">session event object</param>
        /// <param name="uploadImmediately">indicates when start to upload, by default - immediately after event was added</param>
        private static void AddSessionEvent(SessionEvent sessionEvent, bool uploadImmediately = true)
        {
            if (String.IsNullOrWhiteSpace(ServerUrl))
            {
                throw new InvalidOperationException("session is not active");
            }

            ThreadPool.QueueUserWorkItem((work) =>
            {
                lock (sync)
                {
                    Sessions.Add(sessionEvent);

                    SaveSessions();
                }

                if (uploadImmediately)
                {
                    Upload();
                }
            });
        }

        /// <summary>
        /// Uploads sessions queue to Countly server
        /// </summary>
        private static void UploadSessions(Action<bool> callback)
        {
            lock (sync)
            {
                if (uploadInProgress)
                {
                    callback(true);

                    return;
                }

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
                Api.SendSession(ServerUrl, sessionEvent, (UserDetails.isChanged) ? UserDetails : null, (resultResponse) =>
                {
                    if (resultResponse != null && resultResponse.IsSuccess)
                    {
                        UserDetails.isChanged = false;

                        SaveUserDetails();

                        lock (sync)
                        {
                            uploadInProgress = false;

                            try
                            {
                                Sessions.RemoveAt(0);
                            }
                            catch { }

                            Storage.SaveToFile(sessionsFilename, Sessions);
                        }

                        if (Sessions.Count > 0)
                        {
                            UploadSessions(callback);
                        }
                        else
                        {
                            callback(true);
                        }
                    }
                    else
                    {
                        uploadInProgress = false;

                        callback(false);
                    }
                });
            }
            else
            {
                uploadInProgress = false;

                callback(true);
            }
        }

        /// <summary>
        /// Raised when application unhandled exception is thrown
        /// </summary>
        /// <param name="sender">sender param</param>
        /// <param name="e">exception details</param>
        private static void OnApplicationUnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (IsExceptionsLoggingEnabled)
            {
                RecordUnhandledException(e.ExceptionObject.Message, e.ExceptionObject.StackTrace);
            }
        }

        /// <summary>
        /// Immediately disables session, event, exceptions & user details tracking and clears any stored sessions, events, exceptions & user details data.
        /// This API is useful if your app has a tracking opt-out switch, and you want to immediately
        /// disable tracking when a user opts out. The EndSession/RecordEvent methods will throw
        /// InvalidOperationException after calling this until Countly is reinitialized by calling StartSession
        /// again.
        /// </summary>
        public static void Halt()
        {
            lock (sync)
            {
                ServerUrl = null;
                AppKey = null;

                if (Timer != null)
                {
                    Timer.Stop();
                    Timer.Tick -= UpdateSession;
                    Timer = null;
                }

                Events.Clear();
                Sessions.Clear();
                Exceptions.Clear();
                breadcrumb = String.Empty;
                if (userDetails != null)
                {
                    userDetails.UserDetailsChanged -= OnUserDetailsChanged;
                }
                userDetails = new CountlyUserDetails();

                Storage.DeleteFile(eventsFilename);
                Storage.DeleteFile(sessionsFilename);
                Storage.DeleteFile(exceptionsFilename);
                Storage.DeleteFile(userDetailsFilename);
            }
        }

        /// <summary>
        /// Records a custom event with no segmentation values, a count of one and a sum of zero
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        public static void RecordEvent(string Key)
        {
            RecordCountlyEvent(Key, 1, null, null);
        }

        /// <summary>
        /// Records a custom event with no segmentation values, the specified count, and a sum of zero.
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        public static void RecordEvent(string Key, int Count)
        {
            RecordCountlyEvent(Key, Count, null, null);
        }

        /// <summary>
        /// Records a custom event with no segmentation values, and the specified count and sum.
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <param name="Sum">Sum to associate with the event</param>
        public static void RecordEvent(string Key, int Count, double Sum)
        {
            RecordCountlyEvent(Key, Count, Sum, null);
        }

        /// <summary>
        /// Records a custom event with the specified segmentation values and count, and a sum of zero.
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <param name="Segmentation">Segmentation object to associate with the event, can be null</param>
        public static void RecordEvent(string Key, int Count, Segmentation Segmentation)
        {
            RecordCountlyEvent(Key, Count, null, Segmentation);
        }

        /// <summary>
        /// Records a custom event with the specified segmentation values, count and a sum
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <param name="Sum">Sum to associate with the event</param>
        /// <param name="Segmentation">Segmentation object to associate with the event, can be null</param>
        public static void RecordEvent(string Key, int Count, double Sum, Segmentation Segmentation)
        {
            RecordCountlyEvent(Key, Count, Sum, Segmentation);
        }

        /// <summary>
        /// Records a custom event with the specified values
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <param name="Sum">Sum to associate with the event</param>
        /// <param name="Segmentation">Segmentation object to associate with the event, can be null</param>
        private static void RecordCountlyEvent(string Key, int Count, double? Sum, Segmentation Segmentation)
        {
            AddEvent(new CountlyEvent(Key, Count, Sum, Segmentation));
        }

        /// <summary>
        /// Adds event to queue and uploads
        /// </summary>
        /// <param name="countlyEvent">event object</param>
        private static void AddEvent(CountlyEvent countlyEvent)
        {
            if (String.IsNullOrWhiteSpace(ServerUrl))
            {
                throw new InvalidOperationException("session is not active");
            }

            ThreadPool.QueueUserWorkItem((work) =>
            {
                lock (sync)
                {
                    Events.Add(countlyEvent);

                    SaveEvents();
                }

                Upload();
            });
        }

        /// <summary>
        /// Uploads event queue to Countly server
        /// </summary>
        private static void UploadEvents(Action<bool> callback)
        {
            lock (sync)
            {
                // Allow uploading in one thread only
                if (uploadInProgress)
                {
                    callback(true);

                    return;
                }

                uploadInProgress = true;
            }

            int eventsCount;
            
            lock (sync)
            {
                eventsCount = Events.Count;
            }

            if (eventsCount > 0)
            {
                Api.SendEvents(ServerUrl, AppKey, Device.DeviceId, Events.Take(eventsCount).ToList(), (UserDetails.isChanged) ? UserDetails : null, (resultResponse) =>
                {
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

                            SaveEvents();

                            eventsCountToUploadAgain = Events.Count;
                        }

                        if (eventsCountToUploadAgain > 0)
                        {
                            // Upload events added during sync
                            UploadEvents(callback);
                        }
                        else
                        {
                            callback(true);
                        }
                    }
                    else
                    {
                        uploadInProgress = false;

                        callback(false);
                    }
                });
            }
            else
            {
                uploadInProgress = false;

                callback(true);
            }
        }

        /// <summary>
        /// Raised when user details propery is changed
        /// </summary>
        private static void OnUserDetailsChanged()
        {
            UserDetails.isChanged = true;

            SaveUserDetails();

            UploadUserDetails();
        }

        /// <summary>
        /// Uploads user details
        /// </summary>
        /// <returns>true if details are successfully uploaded, false otherwise</returns>
        internal static void UploadUserDetails()
        {
            if (String.IsNullOrWhiteSpace(Countly.ServerUrl))
            {
                throw new InvalidOperationException("session is not active");
            }

            Api.UploadUserDetails(Countly.ServerUrl, Countly.AppKey, Device.DeviceId, UserDetails, (resultResponse) =>
            {
                if (resultResponse != null && resultResponse.IsSuccess)
                {
                    UserDetails.isChanged = false;

                    SaveUserDetails();
                }
            });
        }

        /// <summary>
        /// Uploads user picture. Accepted picture formats are .png, .gif and .jpeg and picture will be resized to maximal 150x150 dimensions
        /// </summary>
        /// <param name="stream">Image stream</param>
        /// <param name="callback">true if image is successfully uploaded, false otherwise</param>
        internal static void UploadUserPicture(Stream imageStream, Action<bool> callback)
        {
            if (String.IsNullOrWhiteSpace(Countly.ServerUrl))
            {
                throw new InvalidOperationException("session is not active");
            }

            Api.UploadUserPicture(Countly.ServerUrl, Countly.AppKey, Device.DeviceId, imageStream, (UserDetails.isChanged) ? UserDetails : null, (resultResponse) =>
            {
                callback((resultResponse != null && resultResponse.IsSuccess));
            });
        }

        /// <summary>
        /// Records exception
        /// </summary>
        /// <param name="error">exception title</param>
        public static void RecordException(string error)
        {
            RecordException(error, null, null);
        }

        /// <summary>
        /// Records exception with stacktrace
        /// </summary>
        /// <param name="error">exception title</param>
        /// <param name="stackTrace">exception stacktrace</param>
        public static void RecordException(string error, string stackTrace)
        {
            RecordException(error, stackTrace, null);
        }

        /// <summary>
        /// Records unhandled exception with stacktrace
        /// </summary>
        /// <param name="error">exception title</param>
        /// <param name="stackTrace">exception stacktrace</param>
        private static void RecordUnhandledException(string error, string stackTrace)
        {
            RecordException(error, stackTrace, null, true);
        }

        /// <summary>
        /// Records exception with stacktrace and custom info
        /// </summary>
        /// <param name="error">exception title</param>
        /// <param name="stackTrace">exception stacktrace</param>
        /// <param name="customInfo">exception custom info</param>
        public static void RecordException(string error, string stackTrace, Dictionary<string, string> customInfo)
        {
            RecordException(error, stackTrace, customInfo, false);
        }

        /// <summary>
        /// Records exception with stacktrace and custom info
        /// </summary>
        /// <param name="error">exception title</param>
        /// <param name="stackTrace">exception stacktrace</param>
        /// <param name="customInfo">exception custom info</param>
        /// <param name="unhandled">bool indicates is exception is fatal or not</param>
        public static void RecordException(string error, string stackTrace, Dictionary<string, string> customInfo, bool unhandled)
        {
            if (String.IsNullOrWhiteSpace(ServerUrl))
            {
                throw new InvalidOperationException("session is not active");
            }
            
            TimeSpan run = DateTime.Now.Subtract(startTime);

            lock (sync)
            {
                Exceptions.Add(new ExceptionEvent(error, stackTrace, unhandled, breadcrumb, run, customInfo));

                SaveExceptions();
            }

            if (!unhandled)
            {
                Upload();
            }
        }

        /// <summary>
        /// Uploads exceptions queue to Countly server
        /// </summary>
        private static void UploadExceptions(Action<bool> callback)
        {
            lock (sync)
            {
                // Allow uploading in one thread only
                if (uploadInProgress)
                {
                    callback(true);

                    return;
                }

                uploadInProgress = true;
            }

            int exceptionsCount;

            lock (sync)
            {
                exceptionsCount = Exceptions.Count;
            }

            if (exceptionsCount > 0)
            {
                Api.SendException(ServerUrl, AppKey, Device.DeviceId, Exceptions[0], (resultResponse) =>
                {
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

                            SaveExceptions();

                            exceptionsCountToUploadAgain = Exceptions.Count;
                        }

                        if (exceptionsCountToUploadAgain > 0)
                        {
                            // Upload next exception
                            UploadExceptions(callback);
                        }
                        else
                        {
                            callback(true);
                        }
                    }
                    else
                    {
                        uploadInProgress = false;

                        callback(false);
                    }
                });
            }
            else
            {
                uploadInProgress = false;

                callback(true);
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
        private static void Upload()
        {
            UploadSessions((sessionsSuccess) =>
            {
                if (sessionsSuccess)
                {
                    UploadEvents((eventsSuccess) =>
                    {
                        if (eventsSuccess)
                        {
                            UploadExceptions((exceptionsSuccess) => { });
                        }
                    });
                }
            });
        }
    }
}
