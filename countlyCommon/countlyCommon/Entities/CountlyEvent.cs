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
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace CountlySDK.Entities
{
    /// <summary>
    /// This class holds the data for a single Count.ly custom event instance.
    /// </summary>
    [DataContractAttribute]
    internal class CountlyEvent : IComparable<CountlyEvent>
    {
        /// <summary>
        /// Key attribute, must be non-empty
        /// </summary>
        [DataMemberAttribute]
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        /// Count parameter, must me positive number
        /// </summary>
        [DataMemberAttribute]
        [JsonProperty("count")]
        public int Count { get; set; }

        /// <summary>
        /// Sum parameter, can be null
        /// </summary>
        [DataMemberAttribute]
        [JsonProperty("sum")]
        public double? Sum { get; set; }

        /// <summary>
        /// Duration of event, can be null
        /// </summary>
        [DataMemberAttribute]
        [JsonProperty("dur")]
        public double? Duration { get; set; }

        /// <summary>
        /// Timestamp of event, can be null
        /// </summary>
        [DataMemberAttribute]
        [JsonProperty("timestamp")]
        public long? Timestamp { get; set; }

        /// <summary>
        /// Segmentation parameter
        /// </summary>        
        [DataMemberAttribute]
        [JsonIgnore]
        public Segmentation Segmentation { get; internal set; }

        /// <summary>
        /// Segmentation json-ready object
        /// </summary>        
        [JsonProperty("segmentation")]
        public Dictionary<String, String> segmentation
        {
            get {
                if (Segmentation == null)
                    return null;

                return Segmentation.segmentation.ToDictionary(s => s.Key, s => s.Value);
            }

            //needed for deserialization
            private set {
                Segmentation = new Segmentation();
                foreach (var a in value) {
                    Segmentation.Add(a.Key, a.Value);
                }

            }
        }

        /// <summary>
        /// Needed for JSON deserialization
        /// </summary>
        [JsonConstructor]
        private CountlyEvent()
        { }

        /// <summary>
        /// Create Countly event with provided values
        /// </summary>
        /// <param name="Key">Key attribute, must be non-empty</param>
        /// <param name="Count">Count parameter, must me positive number</param>
        /// <param name="Sum">Sum parameter, can be null</param>
        /// <param name="Segmentation">Segmentation parameter</param>
        public CountlyEvent(string Key, int Count, double? Sum, double? Duration, Segmentation Segmentation, long? timestamp)
        {
            if (UtilityHelper.IsNullOrEmptyOrWhiteSpace(Key)) {
                throw new ArgumentException("Event Key must be non-empty string");
            }

            if (Count <= 0) {
                throw new ArgumentException("Event Count must be positive number");
            }

            this.Key = Key;
            this.Count = Count;
            this.Sum = Sum;
            this.Segmentation = Segmentation;
            this.Duration = Duration;
            this.Timestamp = timestamp;
        }

        public int CompareTo(CountlyEvent other)
        {
            if (!(Key == null && other.Key == null)) {
                if (Key == null) { return -1; }
                if (other.Key == null) { return 1; }
                if (!Key.Equals(other.Key)) { return Key.CompareTo(other.Key); }
            }

            if (!Count.Equals(other.Count)) { return Count.CompareTo(other.Count); }

            if (!(Sum == null && other.Sum == null)) {
                if (Sum == null) { return -1; }
                if (other.Sum == null) { return 1; }
                if (!Sum.Equals(other.Sum)) { return Sum.Value.CompareTo(other.Sum.Value); }
            }

            if (!(Duration == null && other.Duration == null)) {
                if (Duration == null) { return -1; }
                if (other.Duration == null) { return 1; }
                if (!Duration.Equals(other.Duration)) { return Duration.Value.CompareTo(other.Duration.Value); }
            }

            if (!(segmentation == null && other.segmentation == null)) {
                if (segmentation == null) { return -1; }
                if (other.segmentation == null) { return 1; }
                if (!segmentation.Count.Equals(other.segmentation.Count)) { return segmentation.Count.CompareTo(other.segmentation.Count); }

                foreach (var a in segmentation.Keys) {
                    if (!other.segmentation.ContainsKey(a)) { return -1; }
                    if (!segmentation[a].Equals(other.segmentation[a])) { return segmentation[a].CompareTo(other.segmentation[a]); }
                }
            }

            if (!(Timestamp == null && other.Timestamp == null)) {
                if (Timestamp == null) { return -1; }
                if (other.Timestamp == null) { return 1; }
                if (!Timestamp.Equals(other.Timestamp)) { return Timestamp.Value.CompareTo(other.Timestamp.Value); }
            }

            return 0;
        }
    }
}
