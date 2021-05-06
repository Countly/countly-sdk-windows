/*
Copyright (c) 2012, 2013, 2014, 2015, 2016, 2017 Countly

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

using CountlySDK.Helpers;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace CountlySDK.Entities
{
    [DataContractAttribute]
    internal class BeginSession : SessionEvent
    {
        /// <summary>
        /// Creates BeginSession object with provided values
        /// </summary>
        /// <param name="appKey">App key for the application being tracked; find in the Countly Dashboard under Management > Applications</param>
        /// <param name="deviceId">Unique ID for the device the app is running on</param>
        /// <param name="sdkVersion">SDK version string</param>
        /// <param name="metrics">Metrics parameters</param>
        public BeginSession(string appKey, string deviceId, string sdkVersion, Metrics metrics, string sdkName, long? timestamp = null)
        {
            DateTime dateTime = DateTime.Now.ToUniversalTime();
            timestamp = TimeHelper.ToUnixTime(dateTime);

            int hour = dateTime.TimeOfDay.Hours;
            int dayOfWeek = (int)dateTime.DayOfWeek;
            string timezone = TimeZoneInfo.Local.GetUtcOffset(dateTime).TotalMinutes.ToString(CultureInfo.InvariantCulture);
            string metricsString = UtilityHelper.EncodeDataForURL(metrics.ToString());
            Content = String.Format("/i?app_key={0}&device_id={1}&sdk_version={2}&begin_session=1&metrics={3}&timestamp={4}&sdk_name={5}&hour={6}&dow={7}&tz={8}", appKey, deviceId, sdkVersion, metricsString, timestamp, sdkName, hour, dayOfWeek, timezone);
        }

        /// <summary>
        /// Needed for JSON deserialization
        /// </summary>
        [JsonConstructor]
        private BeginSession()
        { }
    }
}
