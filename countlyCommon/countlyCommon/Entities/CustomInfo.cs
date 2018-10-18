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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace CountlySDK.Entities
{
    /// <summary>
    /// Holds a dictionary of custom info values
    /// </summary>
    [DataContractAttribute]
    public class CustomInfo : IComparable<CustomInfo>
    {
        internal delegate void CollectionChangedEventHandler();

        /// <summary>
        /// Raised when collection is changed
        /// </summary>
        internal event CollectionChangedEventHandler CollectionChanged;

        [DataMemberAttribute]
        internal List<CustomInfoItem> items { get; set; }

        [JsonIgnore]
        protected object sync = new object();

        /// <summary>
        /// Adds new custom item
        /// </summary>
        /// <param name="Name">item name</param>
        /// <param name="Value">item value</param>
        public void Add(string Name, string Value)
        {
            if (this[Name] != null)
            {
                Remove(Name);
            }

            if (Value != null)
            {
                lock (sync)
                {
                    items.Add(new CustomInfoItem(Name, Value));
                }
            }

            if (CollectionChanged != null)
                CollectionChanged();
        }

        /// <summary>
        /// Removes custom item
        /// </summary>
        /// <param name="Name">item name</param>
        public void Remove(string Name)
        {
            CustomInfoItem customInfoItem;
            lock (sync)
            {
                customInfoItem = items.FirstOrDefault(c => c.Name == Name);
                if (customInfoItem != null)
                {
                    items.Remove(customInfoItem);
                }
            }

            if (customInfoItem != null)
            {
                if (CollectionChanged != null)
                    CollectionChanged();
            }
        }

        /// <summary>
        /// Clears items collection
        /// </summary>
        public void Clear()
        {
            lock (sync)
            {
                items.Clear();
            }

            if (CollectionChanged != null)
                CollectionChanged();
        }

        /// <summary>
        /// Gets or sets item value based on provided item name
        /// </summary>
        /// <param name="name">item name</param>
        /// <returns>item value</returns>
        public string this[string name]
        {
            get
            {
                CustomInfoItem customInfoItem;
                lock (sync)
                {
                    customInfoItem = items.FirstOrDefault(c => c.Name == name);
                }

                if (customInfoItem != null)
                {
                    return customInfoItem.Value;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                Add(name, value);
            }
        }

        /// <summary>
        /// Needed for JSON deserialization
        /// </summary>
        public CustomInfo()
        {
            lock (sync)
            {
                items = new List<CustomInfoItem>();
            }
        }

        /// <summary>
        /// Returns items as key/value dictionary pairs
        /// </summary>
        /// <returns></returns>
        internal Dictionary<string, string> ToDictionary()
        {
            lock (sync)
            {
                if (items.Count == 0) return null;
            }

            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            lock (sync)
            {
                foreach (CustomInfoItem item in items)
                {
                    dictionary[item.Name] = item.Value;
                }
            }

            return dictionary;
        }

        public int CompareTo(CustomInfo other)
        {       
            if(items == null && other.items == null)
            {
                return 0;
            }

            if (!items.Count.Equals(other.items.Count))
            {
                return items.Count.CompareTo(other.items.Count);
            }
            
            for(int a = 0; a < items.Count; a++)
            {
                CustomInfoItem cii1 = items[a];
                CustomInfoItem cii2 = other.items[a];

                if (!cii1.Equals(cii2))
                {
                    return cii1.CompareTo(cii2);
                }
            }

            return 0;
        }
    }
}
