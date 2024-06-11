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

//#define TRACE
//#define DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace CountlySDK.Helpers
{
    internal class UtilityHelper
    {
        /// <summary>
        /// Indicates whether the given value is null, empty or consists only of white space characters 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsNullOrEmptyOrWhiteSpace(String value)
        {
            if (String.IsNullOrEmpty(value)) {
                return true;
            }

            bool containsOnlyWhitespace = true;

            foreach (char c in value) {
                if (!char.IsWhiteSpace(c)) {
                    containsOnlyWhitespace = false;
                }
            }

            return containsOnlyWhitespace;
        }

        public static String EncodeDataForURL(String data)
        {
            String escapedString = System.Uri.EscapeDataString(data);
            return escapedString;
        }

        public static String DecodeDataForURL(String data)
        {
            String unescapedString = System.Uri.UnescapeDataString(data);
            return unescapedString;
        }

        public static void CountlyLogging(String msg, LogLevel level = LogLevel.DEBUG)
        {
            if (Countly.IsLoggingEnabled) {
                StringBuilder fullMessage = new StringBuilder(msg.Length + 10);

                if (level == LogLevel.VERBOSE) {
                    fullMessage.Append("[VERBOSE] ");
                } else if (level == LogLevel.DEBUG) {
                    fullMessage.Append("[DEBUG] ");
                } else if (level == LogLevel.INFO) {
                    fullMessage.Append("[INFO] ");
                } else if (level == LogLevel.WARNING) {
                    fullMessage.Append("[WARNING] ");
                } else if (level == LogLevel.ERROR) {
                    fullMessage.Append("[ERROR] ");
                } else {
                    fullMessage.Append("[OTHER] ");
                }

                fullMessage.Append(msg);
                System.Diagnostics.Debug.WriteLine(fullMessage.ToString());
            }
        }

        public static int CompareQueues<T>(Queue<T> first, Queue<T> second) where T : IComparable<T>
        {
            if (first == null && second == null) {
                return 0;
            }
            if (first == null) { return -1; }
            if (second == null) { return 1; }

            List<T> firstA = new List<T>(first.ToArray());
            List<T> secondA = new List<T>(second.ToArray());
            return CompareLists(firstA, secondA);
        }

        public static int CompareLists<T>(List<T> first, List<T> second) where T : IComparable<T>
        {
            if (first == null && second == null) {
                return 0;
            }
            if (first == null) { return -1; }
            if (second == null) { return 1; }

            if (first.Count > second.Count) { return 1; }
            if (first.Count < second.Count) { return -1; }

            for (int a = 0; a < first.Count; a++) {
                if (!first[a].Equals(second[a])) {
                    return first[a].CompareTo(second[a]);
                }
            }

            return 0;
        }


        /// <summary>
        /// Create a stream from given string
        /// Used when sending data to server
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static Stream GenerateStreamFromString(string streamData)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(streamData);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        internal static string TrimKey(string k, int maxKeyLength)
        {
            if (k.Length > maxKeyLength) {
                UtilityHelper.CountlyLogging("[UtilityHelper] TrimKey : Max allowed key length is " + maxKeyLength + ". " + k + " will be truncated.");
                k = k.Substring(0, maxKeyLength);
            }

            return k;
        }

        internal static string[] TrimValues(string[] values, int maxValueSize)
        {
            for (int i = 0; i < values.Length; ++i) {
                if (values[i].Length > maxValueSize) {
                    UtilityHelper.CountlyLogging("[UtilityHelper] TrimValues : Max allowed value length is " + maxValueSize + ". " + values[i] + " will be truncated.");
                    values[i] = values[i].Substring(0, maxValueSize);
                }
            }


            return values;
        }

        internal static string TrimUrl(string v)
        {
            if (v != null && v.Length > 4096) {
                UtilityHelper.CountlyLogging("[UtilityHelper] TrimUrl : Max allowed length of 'PictureUrl' is 4096.");
                v = v.Substring(0, 4096);
            }

            return v;
        }

        internal static string TrimValue(string fieldName, string v, int maxValueSize)
        {
            if (v != null && v.Length > maxValueSize) {
                UtilityHelper.CountlyLogging("[UtilityHelper] TrimValue : Max allowed '" + fieldName + "' length is " + maxValueSize + ". " + v + " will be truncated.");
                v = v.Substring(0, maxValueSize);
            }

            return v;
        }

        internal static Dictionary<string, string> RemoveExtraSegments(Dictionary<string, string> segments, int maxSegmentationValues)
        {

            if (segments == null || segments.Count <= maxSegmentationValues) {
                return segments;
            }

            int i = 0;
            List<string> toRemove = new List<string>();
            foreach (KeyValuePair<string, string> item in segments) {
                if (++i > maxSegmentationValues) {
                    toRemove.Add(item.Key);
                }
            }

            foreach (string k in toRemove) {
                segments.Remove(k);
            }

            return segments;
        }

        internal static Segmentation RemoveExtraSegments(Segmentation segments, int maxSegmentationValues)
        {

            if (segments == null || segments.segmentation.Count <= maxSegmentationValues) {
                return segments;
            }

            while (segments.segmentation.Count > maxSegmentationValues) {
                segments.segmentation.RemoveAt(maxSegmentationValues);
            }

            return segments;
        }

        internal static Segmentation FixSegmentKeysAndValues(Segmentation segments, int maxKeyLength, int maxValueSize)
        {
            if (segments == null || segments.segmentation.Count == 0) {
                return segments;
            }

            Segmentation segmentation = new Segmentation();
            foreach (SegmentationItem item in segments.segmentation) {
                string k = item.Key;
                string v = item.Value;

                if (item.Key == null || item.Value == null) {
                    continue;
                }

                k = TrimKey(k, maxKeyLength);

                if (v.GetType() == typeof(string)) {
                    v = TrimValue(k, v, maxValueSize);
                }

                segmentation.Add(k, v);
            }

            return segmentation;
        }

        internal static Dictionary<string, string> FixSegmentKeysAndValues(Dictionary<string, string> segments, int maxKeyLength, int maxValueSize)
        {
            if (segments == null || segments.Count == 0) {
                return segments;
            }

            Dictionary<string, string> segmentation = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> item in segments) {
                string k = item.Key;
                string v = item.Value;

                if (k == null || v == null) {
                    continue;
                }

                k = TrimKey(k, maxKeyLength);
                v = TrimValue(k, v, maxValueSize);

                segmentation.Add(k, v);
            }

            return segmentation;
        }

        internal static string ManipulateStackTrace(string stackTrace, int maxStackTraceLinesPerThread, int maxStackTraceLineLength)
        {
            string result = null;
            if (!string.IsNullOrEmpty(stackTrace)) {
                string[] lines = stackTrace.Split('\n');

                int limit = lines.Length;

                if (limit > maxStackTraceLinesPerThread) {
                    limit = maxStackTraceLinesPerThread;
                }

                for (int i = 0; i < limit; ++i) {
                    string line = lines[i];

                    if (line.Length > maxStackTraceLineLength) {
                        line = line.Substring(0, maxStackTraceLineLength);
                    }

                    if (i + 1 != limit) {
                        line += '\n';
                    }

                    result += line;
                }
            }

            return result;
        }

        internal static string ComputeChecksum(string content)
        {
            using (var sha256 = new SHA256Managed()) {
                return BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(content))).Replace("-", "");
            }
        }
    }
}
