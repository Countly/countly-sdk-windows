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
            public int Timezone { get; private set; }
            public long Timestamp { get; private set; }

            

            internal TimeInstant()
            {
                DateTime dateTime = DateTime.Now;

                Dow = (int)dateTime.DayOfWeek;
                Hour = dateTime.TimeOfDay.Hours;

                TimeSpan ts = dateTime.Subtract(new DateTime(1970, 1, 1));
                Timestamp = (long)ts.TotalMilliseconds;

                Timezone = (int)TimeZoneInfo.Local.GetUtcOffset(dateTime).TotalMinutes;
            }

            internal TimeInstant(long timestampInMillis, int hour, int dow)
            {
                Dow = dow;
                Hour = hour;
                Timestamp = timestampInMillis;
            }

            internal static TimeInstant Get(long timestampInMillis)
            {
                if (timestampInMillis < 0L) {
                    throw new ArgumentException("timestampInMillis must be greater than or equal to zero");
                }

                TimeSpan time = TimeSpan.FromMilliseconds(timestampInMillis);
                DateTime dateTime = new DateTime(1970, 1, 1) + time;

                long timestamp = timestampInMillis;
                int dow = (int)dateTime.DayOfWeek;
                int hour = dateTime.TimeOfDay.Hours;

                return new TimeInstant(timestamp, hour, dow);
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
            long calculatedMillis =  ToUnixTime(DateTime.Now.ToUniversalTime());

            if (_lastMilliSecTimeStamp >= calculatedMillis) {
                ++_lastMilliSecTimeStamp;
            } else {
                _lastMilliSecTimeStamp = calculatedMillis;
            }

            return _lastMilliSecTimeStamp;
        }
    }
}
