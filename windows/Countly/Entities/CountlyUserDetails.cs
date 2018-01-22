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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace CountlySDK.Entities
{
    /// <summary>
    /// Holds user-specific info in json-ready format
    /// </summary>
    [DataContractAttribute]
    public class CountlyUserDetails
    {
        internal delegate void UserDetailsChangedEventHandler();

        /// <summary>
        /// raised when any of properties are changed
        /// </summary>
        internal event UserDetailsChangedEventHandler UserDetailsChanged;

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
        [DataMemberAttribute]
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
        private Dictionary<string, string> _custom
        {
            get
            {
                return Custom.ToDictionary();
            }
        }

        private async void NotifyDetailsChanged()
        {
            if (UserDetailsChanged != null)
            {
                isNotified = false;

                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    if (!isNotified)
                    {
                        isNotified = true;

                        UserDetailsChanged();
                    }
                });
            }
        }

        [JsonIgnore]
        [DataMemberAttribute]
        internal bool isChanged { get; set; }

        [JsonIgnore]
        internal bool isNotified { get; set; }

        public CountlyUserDetails()
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
            return await Countly.UploadUserPicture(imageStream);
        }

        /// <summary>
        /// Serializes object into json
        /// </summary>
        /// <returns>json representation string</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }
    }
}
