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

using System;
using System.Globalization;

namespace CountlySDK.Helpers
{
    internal class TimeHelper
    {

        internal class TimeInstant
        {
            public int Dow { get; private set; }
            public int Hour { get; private set; }
            public string Timezone { get; private set; }
            public long Timestamp { get; private set; }

            internal TimeInstant() { }
            internal TimeInstant(long timestampInMillis, int hour, int dow, string timezone)
            {
                Dow = dow;
                Hour = hour;
                Timezone = timezone;
                Timestamp = timestampInMillis;
            }

            internal static TimeInstant Get(long timestampInMillis)
            {
                if (timestampInMillis < 0L) {
                    timestampInMillis = 0;
                    UtilityHelper.CountlyLogging("[TimeInstant][Get] Provided timestamp was less than 0. Value was overridden to 0.");
                }

                TimeSpan time = TimeSpan.FromMilliseconds(timestampInMillis);
                DateTime dateTime = new DateTime(1970, 1, 1) + time;

                long timestamp = timestampInMillis;
                int dow = (int)dateTime.DayOfWeek;
                int hour = dateTime.TimeOfDay.Hours;
                string timezone = TimeZoneInfo.Local.GetUtcOffset(dateTime).TotalMinutes.ToString(CultureInfo.InvariantCulture);

                return new TimeInstant(timestamp, hour, dow, timezone);
            }
        }

        //variable to hold last used timestamp
        private long _lastMilliSecTimeStamp = 0;

        internal TimeHelper() { }

        /// <summary>
        /// Converts DateTime to Unix time format
        /// </summary>
        /// <param name="date">DateTime object</param>
        /// <returns>Unix timestamp</returns>
        public long ToUnixTime(DateTime date)
        {
            TimeSpan ts = date.Subtract(new DateTime(1970, 1, 1));
            long calculatedMillis = (long)ts.TotalMilliseconds;

            return calculatedMillis;
        }

        public long GetUniqueUnixTime()
        {
            long calculatedMillis = ToUnixTime(DateTime.Now.ToUniversalTime());

            if (_lastMilliSecTimeStamp >= calculatedMillis) {
                ++_lastMilliSecTimeStamp;
            } else {
                _lastMilliSecTimeStamp = calculatedMillis;
            }

            return _lastMilliSecTimeStamp;
        }

        public TimeInstant GetUniqueInstant()
        {
            long currentTimestamp = GetUniqueUnixTime();
            TimeInstant timeInstant = TimeInstant.Get(currentTimestamp);
            return timeInstant;
        }
    }
}
