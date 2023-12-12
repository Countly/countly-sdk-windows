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
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace CountlySDK
{
    /// <summary>
    /// Holds data about segmentation value
    /// </summary>
    [DataContractAttribute]
    public class SegmentationItem : IComparable<SegmentationItem>
    {
        /// <summary>
        /// Segmentation key
        /// </summary>
        [DataMemberAttribute]
        public string Key { get; set; }

        /// <summary>
        /// Segmentation value
        /// </summary>
        [DataMemberAttribute]
        public string Value { get; set; }

        /// <summary>
        /// Creates object with provided values
        /// </summary>
        /// <param name="Key">Segmentation key</param>
        /// <param name="Value">Segmentation value</param>
        public SegmentationItem(string Key, string Value)
        {
            this.Key = Key;
            this.Value = Value;
        }

        /// <summary>
        /// Needed for JSON deserialization
        /// </summary>
        [JsonConstructor]
        private SegmentationItem()
        { }

        public int CompareTo(SegmentationItem other)
        {
            if (!(Key == null && other.Key == null)) {
                if (Key == null) { return -1; }
                if (other.Key == null) { return 1; }
                if (!Key.Equals(other.Key)) { return Key.CompareTo(other.Key); }
            }

            if (!(Value == null && other.Value == null)) {
                if (Value == null) { return -1; }
                if (other.Value == null) { return 1; }
                if (!Value.Equals(other.Value)) { return Value.CompareTo(other.Value); }
            }

            return 0;
        }
    }
}
