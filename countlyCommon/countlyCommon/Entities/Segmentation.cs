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
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CountlySDK
{
    /// <summary>
    /// Holds an array of segmentation values
    /// </summary>
    [DataContractAttribute]
    public class Segmentation : IComparable<Segmentation>
    {
        /// <summary>
        /// Segmenation array
        /// </summary>
        [DataMemberAttribute]
        internal List<SegmentationItem> segmentation { get; set; }

        /// <summary>
        /// Needed for JSON deserialization
        /// </summary>
        public Segmentation()
        {
            segmentation = new List<SegmentationItem>();
        }

        /// <summary>
        /// Add new segmentation value, omits the null keys
        /// overrides the same keys
        /// </summary>
        /// <param name="Key">Segmenation key</param>
        /// <param name="Value">Segmenation value</param>
        public void Add(string Key, string Value)
        {
            if (string.IsNullOrEmpty(Key)) {
                return;
            }
            // Check if a segmentation item with the same key already exists
            SegmentationItem existingItem = segmentation.Find(item => item.Key == Key);
            if (existingItem != null) {
                // Update the value if the key exists
                existingItem.Value = Value;
            } else {
                // Add a new item if the key doesn't exist
                segmentation.Add(new SegmentationItem(Key, Value));
            }
        }

        public int CompareTo(Segmentation other)
        {
            if (!(segmentation == null && other.segmentation == null)) {
                if (segmentation == null) { return -1; }
                if (other.segmentation == null) { return 1; }
                if (!segmentation.Count.Equals(other.segmentation.Count)) { return segmentation.Count.CompareTo(other.segmentation.Count); }

                for (int a = 0; a < segmentation.Count; a++) {
                    if (!segmentation[a].Equals(other.segmentation[a])) { return segmentation[a].CompareTo(other.segmentation[a]); }
                }
            }

            return 0;
        }
    }
}
