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
using System.Windows.Threading;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using CountlySDK.Server.Responses;
using System.IO;
using System.Diagnostics;
using static CountlySDK.Entities.EntityBase.DeviceBase;
using CountlySDK.Entities.EntityBase;
using CountlySDK.CountlyCommon;

namespace CountlySDK
{
    /// <summary>
    /// This class is the public API for the Countly .NET 3.5/4.0 SDK.
    /// </summary>
    public class Countly : CountlyBase
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly Countly instance = new Countly();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static Countly() { }
        internal Countly() { }
        public static Countly Instance { get { return instance; } }
        //-------------SINGLETON-----------------


        // File that stores events objects
        private const string eventsFilename = "events.xml";
        // File that stores sessions objects
        private const string sessionsFilename = "sessions.xml";
        // File that stores exceptions objects
        private const string exceptionsFilename = "exceptions.xml";
        // File that stores user details object
        private const string userDetailsFilename = "userdetails.xml";

        //methods for generating device ID
        public enum DeviceIdMethod { cpuId = DeviceBase.DeviceIdMethodInternal.cpuId, multipleFields = DeviceBase.DeviceIdMethodInternal.multipleWindowsFields };

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
                        events = Storage.Instance.LoadFromFile<List<CountlyEvent>>(eventsFilename).Result;

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
                        sessions = Storage.Instance.LoadFromFile<List<SessionEvent>>(sessionsFilename).Result;

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
                        exceptions = Storage.Instance.LoadFromFile<List<ExceptionEvent>>(exceptionsFilename).Result;

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
                        userDetails = Storage.Instance.LoadFromFile<CountlyUserDetails>(userDetailsFilename).Result;

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

        // Update session timer
        private static DispatcherTimer Timer;
        //holds device info
        private static Device DeviceData = new Device();

        /// <summary>
        /// Determines if Countly debug messages are displayed to Output window
        /// </summary>
        public static bool IsLoggingEnabled { get; set; }

        /// <summary>
        /// Saves events to the storage
        /// </summary>
        private static void SaveEvents()
        {
            lock (sync)
            {
                var res = Storage.Instance.SaveToFile<List<SessionEvent>>(eventsFilename, Events).Result;
            }
        }

        /// <summary>
        /// Saves sessions to the storage
        /// </summary>
        private static void SaveSessions()
        {
            lock (sync)
            {
                var res = Storage.Instance.SaveToFile<List<SessionEvent>>(sessionsFilename, Sessions).Result;
            }
        }

        /// <summary>
        /// Saves exceptions to the storage
        /// </summary>
        private static void SaveExceptions()
        {
            lock (sync)
            {
                var res = Storage.Instance.SaveToFile<List<ExceptionEvent>>(exceptionsFilename, Exceptions).Result;
            }
        }

        /// <summary>
        /// Saves user details info to the storage
        /// </summary>
        private static void SaveUserDetails()
        {
            lock (sync)
            {
                var res = Storage.Instance.SaveToFile<CountlyUserDetails>(userDetailsFilename, UserDetails).Result;
            }
        }       

        /// <summary>
        /// Starts Countly tracking session.
        /// Call from your entry point.
        /// Must be called before other SDK methods can be used.
        /// </summary>
        /// <param name="serverUrl">URL of the Countly server to submit data to; use "https://cloud.count.ly" for Countly Cloud</param>
        /// <param name="appKey">app key for the application being tracked; find in the Countly Dashboard under Management > Applications</param>
        /// <param name="appVersion">Application version</param>
        public static async Task StartSession(string serverUrl, string appKey, string appVersion, DeviceIdMethod idMethod = DeviceIdMethod.cpuId)
        {
            if (String.IsNullOrEmpty(serverUrl))
            {
                throw new ArgumentException("invalid server url");
            }

            if (String.IsNullOrEmpty(appKey))
            {
                throw new ArgumentException("invalid application key");
            }

            ServerUrl = serverUrl;
            AppKey = appKey;
            AppVersion = appVersion;

            DeviceData.SetPreferredDeviceIdMethod((DeviceIdMethodInternal) idMethod);

            startTime = DateTime.Now;

            Timer = new DispatcherTimer();
            Timer.Interval = TimeSpan.FromSeconds(updateInterval);
            Timer.Tick += UpdateSession;
            Timer.Start();

            await AddSessionEvent(new BeginSession(AppKey, await DeviceData.GetDeviceId(), sdkVersion, new Metrics(DeviceData.OS, DeviceData.OSVersion, DeviceData.DeviceName, DeviceData.Resolution, null, appVersion)));
        }

        /// <summary>
        /// Sends session duration. Called automatically each <updateInterval> seconds
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void UpdateSession(object sender, EventArgs e)
        {
            await AddSessionEvent(new UpdateSession(AppKey, await DeviceData.GetDeviceId(), (int)DateTime.Now.Subtract(startTime).TotalSeconds));
        }

