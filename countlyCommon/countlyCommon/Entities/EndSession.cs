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
    internal class EndSession : SessionEvent
    {
        /// <summary>
        /// Creates EndSession object with provided values
        /// </summary>
        /// <param name="appKey">App key for the application being tracked; find in the Countly Dashboard under Management > Applications</param>
        /// <param name="deviceId">Unique ID for the device the app is running on</param>
        public EndSession(string appKey, string deviceId, string sdkVersion, string sdkName, long? timestamp = null, long? duration = null)
        {
            DateTime dateTime = DateTime.Now; ;
            if (timestamp == null)
            {
                timestamp = TimeHelper.ToUnixTime(dateTime);
            }

            int hour = dateTime.TimeOfDay.Hours;
            int dayOfWeek = (int)dateTime.DayOfWeek;
            string timezone = TimeZoneInfo.Local.GetUtcOffset(dateTime).TotalMinutes.ToString(CultureInfo.InvariantCulture);

            String durationAddition = "";
            if (duration != null && duration > 0)
            {
                duration = Math.Min(duration.Value, 60);
                durationAddition = String.Format("&session_duration={0}", duration.Value);
            }

            Content = String.Format("/i?app_key={0}&device_id={1}&end_session=1&timestamp={2}&sdk_version={3}&sdk_name={4}&hour={5}&dow={6}&tz={7}{8}", appKey, deviceId, timestamp, sdkVersion, sdkName, hour, dayOfWeek, timezone, durationAddition);
        }

        [JsonConstructor]
        public EndSession() { }
    }
}
