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
using Windows.UI.Xaml;
using System.IO;
using Newtonsoft.Json;
using Windows.System.Threading;
using System.Diagnostics;
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

        // Raised when the async session is established
        public static event EventHandler SessionStarted;

        // Update session timer
        private ThreadPoolTimer Timer;

        /// <summary>
        /// Determines if exception autotracking is enabled
        /// </summary>
        public static bool IsExceptionsLoggingEnabled { get; set; }

        /// <summary>
        /// Saves collection to the storage
        /// </summary>
        /// <returns>True if success, otherwise - False</returns>
        private async Task<bool> SaveCollection<T>(List<T> collection, string path)
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

        protected override bool SaveEvents()
        {
            lock (sync)
            {
                return SaveCollection<CountlyEvent>(Events, eventsFilename).Result;
            }
        }

        protected override bool SaveSessions()
        {
            lock (sync)
            {
                return SaveCollection<SessionEvent>(Sessions, sessionsFilename).Result;
            }
        }

        protected override bool SaveExceptions()
        {
            lock (sync)
            {
                return SaveCollection<ExceptionEvent>(Exceptions, exceptionsFilename).Result;
            }
        }

        internal override bool SaveUnhandledException(ExceptionEvent exceptionEvent)
        {
            lock (sync)
            {
                string json = JsonConvert.SerializeObject(exceptionEvent, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
                return Storage.Instance.SetValue(unhandledExceptionFilename, json);
            }
        }

        protected override bool SaveUserDetails()
        {
            lock (sync)
            {
                return Storage.Instance.SaveToFile<CountlyUserDetails>(userDetailsFilename, UserDetails).Result;
            }
        }

        /// <summary>
        /// Common function to initiate countly sdk.
        /// Called either from the foreground or the background.
        /// </summary>
        /// <param name="serverUrl">URL of the Countly server to submit data to; use "https://cloud.count.ly" for Countly Cloud</param>
        /// <param name="appKey">app key for the application being tracked; find in the Countly Dashboard under Management > Applications</param>
        private async Task StartSessionCommon(string serverUrl, string appKey, Application application = null, bool calledFromBackground = true)
        {
            Countly.Instance.StartSessionInternal(serverUrl, appKey, application, calledFromBackground);
        }

        public async Task StartSessionInternal(string serverUrl, string appKey, Application application = null, bool calledFromBackground = true)
        {
            if (ServerUrl != null)
            {
                // session already active
                return;
            }

            string appVersion = DeviceData.AppVersion;

            if (application != null)
            {
                IsExceptionsLoggingEnabled = true;

                application.UnhandledException -= OnApplicationUnhandledException;
                application.UnhandledException += OnApplicationUnhandledException;
            }

            if (!IsInitialized())
            {
                CountlyConfig cc = new CountlyConfig() { appKey = appKey, appVersion = appVersion, serverUrl = serverUrl };
                await Init(cc);
            }       

            String unhandledExceptionValue = Storage.Instance.GetValue<string>(unhandledExceptionFilename, "");
            ExceptionEvent unhandledException = JsonConvert.DeserializeObject<ExceptionEvent>(unhandledExceptionValue);
            if(unhandledException != null)
            {
                //add the saved unhandled exception to the other ones
                if (Countly.IsLoggingEnabled)
                {
                    Debug.WriteLine("Found a stored unhandled exception, adding it the the other stored exceptions");
                }
                Exceptions.Add(unhandledException);
                SaveExceptions();
                SaveUnhandledException(null);
            }            

            if (!calledFromBackground)
            {
                startTime = DateTime.Now;

                SessionTimerStart();

                Metrics metrics = new Metrics(DeviceData.OS, DeviceData.OSVersion, DeviceData.DeviceName, DeviceData.Resolution, DeviceData.Carrier, AppVersion, DeviceData.Locale);
                await AddSessionEvent(new BeginSession(AppKey, await DeviceData.GetDeviceId(), sdkVersion, metrics));

                if (null != SessionStarted)
                {
                    SessionStarted(null, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Starts Countly tracking session.
        /// Call from your App.xaml.cs Application_Launching and Application_Activated events.
        /// Must be called before other SDK methods can be used.
        /// </summary>
        /// <param name="serverUrl">URL of the Countly server to submit data to; use "https://cloud.count.ly" for Countly Cloud</param>
        /// <param name="appKey">app key for the application being tracked; find in the Countly Dashboard under Management > Applications</param>
        public static async Task StartSession(string serverUrl, string appKey, Application application = null)
        {
            await Countly.Instance.StartSessionCommon(serverUrl, appKey, application, false);           
        }

        /// <summary>
        /// Starts Countly background tracking session.
        /// Call from your background agent OnInvoke method.
        /// Must be called before other SDK methods can be used.
        /// </summary>
        /// <param name="serverUrl"></param>
        /// <param name="appKey"></param>
        public static async void StartBackgroundSession(string serverUrl, string appKey)
        {
            await Countly.Instance.StartSessionCommon(serverUrl, appKey, null, true);            
        }

        /// <summary>
        /// Sends session duration. Called automatically each <updateInterval> seconds
        /// </summary>
        /// <param name="timer"></param>
        private async void UpdateSession(ThreadPoolTimer timer)
        {
            await UpdateSessionInternal();
        }

        /// <summary>
        /// Raised when application unhandled exception is thrown
        /// </summary>
        /// <param name="sender">sender param</param>
        /// <param name="e">exception details</param>
        private async void OnApplicationUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (IsExceptionsLoggingEnabled)
            {
                // If we access the StackTrace here, it will not be null when it gets recorded.
                string stackTrace = e.Exception.StackTrace ?? string.Empty;
                await RecordUnhandledException(e.Exception.Message, stackTrace);
            }
        }

        protected override void SessionTimerStart()
        {
            Timer = ThreadPoolTimer.CreatePeriodicTimer(UpdateSession, TimeSpan.FromSeconds(updateInterval));
        }

        protected override void SessionTimerStop()
        {
            if (Timer != null)
            {
                Timer.Cancel();
                Timer = null;
            }
        }
    }
}