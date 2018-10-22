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
        public enum DeviceIdMethod { cpuId = DeviceBase.DeviceIdMethodInternal.cpuId, multipleFields = DeviceBase.DeviceIdMethodInternal.multipleWindowsFields, windowsGUID = DeviceBase.DeviceIdMethodInternal.windowsGUID, developerSupplied = DeviceBase.DeviceIdMethodInternal.developerSupplied };
        
        // Update session timer
        private DispatcherTimer Timer;

        protected override bool SaveEvents()
        {
            lock (sync)
            {
                return Storage.Instance.SaveToFile<List<SessionEvent>>(eventsFilename, Events).Result;
            }
        }

        protected override bool SaveSessions()
        {
            lock (sync)
            {
                return Storage.Instance.SaveToFile<List<SessionEvent>>(sessionsFilename, Sessions).Result;
            }
        }

        protected override bool SaveExceptions()
        {
            lock (sync)
            {
                return Storage.Instance.SaveToFile<List<ExceptionEvent>>(exceptionsFilename, Exceptions).Result;
            }
        }

        internal override bool SaveUnhandledException(ExceptionEvent exceptionEvent)
        {
            lock (sync)
            {
                //for now we treat unhandled exceptions just like regular exceptions
                Exceptions.Add(exceptionEvent);
                return SaveExceptions();
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
        /// Starts Countly tracking session.
        /// Call from your entry point.
        /// Must be called before other SDK methods can be used.
        /// </summary>
        /// <param name="serverUrl">URL of the Countly server to submit data to; use "https://cloud.count.ly" for Countly Cloud</param>
        /// <param name="appKey">app key for the application being tracked; find in the Countly Dashboard under Management > Applications</param>
        /// <param name="appVersion">Application version</param>
        [Obsolete("static 'StartSession' is deprecated, please use 'Countly.Instance.Init' together with 'Countly.Instance.SessionBegin' in place of this call")]
        public static async Task StartSession(string serverUrl, string appKey, string appVersion, DeviceIdMethod idMethod = DeviceIdMethod.cpuId)
        {
            Countly.Instance.StartSessionInternal(serverUrl, appKey, appVersion, idMethod);
        }

        private async Task StartSessionInternal(string serverUrl, string appKey, string appVersion, DeviceIdMethod idMethod = DeviceIdMethod.cpuId)
        {
            if (ServerUrl != null)
            {
                // session already active
                return;
            }

            if (!IsInitialized())
            {
                CountlyConfig cc = new CountlyConfig() { appKey = appKey, appVersion = appVersion, serverUrl = serverUrl, deviceIdMethod = idMethod };
                await Init(cc);
            }

            await SessionBeginInternal();
        }

        public override async Task Init(CountlyConfig config)
        {
            if (IsInitialized()) { return; }

            if (config == null) { throw new InvalidOperationException("Configuration object can not be null while initializing Conutly"); }

            await InitBase(config);            
        }

        protected override async Task SessionBeginInternal()
        {
            startTime = DateTime.Now;
            lastSessionUpdateTime = startTime;
            SessionTimerStart();

            Metrics metrics = new Metrics(DeviceData.OS, DeviceData.OSVersion, DeviceData.DeviceName, DeviceData.Resolution, null, AppVersion, DeviceData.Locale);
            await AddSessionEvent(new BeginSession(AppKey, await DeviceData.GetDeviceId(), sdkVersion, metrics));
        }

        /// <summary>
        /// Sends session duration. Called automatically each <updateInterval> seconds
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UpdateSession(object sender, EventArgs e)
        {
            await UpdateSessionInternal();            
        }

        /// <summary>
        /// Set the custom data path for temporary caching files
        /// Set it to null if you want to use the default location
        /// THIS WILL ONLY WORK WHEN TARGETING .NET3.5
        /// If you downloaded this package from nuget and are targeting .net4.0,
        /// this will do nothing.
        /// </summary>
        /// <param name="customPath">Custom location for countly data files</param>
        [Obsolete("static 'SetCustomDataPath' is deprecated, please set the value 'customDataPath' in 'CountlyConfig' while initiating the SDK")]
        public static void SetCustomDataPath(string customPath)
        {
            Storage.Instance.SetCustomDataPath(customPath);
        }

        protected override void SessionTimerStart()
        {
            Timer = new DispatcherTimer();
            Timer.Interval = TimeSpan.FromSeconds(updateInterval);
            Timer.Tick += UpdateSession;
            Timer.Start();
        }

        protected override void SessionTimerStop()
        {
            if (Timer != null)
            {
                Timer.Stop();
                Timer.Tick -= UpdateSession;
                Timer = null;
            }
        }
    }
}