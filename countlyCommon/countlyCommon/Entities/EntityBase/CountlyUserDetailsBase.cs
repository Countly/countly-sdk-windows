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

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace CountlySDK.CountlyCommon.Entities.EntityBase
{
    /// <summary>
    /// Holds user-specific info in json-ready format
    /// </summary>
    [DataContractAttribute]
    abstract public class CountlyUserDetailsBase : IComparable<CountlyUserDetailsBase>
    {
        internal delegate void UserDetailsChangedEventHandler();
        internal bool manualUserDetailsSave = true;
        private bool isUserPropertiesChanged = false;

        // Static because DataContractSerializer skips field initializers on the instance it restores from storage
        private static readonly object modifiersLock = new object();

        // Pending custom property modifiers, key to modifier object (for example {"$inc": 1}). One-shot and never stored.
        private JObject customModifiers;

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
            if (isNotificationEnabled) {
                UserDetailsChanged?.Invoke();
            }
        }

        private string name;
        /// <summary>
        /// Name
        /// </summary>
        [JsonProperty("name")]
        [DataMemberAttribute]
        public string Name
        {
            get {
                return name;
            }
            set {
                if (name != value) {
                    name = value;
                    SaveInternal();
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
            get {
                return username;
            }
            set {
                if (username != value) {
                    username = value;
                    SaveInternal();
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
            get {
                return email;
            }
            set {
                if (email != value) {
                    email = value;
                    SaveInternal();
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
            get {
                return organization;
            }
            set {
                if (organization != value) {
                    organization = value;
                    SaveInternal();
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
            get {
                return phone;
            }
            set {
                if (phone != value) {
                    phone = value;
                    SaveInternal();
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
            get {
                return picture;
            }
            set {
                if (picture != value) {
                    picture = value;
                    SaveInternal();
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
            get {
                return gender;
            }
            set {
                if (gender != value) {
                    gender = value;
                    SaveInternal();
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
            get {
                return birthYear;
            }
            set {
                if (birthYear != value) {
                    birthYear = value;

                    SaveInternal();
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
            get {
                return custom;
            }
            set {
                if (custom != value) {
                    if (custom != null) {
                        custom.CollectionChanged -= SaveInternal;
                    }

                    if (value != null) {
                        custom = value;

                        custom.CollectionChanged += SaveInternal;
                    } else {
                        custom?.Clear();
                    }

                    SaveInternal();
                }
            }
        }

        /// <summary>
        /// Custom data ready for json serializer
        /// </summary>
        [JsonProperty("custom")]
        [DataMemberAttribute]
        internal Dictionary<string, string> _custom
        {
            get {
                return Custom?.ToDictionary();
            }
            set {
                Custom = new CustomInfo();

                if (value != null) {
                    foreach (var a in value) {
                        Custom.Add(a.Key, a.Value);
                    }
                }
            }
        }

        protected abstract void NotifyDetailsChanged();

        [JsonIgnore]
        [DataMemberAttribute]
        internal bool isChanged { get; set; }

        [JsonIgnore]
        internal bool isNotified { get; set; }

        [JsonIgnore]
        internal bool isNotificationEnabled { get; set; }

        /// <summary>
        /// Needed for JSON deserialization
        /// </summary>
        public CountlyUserDetailsBase()
        {
            Custom = new CustomInfo();
            isNotificationEnabled = true;
        }

        /// <summary>
        /// Uploads user picture. Accepted picture formats are .png, .gif and .jpeg and picture will be resized to maximal 150x150 dimensions
        /// </summary>
        /// <param name="imageStream">Image stream</param>
        /// <returns>true if image is successfully uploaded, false otherwise</returns>
        public async Task<bool> UploadUserPicture(Stream imageStream)
        {
            return await Countly.Instance.UploadUserPicture(imageStream);
        }

        /// <summary>
        /// Saves user details
        /// </summary>
        public void Save()
        {
            UtilityHelper.CountlyLogging("[Countly] [CountlyUserDetailsBase] Save, manualUserDetailsSave: [" + manualUserDetailsSave + "], isUserPropertiesChanged: [" + isUserPropertiesChanged + "]");
            if (!manualUserDetailsSave) {
                return;
            }

            if (!isUserPropertiesChanged && !HasCustomModifiers()) {
                return;
            }
            isUserPropertiesChanged = false;
            NotifyDetailsChanged();
        }

        /// <summary>
        /// Increments a custom user property by 1. Sent with the next user details save.
        /// </summary>
        /// <param name="key">custom property name</param>
        public void Increment(string key)
        {
            ModifyCustomData(key, 1, "$inc");
        }

        /// <summary>
        /// Increments a custom user property by the given value. Sent with the next user details save.
        /// </summary>
        /// <param name="key">custom property name</param>
        /// <param name="value">value to add</param>
        public void IncrementBy(string key, int value)
        {
            ModifyCustomData(key, value, "$inc");
        }

        /// <summary>
        /// Multiplies a custom user property by the given value. Sent with the next user details save.
        /// </summary>
        /// <param name="key">custom property name</param>
        /// <param name="value">value to multiply by</param>
        public void Multiply(string key, int value)
        {
            ModifyCustomData(key, value, "$mul");
        }

        /// <summary>
        /// Keeps the larger of a custom user property's current value and the given value. Sent with the next user details save.
        /// </summary>
        /// <param name="key">custom property name</param>
        /// <param name="value">value to compare with</param>
        public void SaveMax(string key, int value)
        {
            ModifyCustomData(key, value, "$max");
        }

        /// <summary>
        /// Keeps the smaller of a custom user property's current value and the given value. Sent with the next user details save.
        /// </summary>
        /// <param name="key">custom property name</param>
        /// <param name="value">value to compare with</param>
        public void SaveMin(string key, int value)
        {
            ModifyCustomData(key, value, "$min");
        }

        /// <summary>
        /// Sets a custom user property only if it does not exist yet. Sent with the next user details save.
        /// </summary>
        /// <param name="key">custom property name</param>
        /// <param name="value">value to set</param>
        public void SetOnce(string key, string value)
        {
            ModifyCustomData(key, value, "$setOnce");
        }

        /// <summary>
        /// Adds a value to a custom user property array, creating the array if the property does not exist yet.
        /// Sent with the next user details save.
        /// </summary>
        /// <param name="key">custom property name</param>
        /// <param name="value">value to add</param>
        public void Push(string key, string value)
        {
            ModifyCustomData(key, value, "$push");
        }

        /// <summary>
        /// Adds a value to a custom user property array only if the array does not contain it yet,
        /// creating the array if the property does not exist yet. Sent with the next user details save.
        /// </summary>
        /// <param name="key">custom property name</param>
        /// <param name="value">value to add</param>
        public void PushUnique(string key, string value)
        {
            ModifyCustomData(key, value, "$addToSet");
        }

        /// <summary>
        /// Removes a value from a custom user property array. Sent with the next user details save.
        /// </summary>
        /// <param name="key">custom property name</param>
        /// <param name="value">value to remove</param>
        public void Pull(string key, string value)
        {
            ModifyCustomData(key, value, "$pull");
        }

        /// <summary>
        /// Records a modifier for a custom user property until the next user details save.
        /// A numeric modifier or "$setOnce" replaces the key's pending modifiers, an array modifier accumulates into them.
        /// </summary>
        /// <param name="key">custom property name</param>
        /// <param name="value">int or string value of the modifier</param>
        /// <param name="mod">modifier name, for example "$inc"</param>
        private void ModifyCustomData(string key, object value, string mod)
        {
            UtilityHelper.CountlyLogging("[Countly] [CountlyUserDetailsBase] ModifyCustomData, key: [" + key + "], modifier: [" + mod + "]");
            Countly cly = Countly.Instance;
            CountlyConfig config = cly.Configuration;
            if (config == null || !cly.IsInitialized()) {
                UtilityHelper.CountlyLogging("[Countly] [CountlyUserDetailsBase] ModifyCustomData, SDK must be initialized before modifying custom user properties", LogLevel.WARNING);
                return;
            }

            if (string.IsNullOrEmpty(key) || value == null) {
                UtilityHelper.CountlyLogging("[Countly] [CountlyUserDetailsBase] ModifyCustomData, key can't be null or empty and value can't be null, ignoring", LogLevel.WARNING);
                return;
            }

            if (!cly.IsConsentGiven(ConsentFeatures.Users)) {
                UtilityHelper.CountlyLogging("[Countly] [CountlyUserDetailsBase] ModifyCustomData, 'Users' consent not given, ignoring");
                return;
            }

            string truncatedKey = UtilityHelper.TrimKey(key, config.MaxKeyLength);
            ModuleServerConfig serverConfig = cly.moduleServerConfig;
            int cacheLimit = int.MaxValue;
            if (serverConfig != null) {
                if (!serverConfig.IsUserPropertyAllowed(truncatedKey)) {
                    UtilityHelper.CountlyLogging("[Countly] [CountlyUserDetailsBase] ModifyCustomData, key: [" + truncatedKey + "] is filtered out by server config, ignoring");
                    return;
                }
                cacheLimit = serverConfig.GetUserPropertyCacheLimit();
            }

            if (value is string) {
                value = UtilityHelper.TrimValue(truncatedKey, (string)value, config.MaxValueSize);
            }

            bool isArrayModifier = mod == "$push" || mod == "$addToSet" || mod == "$pull";
            bool overCacheLimit = false;
            lock (modifiersLock) {
                JObject modifiers = customModifiers ?? new JObject();
                JObject pending = modifiers[truncatedKey] as JObject;
                if (pending == null && modifiers.Count >= cacheLimit) {
                    overCacheLimit = true;
                } else {
                    if (pending == null || !isArrayModifier) {
                        pending = new JObject();
                        modifiers[truncatedKey] = pending;
                    }
                    Accumulate(pending, mod, new JValue(value));
                    customModifiers = modifiers;
                }
            }

            if (overCacheLimit) {
                UtilityHelper.CountlyLogging("[Countly] [CountlyUserDetailsBase] ModifyCustomData, user property cache limit [" + cacheLimit + "] reached, dropping: [" + truncatedKey + "]", LogLevel.WARNING);
                return;
            }

            SaveInternal();
        }

        /// <summary>
        /// Adds the value under the modifier the way Android's JSONObject.accumulate does:
        /// a single value first, an array once there are more
        /// </summary>
        private static void Accumulate(JObject pending, string mod, JValue value)
        {
            JToken existing = pending[mod];
            if (existing == null) {
                pending[mod] = value;
            } else if (existing is JArray) {
                ((JArray)existing).Add(value);
            } else {
                pending[mod] = new JArray(existing, value);
            }
        }

        /// <summary>
        /// Returns true when custom property modifiers wait for the next user details save
        /// </summary>
        private bool HasCustomModifiers()
        {
            lock (modifiersLock) {
                return customModifiers != null;
            }
        }

        /// <summary>
        /// Returns the pending custom property modifiers, key to modifier object, and clears them; null when there are none
        /// </summary>
        internal JObject TakeCustomModifiers()
        {
            lock (modifiersLock) {
                JObject modifiers = customModifiers;
                customModifiers = null;
                return modifiers;
            }
        }

        private void SaveInternal()
        {
            UtilityHelper.CountlyLogging("[Countly] [CountlyUserDetailsBase] SaveInternal, manualUserDetailsSave: [" + manualUserDetailsSave + "], if true will not call upload immediately");
            isUserPropertiesChanged = true;
            if (!manualUserDetailsSave) {
                NotifyDetailsChanged();
            }
        }

        internal void Clear()
        {
            UtilityHelper.CountlyLogging("[Countly] [CountlyUserDetailsBase] Clear, will clear the cache");
            Custom = null;
            Name = null;
            Username = null;
            Email = null;
            Organization = null;
            Phone = null;
            Picture = null;
            BirthYear = null;
            Gender = null;
            isChanged = false;
            isUserPropertiesChanged = false;
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
            if (!(name == null && other.name == null)) {
                if (name == null) { return -1; }
                if (other.name == null) { return 1; }
                if (!name.Equals(other.name)) { return name.CompareTo(other.name); }
            }

            if (!(username == null && other.username == null)) {
                if (username == null) { return -1; }
                if (other.username == null) { return 1; }
                if (!username.Equals(other.username)) { return username.CompareTo(other.username); }
            }

            if (!(email == null && other.email == null)) {
                if (email == null) { return -1; }
                if (other.email == null) { return 1; }
                if (!email.Equals(other.email)) { return email.CompareTo(other.email); }
            }

            if (!(organization == null && other.organization == null)) {
                if (organization == null) { return -1; }
                if (other.organization == null) { return 1; }
                if (!organization.Equals(other.organization)) { return organization.CompareTo(other.organization); }
            }

            if (!(phone == null && other.phone == null)) {
                if (phone == null) { return -1; }
                if (other.phone == null) { return 1; }
                if (!phone.Equals(other.phone)) { return phone.CompareTo(other.phone); }
            }

            if (!(picture == null && other.picture == null)) {
                if (picture == null) { return -1; }
                if (other.picture == null) { return 1; }
                if (!picture.Equals(other.picture)) { return picture.CompareTo(other.picture); }
            }

            if (!(gender == null && other.gender == null)) {
                if (gender == null) { return -1; }
                if (other.gender == null) { return 1; }
                if (!gender.Equals(other.gender)) { return gender.CompareTo(other.gender); }
            }

            if (!(birthYear == null && other.birthYear == null)) {
                if (birthYear == null) { return -1; }
                if (other.birthYear == null) { return 1; }
                if (!birthYear.Equals(other.birthYear)) { return birthYear.Value.CompareTo(other.birthYear.Value); }
            }

            if (!(_custom == null && other._custom == null)) {
                if (_custom == null) { return -1; }
                if (other._custom == null) { return 1; }
                if (!_custom.Equals(other._custom)) {
                    //the one with more fields is greater
                    if (!_custom.Count.Equals(other._custom.Count)) { _custom.Count.CompareTo(other._custom.Count); }

                    //if some differences are found, assume that this is greater
                    foreach (KeyValuePair<String, String> pair in _custom) {
                        if (!other._custom.ContainsKey(pair.Key)) {
                            return 1;
                        }
                        String otherPairValue = other._custom[pair.Key];

                        if (!pair.Value.Equals(otherPairValue)) {
                            return 1;
                        }
                    }
                }
            }

            return 0;
        }
    }
}
