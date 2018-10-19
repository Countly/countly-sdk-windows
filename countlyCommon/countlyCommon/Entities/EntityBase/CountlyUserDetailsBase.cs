/*
Copyright (c) 2012, 2013, 2014 Countly

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

using CountlySDK.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace CountlySDK.CountlyCommon.Entities.EntityBase
{
    /// <summary>
    /// Holds user-specific info in json-ready format
    /// </summary>
    [DataContractAttribute]
    abstract public class CountlyUserDetailsBase : IComparable<CountlyUserDetailsBase>
    {
        internal delegate void UserDetailsChangedEventHandler();

        /// <summary>
        /// raised when any of properties are changed
        /// </summary>
        internal event UserDetailsChangedEventHandler UserDetailsChanged;

        protected bool IsSetUserDetailsChanged()
        {
            return UserDetailsChanged != null;
        }

        protected void CallUserDetailsChanged()
        {
            UserDetailsChanged?.Invoke();
        }

        private string name;
        /// <summary>
        /// Name
        /// </summary>
        [JsonProperty("name")]
        [DataMemberAttribute]
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (name != value)
                {
                    name = value;

                    NotifyDetailsChanged();
                }
            }
        }

        private string username;
        /// <summary>
        /// Username or login info
        /// </summary>
        [JsonProperty("username")]
        [DataMemberAttribute]
        public string Username
        {
            get
            {
                return username;
            }
            set
            {
                if (username != value)
                {
                    username = value;

                    NotifyDetailsChanged();
                }
            }
        }

        private string email;
        /// <summary>
        /// User email address
        /// </summary>
        [JsonProperty("email")]
        [DataMemberAttribute]
        public string Email
        {
            get
            {
                return email;
            }
            set
            {
                if (email != value)
                {
                    email = value;

                    NotifyDetailsChanged();
                }
            }
        }

        private string organization;
        /// <summary>
        /// User organization
        /// </summary>
        [JsonProperty("organization")]
        [DataMemberAttribute]
        public string Organization
        {
            get
            {
                return organization;
            }
            set
            {
                if (organization != value)
                {
                    organization = value;

                    NotifyDetailsChanged();
                }
            }
        }

        private string phone;
        /// <summary>
        /// User phone
        /// </summary>
        [JsonProperty("phone")]
        [DataMemberAttribute]
        public string Phone
        {
            get
            {
                return phone;
            }
            set
            {
                if (phone != value)
                {
                    phone = value;

                    NotifyDetailsChanged();
                }
            }
        }

        private string picture;
        /// <summary>
        /// Web URL to picture
        /// </summary>
        [JsonProperty("picture")]
        [DataMemberAttribute]
        public string Picture
        {
            get
            {
                return picture;
            }
            set
            {
                if (picture != value)
                {
                    picture = value;

                    NotifyDetailsChanged();
                }
            }
        }

        private string gender;
        /// <summary>
        /// User gender
        /// </summary>
        [JsonProperty("gender")]
        [DataMemberAttribute]
        public string Gender
        {
            get
            {
                return gender;
            }
            set
            {
                if (gender != value)
                {
                    gender = value;

                    NotifyDetailsChanged();
                }
            }
        }

        private int? birthYear;
        /// <summary>
        /// User birth year
        /// </summary>
        [JsonProperty("byear")]
        [DataMemberAttribute]
        public int? BirthYear
        {
            get
            {
                return birthYear;
            }
            set
            {
                if (birthYear != value)
                {
                    birthYear = value;

                    NotifyDetailsChanged();
                }
            }
        }

        private CustomInfo custom;
        /// <summary>
        /// User custom data
        /// </summary>
        [JsonIgnore]        
        public CustomInfo Custom
        {
            get
            {
                return custom;
            }
            set
            {
                if (custom != value)
                {
                    if (custom != null)
                    {
                        custom.CollectionChanged -= NotifyDetailsChanged;
                    }

                    if (value != null)
                    {
                        custom = value;

                        custom.CollectionChanged += NotifyDetailsChanged;
                    }
                    else
                    {
                        custom.Clear();
                    }

                    NotifyDetailsChanged();
                }
            }
        }

        /// <summary>
        /// Custom data ready for json serializer
        /// </summary>
        [JsonProperty("custom")]
        [DataMemberAttribute]
        private Dictionary<string, string> _custom
        {
            get
            {
                return Custom?.ToDictionary();
            }
            set
            {
                Custom = new CustomInfo();
                foreach(var a in value)
                {
                    Custom.Add(a.Key, a.Value);
                }
            }
        }

        protected abstract void NotifyDetailsChanged();

        [JsonIgnore]
        [DataMemberAttribute]
        internal bool isChanged { get; set; }

        [JsonIgnore]
        internal bool isNotified { get; set; }

        /// <summary>
        /// Needed for JSON deserialization
        /// </summary>
        public CountlyUserDetailsBase()
        {
            Custom = new CustomInfo();
        }

        /// <summary>
        /// Uploads user picture. Accepted picture formats are .png, .gif and .jpeg and picture will be resized to maximal 150x150 dimensions
        /// </summary>
        /// <param name="stream">Image stream</param>
        /// <returns>true if image is successfully uploaded, false otherwise</returns>
        public async Task<bool> UploadUserPicture(Stream imageStream)
        {
            return await Countly.Instance.UploadUserPicture(imageStream);
        }

        /// <summary>
        /// Serializes object into json
        /// </summary>
        /// <returns>json representation string</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        public int CompareTo(CountlyUserDetailsBase other)
        {
            //the one with null values is lesser
            if (!(name == null && other.name == null))
            {
                if (name == null) { return -1; }
                if (other.name == null) { return 1; }
                if (!name.Equals(other.name)) { return name.CompareTo(other.name); }
            }

            if (!(username == null && other.username == null))
            {
                if (username == null) { return -1; }
                if (other.username == null) { return 1; }
                if (!username.Equals(other.username)) { return username.CompareTo(other.username); }
            }

            if (!(email == null && other.email == null))
            {
                if (email == null) { return -1; }
                if (other.email == null) { return 1; }
                if (!email.Equals(other.email)) { return email.CompareTo(other.email); }
            }

            if (!(organization == null && other.organization == null))
            {
                if (organization == null) { return -1; }
                if (other.organization == null) { return 1; }
                if (!organization.Equals(other.organization)) { return organization.CompareTo(other.organization); }
            }

            if (!(phone == null && other.phone == null))
            {
                if (phone == null) { return -1; }
                if (other.phone == null) { return 1; }
                if (!phone.Equals(other.phone)) { return phone.CompareTo(other.phone); }
            }

            if (!(picture == null && other.picture == null))
            {
                if (picture == null) { return -1; }
                if (other.picture == null) { return 1; }
                if (!picture.Equals(other.picture)) { return picture.CompareTo(other.picture); }
            }

            if (!(gender == null && other.gender == null))
            {
                if (gender == null) { return -1; }
                if (other.gender == null) { return 1; }
                if (!gender.Equals(other.gender)) { return gender.CompareTo(other.gender); }
            }                       

            if(!(birthYear == null && other.birthYear == null))
            {
                if (birthYear == null) { return -1; }
                if (other.birthYear == null) { return 1; }
                if (!birthYear.Equals(other.birthYear)) { return birthYear.Value.CompareTo(other.birthYear.Value); }
            }
            
            if(!(_custom == null && other._custom == null))
            {
                if (_custom == null) { return -1; }
                if (other._custom == null) { return 1; }
                if (!_custom.Equals(other._custom))
                {
                    //the one with more fields is greater
                    if (!_custom.Count.Equals(other._custom.Count)) { _custom.Count.CompareTo(other._custom.Count); }

                    //if some differences are found, assume that this is greater
                    foreach (KeyValuePair<String, String> pair in _custom)
                    {
                        if (!other._custom.ContainsKey(pair.Key)) return 1;
                        String otherPairValue = other._custom[pair.Key];

                        if (!pair.Value.Equals(otherPairValue)) return 1;
                    }
                }
            }
                             
            return 0;
        }
    }
}
