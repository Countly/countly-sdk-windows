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
using CountlySDK.CountlyCommon;

namespace CountlySDK
{
    /// <summary>
    /// This class is the public API for the Countly Windows Phone SDK.
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

            bool success = await Storage.Instance.SaveToFile<List<T>>(path, collection_);

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
            await Storage.Instance.SaveToFile<CountlyUserDetails>(userDetailsFilename, UserDetails);
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

            Storage.Instance.fileSystem = fileSystem;

            Events = await Storage.Instance.LoadFromFile<List<CountlyEvent>>(eventsFilename) ?? new List<CountlyEvent>();

            Sessions = await Storage.Instance.LoadFromFile<List<SessionEvent>>(sessionsFilename) ?? new List<SessionEvent>();

            Exceptions = await Storage.Instance.LoadFromFile<List<ExceptionEvent>>(exceptionsFilename) ?? new List<ExceptionEvent>();

            UserDetails = await Storage.Instance.LoadFromFile<CountlyUserDetails>(userDetailsFilename) ?? new CountlyUserDetails();

            UserDetails.UserDetailsChanged += OnUserDetailsChanged;

            startTime = DateTime.Now;

            Timer = new TimerHelper(UpdateSession, null, updateInterval * 1000, updateInterval * 1000);

            await AddSessionEvent(new BeginSession(AppKey, await DeviceData.GetDeviceId(), sdkVersion, new Metrics(DeviceData.OS, null, null, null, null, appVersion)));

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

            await Storage.Instance.DeleteFile(eventsFilename);
            await Storage.Instance.DeleteFile(sessionsFilename);
            await Storage.Instance.DeleteFile(exceptionsFilename);
            await Storage.Instance.DeleteFile(userDetailsFilename);
        }

        /// <summary>
        /// Adds log breadcrumb
        /// </summary>
        /// <param name="log">log string</param>
        public static void AddBreadCrumb(string log)
        {
            breadcrumb += log + "\r\n";
        }

        public static async Task<String> GetDeviceId()
        {
            return await DeviceData.GetDeviceId();
        }
    }
}
