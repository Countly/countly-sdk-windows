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
using System.Diagnostics;
using System.IO;

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
            if (String.IsNullOrEmpty(value))
            {
                return true;
            }

            bool containsOnlyWhitespace = true;

            foreach(char c in value)
            {
                if (!char.IsWhiteSpace(c))
                {
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

        public static void CountlyLogging(String msg)
        {
            if (Countly.IsLoggingEnabled)
            {
                System.Diagnostics.Debug.WriteLine(msg);
            }
        }

        public static int CompareQueues<T>(Queue<T> first, Queue<T> second) where T : IComparable<T>
        {
            if (first == null && second == null)
            {
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
            if (first == null && second == null) return 0;
            if (first == null) { return -1; }
            if (second == null) { return 1; }

            if (first.Count > second.Count) { return 1; }
            if (first.Count < second.Count) { return -1; }

            for (int a = 0; a < first.Count; a++)
            {
                if (!first[a].Equals(second[a]))
                {
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
    }
}