        /// <summary>
        /// End Countly tracking session.
        /// Call from your closing event.
        /// </summary>
        public static async Task EndSession()
        {
            if (Timer != null)
            {
                Timer.Stop();
                Timer.Tick -= UpdateSession;
                Timer = null;
            }

            await AddSessionEvent(new EndSession(AppKey, await DeviceData.GetDeviceId()), true);
        }

        /// <summary>
        ///  Adds session event to queue and uploads
        /// </summary>
        /// <param name="sessionEvent">session event object</param>
        /// <param name="uploadImmediately">indicates when start to upload, by default - immediately after event was added</param>
        private static async Task AddSessionEvent(SessionEvent sessionEvent, bool uploadImmediately = true)
        {
            try
            {
                if (String.IsNullOrEmpty(ServerUrl))
                {
                    return;
                }

                lock (sync)
                {
                    Sessions.Add(sessionEvent);

                    SaveSessions();
                }

                if (uploadImmediately)
                {
                    await Upload();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
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
                    lock (sync)
                    {
                        UserDetails.isChanged = false;
                    }

                    SaveUserDetails();
                    int sessionCount = 0;

                    lock (sync)
                    {
                        uploadInProgress = false;

                        try
                        {
                            Sessions.RemoveAt(0);
                        }
                        catch { }

                        var res = Storage.Instance.SaveToFile<List<SessionEvent>>(sessionsFilename, Sessions).Result;
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

                Storage.Instance.DeleteFile(eventsFilename).RunSynchronously();
                Storage.Instance.DeleteFile(sessionsFilename).RunSynchronously();
                Storage.Instance.DeleteFile(exceptionsFilename).RunSynchronously();
                Storage.Instance.DeleteFile(userDetailsFilename).RunSynchronously();
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
        private static async void RecordCountlyEvent(string Key, int Count, double? Sum, Segmentation Segmentation)
        {
            await AddEvent(new CountlyEvent(Key, Count, Sum, Segmentation));
        }

        /// <summary>
        /// Adds event to queue and uploads
        /// </summary>
        /// <param name="countlyEvent">event object</param>
        /// <returns>True if success</returns>
        private async static Task<bool> AddEvent(CountlyEvent countlyEvent)
        {
            if (String.IsNullOrEmpty(ServerUrl))
            {
                return false;
            }

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            ThreadPool.QueueUserWorkItem(async (work) =>
            {
                lock (sync)
                {
                    Events.Add(countlyEvent);

                    SaveEvents();
                }

                bool success = await Upload();

                tcs.SetResult(success);
            });

            return await tcs.Task;
        }

        /// <summary>
        /// Set the custom data path for temporary caching files
        /// Set it to null if you want to use the default location
        /// THIS WILL ONLY WORK WHEN TARGETING .NET3.5
        /// If you downloaded this package from nuget and are targeting .net4.0,
        /// this will do nothing.
        /// </summary>
        /// <param name="customPath">Custom location for countly data files</param>
        public static void SetCustomDataPath(string customPath)
        {
            Storage.Instance.SetCustomDataPath(customPath);
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

            SaveUserDetails();

            await UploadUserDetails();
        }

        /// <summary>
        /// Uploads user details
        /// </summary>
        /// <returns>true if details are successfully uploaded, false otherwise</returns>
        internal static async Task<bool> UploadUserDetails()
        {
            if (String.IsNullOrEmpty(Countly.ServerUrl))
            {
                return false;
            }

            ResultResponse resultResponse = await Api.UploadUserDetails(Countly.ServerUrl, Countly.AppKey, await DeviceData.GetDeviceId(), UserDetails);

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
        /// Uploads user picture. Accepted picture formats are .png, .gif and .jpeg and picture will be resized to maximal 150x150 dimensions
        /// </summary>
        /// <param name="stream">Image stream</param>
        /// <returns>true if image is successfully uploaded, false otherwise</returns>
        internal static async Task<bool> UploadUserPicture(Stream imageStream)
        {
            if (String.IsNullOrEmpty(Countly.ServerUrl))
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
        /// <returns>True if exception successfully uploaded, False - queued for delayed upload</returns>
        public static async void RecordException(string error, string stackTrace, Dictionary<string, string> customInfo, bool unhandled)
        {
            if (String.IsNullOrEmpty(ServerUrl))
            {
                return;
            }

            TimeSpan run = DateTime.Now.Subtract(startTime);

            lock (sync)
            {
                Exceptions.Add(new ExceptionEvent(error, stackTrace, unhandled, breadcrumb, run, AppVersion, customInfo, DeviceData));

                SaveExceptions();
            }

            if (!unhandled)
            {
                await Upload();
            }
            else
            {
                return;
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

                        SaveExceptions();

                        exceptionsCountToUploadAgain = Exceptions.Count;
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

        public static async Task<String> GetDeviceId()
        {
            if (String.IsNullOrEmpty(ServerUrl))
            {
                if (Countly.IsLoggingEnabled)
                {
                    Debug.WriteLine("GetDeviceId cannot be called before StartingSession");
                }
                return "";
            }

            return await DeviceData.GetDeviceId();
        }        
    }  
}
