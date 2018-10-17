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

        //methods for generating device ID
        public enum DeviceIdMethod { cpuId = DeviceBase.DeviceIdMethodInternal.cpuId, multipleFields = DeviceBase.DeviceIdMethodInternal.multipleWindowsFields };

        // Update session timer
        private static DispatcherTimer Timer;


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
    }  
}