﻿/*
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
using System.Collections.Generic;
//using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using CountlySDK.CountlyCommon;
using CountlySDK.Entities;
using CountlySDK.Entities.EntityBase;
using CountlySDK.Helpers;

#if RUNNING_ON_35
//[assembly: InternalsVisibleTo("CountlyTest_35")]
#elif RUNNING_ON_40
//[assembly: InternalsVisibleTo("CountlyTest_45")]
#endif

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

        public override string sdkName()
        {
#if RUNNING_ON_35
            return "csharp-net35";
#elif RUNNING_ON_40
            return "csharp-net45";
#endif
        }

        protected override bool SaveEvents()
        {
            lock (sync) {
                return Storage.Instance.SaveToFile<List<SessionEvent>>(eventsFilename, Events).Result;
            }
        }

        protected override bool SaveSessions()
        {
            lock (sync) {
                return Storage.Instance.SaveToFile<List<SessionEvent>>(sessionsFilename, Sessions).Result;
            }
        }

        protected override bool SaveExceptions()
        {
            lock (sync) {
                return Storage.Instance.SaveToFile<List<ExceptionEvent>>(exceptionsFilename, Exceptions).Result;
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
                UtilityHelper.CountlyLogging("[Countly] Init, object can not be null while initializing Countly");
                return;
            }

            if (config.deviceIdMethod == DeviceIdMethod.multipleFields || config.deviceIdMethod == DeviceIdMethod.cpuId) {
                UtilityHelper.CountlyLogging("[Countly] Init, multipleFields and cpuId are deprecated, please use windowsGUID");
            }

            await InitBase(config);
        }

        internal override Metrics GetSessionMetrics()
        {
            Metrics metrics = new Metrics(DeviceData.OS, DeviceData.OSVersion, DeviceData.DeviceName, DeviceData.Resolution, null, AppVersion, DeviceData.Locale);
            metrics.SetMetricOverride(Configuration.MetricOverride);
            return metrics;
        }

        internal override void InformSessionEvent()
        {

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

        protected override void SessionTimerStart()
        {
            Timer = new DispatcherTimer();
            Timer.Interval = TimeSpan.FromSeconds(sessionUpdateInterval);
            Timer.Tick += UpdateSession;
            Timer.Start();
        }

        protected override void SessionTimerStop()
        {
            if (Timer != null) {
                Timer.Stop();
                Timer.Tick -= UpdateSession;
                Timer = null;
            }
        }

        [Obsolete("This function is deprecated")]
        public String GenerateDeviceIdMultipleFields()
        {
            return DeviceIdHelper.GenerateId();
        }
    }
}
