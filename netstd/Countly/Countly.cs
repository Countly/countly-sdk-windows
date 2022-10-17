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

using CountlySDK.CountlyCommon;
using CountlySDK.Entities;
using CountlySDK.Entities.EntityBase;
using CountlySDK.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CountlySDK.Helpers.TimeHelper;
//[assembly: InternalsVisibleTo("CountlyTest_461")]
//[assembly: InternalsVisibleTo("CountlySampleUWP")]

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

        //methods for generating device ID
        public enum DeviceIdMethod { windowsGUID = DeviceBase.DeviceIdMethodInternal.windowsGUID, developerSupplied = DeviceBase.DeviceIdMethodInternal.developerSupplied };

        // Raised when the async session is established
        public static event EventHandler SessionStarted;

        // Update session timer
        private TimerHelper Timer;

        public override string sdkName()
        {
            return "csharp-netstd";
        }

        /// <summary>
        /// Saves collection to the storage
        /// </summary>
        /// <returns>True if success, otherwise - False</returns>
        private async Task<bool> SaveCollection<T>(List<T> collection, string path)
        {
            List<T> collection_;

            lock (sync) {
                collection_ = collection.ToList();
            }

            bool success = await Storage.Instance.SaveToFile<List<T>>(path, collection_);

            if (success) {
                if (collection_.Count != collection.Count) {
                    // collection was changed during saving, save it again
                    return await SaveCollection<T>(collection, path);
                } else {
                    return true;
                }
            } else {
                return false;
            }
        }

        protected override bool SaveEvents()
        {
            lock (sync) {
                return SaveCollection<CountlyEvent>(Events, eventsFilename).Result;
            }
        }

        protected override bool SaveSessions()
        {
            lock (sync) {
                return SaveCollection<SessionEvent>(Sessions, sessionsFilename).Result;
            }
        }

        protected override bool SaveExceptions()
        {
            lock (sync) {
                return SaveCollection<ExceptionEvent>(Exceptions, exceptionsFilename).Result;
            }
        }

        internal override bool SaveUnhandledException(ExceptionEvent exceptionEvent)
        {
            lock (sync) {
                //for now we treat unhandled exceptions just like regular exceptions
                Exceptions.Add(exceptionEvent);
                return SaveExceptions();
            }
        }

        protected override bool SaveUserDetails()
        {
            lock (sync) {
                return Storage.Instance.SaveToFile<CountlyUserDetails>(userDetailsFilename, UserDetails).Result;
            }
        }

        public override async Task Init(CountlyConfig config)
        {
            if (IsInitialized()) { return; }

            if (config == null) {
                UtilityHelper.CountlyLogging("Configuration object can not be null while initializing Countly");
            }

            await InitBase(config);
        }

        protected override async Task SessionBeginInternal()
        {
            startTime = DateTime.Now;
            lastSessionUpdateTime = startTime;
            SessionTimerStart();
            SessionStarted?.Invoke(null, EventArgs.Empty);

            TimeInstant timeInstant = timeHelper.GetUniqueInstant();
            Metrics metrics = new Metrics(DeviceData.OS, null, null, null, null, AppVersion, DeviceData.Locale);
            await AddSessionEvent(new BeginSession(AppKey, await DeviceData.GetDeviceId(), sdkVersion, metrics, sdkName(), timeInstant));
        }

        /// <summary>
        /// Sends session duration. Called automatically each <updateInterval> seconds
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UpdateSession(object sender, object e)
        {
            await UpdateSessionInternal();
        }

        protected override void SessionTimerStart()
        {
            Timer = new TimerHelper(UpdateSession, null, sessionUpdateInterval * 1000, sessionUpdateInterval * 1000);
        }

        protected override void SessionTimerStop()
        {
            if (Timer != null) {
                Timer.Dispose();
                Timer = null;
            }
        }
    }
}
