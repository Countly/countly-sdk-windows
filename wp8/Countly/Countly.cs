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
using CountlySDK.Entitites;
using CountlySDK.Helpers;
using CountlySDK.Server.Responses;

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
        private const int updateInterval = 20;

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

        // Start session timestamp
        private static DateTime startTime;
        // Update session timer
        private static DispatcherTimer Timer;

        public static bool IsLoggingEnabled { get; set; }

        /// <summary>
        /// Starts Countly tracking session.
        /// Call from your App.xaml.cs Application_Launching and Application_Activated events.
        /// Must be called before other SDK methods can be used.
        /// </summary>
        /// <param name="serverUrl">URL of the Countly server to submit data to; use "https://cloud.count.ly" for Countly Cloud</param>
        /// <param name="appKey">app key for the application being tracked; find in the Countly Dashboard under Management > Applications</param>
        public static void StartSession(string serverUrl, string appKey)
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

            AddSessionEvent(new EndSession(AppKey, Device.DeviceId), false);
        }

        /// <summary>
        /// Immediately disables session & event tracking and clears any stored session & event data.
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

                Storage.DeleteFile(eventsFilename);
                Storage.DeleteFile(sessionsFilename);
            }
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

            ThreadPool.QueueUserWorkItem(async (work) =>
            {
                lock (sync)
                {
                    Sessions.Add(sessionEvent);

                    SaveSessions();
                }

                if (uploadImmediately)
                {
                    await Upload();
                }
            });
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
                ResultResponse resultResponse = await Api.SendSession(ServerUrl, sessionEvent);

                if (resultResponse != null && resultResponse.IsSuccess)
                {
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
        /// <returns>True if event is uploaded successfully, otherwise - False</returns>
        public static Task<bool> RecordEvent(string Key)
        {
            return RecordCountlyEvent(Key, 1, null, null);
        }

        /// <summary>
        /// Records a custom event with no segmentation values, the specified count, and a sum of zero.
        /// </summary>
        /// <param name="Key">Name of the custom event, required, must not be the empty string</param>
        /// <param name="Count">Count to associate with the event, should be more than zero</param>
        /// <returns>True if event is uploaded successfully, otherwise - False</returns>
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
        /// <returns>True if event is uploaded successfully, otherwise - False</returns>
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
        /// <returns>True if event is uploaded successfully, otherwise - False</returns>
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
        /// <returns>True if event is uploaded successfully, otherwise - False</returns>
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
        /// <returns>True if event is uploaded successfully, otherwise - False</returns>
        private static Task<bool> RecordCountlyEvent(string Key, int Count, double? Sum, Segmentation Segmentation)
        {
            return AddEvent(new CountlyEvent(Key, Count, Sum, Segmentation));
        }

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
        /// Adds event to queue and uploads
        /// </summary>
        /// <param name="countlyEvent">event object</param>
        /// <returns>True if success</returns>
        private async static Task<bool> AddEvent(CountlyEvent countlyEvent)
        {
            if (String.IsNullOrWhiteSpace(ServerUrl))
            {
                throw new InvalidOperationException("session is not active");
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
                eventsCount = Events.Count;
            }

            if (eventsCount > 0)
            {
                ResultResponse resultResponse = await Api.SendEvents(ServerUrl, AppKey, Device.DeviceId, Events.Take(eventsCount).ToList());

                if (resultResponse != null && resultResponse.IsSuccess)
                {
                    int eventsCountToUploadAgain = 0;

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

                        Storage.SaveToFile(eventsFilename, Events);

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

                return false;
            }
        }

        /// <summary>
        /// Upload sessions and events queues
        /// </summary>
        /// <returns>True if success</returns>
        private static async Task<bool> Upload()
        {
            bool success = await UploadSessions();

            if (success)
            {
                success = await UploadEvents();
            }

            return success;
        }
    }
}
