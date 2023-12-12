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

namespace CountlySDK.Entities
{
    /// <summary>
    /// Holds data about segmentation value
    /// </summary>
    [DataContractAttribute]
    public class CustomInfoItem : IComparable<CustomInfoItem>
    {
        /// <summary>
        /// Property name
        /// </summary>
        [DataMemberAttribute]
        public string Name { get; set; }

        /// <summary>
        /// Property value
        /// </summary>
        [DataMemberAttribute]
        public string Value { get; set; }

        /// <summary>
        /// Creates object with provided values
        /// </summary>
        /// <param name="Name">Property name</param>
        /// <param name="Value">Property value</param>
        public CustomInfoItem(string Name, string Value)
        {
            this.Name = Name;
            this.Value = Value;
        }

        /// <summary>
        /// Needed for JSON deserialization
        /// </summary>
        [JsonConstructor]
        public CustomInfoItem() { }

        public int CompareTo(CustomInfoItem other)
        {
            if (!(Name == null && other.Name == null)) {
                if (Name == null) { return -1; }
                if (other.Name == null) { return 1; }
                if (!Name.Equals(other.Name)) { return Name.CompareTo(other.Name); }
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
