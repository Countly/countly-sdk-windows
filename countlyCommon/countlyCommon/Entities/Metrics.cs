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
using System.Runtime.Serialization;

namespace CountlySDK.Entities
{
    /// <summary>
    /// Holds device-specific info in json-ready format
    /// </summary>
    [DataContractAttribute]
    internal class Metrics : IComparable<Metrics>
    {
        /// <summary>
        /// Name of the current operating system
        /// </summary>
        [JsonProperty("_os")]
        [DataMemberAttribute]
        public string OS { get; set; }

        /// <summary>
        /// Current operating system version
        /// </summary>
        [JsonProperty("_os_version")]
        [DataMemberAttribute]
        public string OSVersion { get; set; }

        /// <summary>
        /// Local machine name (windows) or current device model (mobile)
        /// </summary>
        [JsonProperty("_device")]
        [DataMemberAttribute]
        public string Device { get; set; }

        /// <summary>
        /// Device resolution
        /// </summary>
        [JsonProperty("_resolution")]
        [DataMemberAttribute]
        public string Resolution { get; set; }

        /// <summary>
        /// Cellular mobile operator (where applicable)
        /// </summary>
        [JsonProperty("_carrier")]
        [DataMemberAttribute]
        public string Carrier { get; set; }

        /// <summary>
        /// Application version
        /// </summary>
        [JsonProperty("_app_version")]
        [DataMemberAttribute]
        public string AppVersion { get; set; }

        /// <summary>
        /// Creates Metrics object with provided values
        /// </summary>
        /// <param name="OS">Name of the current operating system</param>
        /// <param name="OSVersion">Current operating system version</param>
        /// <param name="Device">Current device model</param>
        /// <param name="Resolution">Device resolution</param>
        /// <param name="Carrier">Cellular mobile operator</param>
        /// <param name="AppVersion">Application version</param>
        public Metrics(string OS, string OSVersion, string Device, string Resolution, string Carrier, string AppVersion)
        {
            this.OS = OS;
            this.OSVersion = OSVersion;
            this.Device = Device;
            this.Resolution = Resolution;
            this.Carrier = Carrier;
            this.AppVersion = AppVersion;
        }

        /// <summary>
        /// Serializes object into json
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        public int CompareTo(Metrics other)
        {
            if (!(OS == null && other.OS == null))
            {
                if (OS == null) { return -1; }
                if (other.OS == null) { return 1; }
                if (!OS.Equals(other.OS)) { return OS.CompareTo(other.OS); }
            }

            if (!(OSVersion == null && other.OSVersion == null))
            {
                if (OSVersion == null) { return -1; }
                if (other.OSVersion == null) { return 1; }
                if (!OSVersion.Equals(other.OSVersion)) { return OSVersion.CompareTo(other.OSVersion); }
            }

            if (!(Device == null && other.Device == null))
            {
                if (Device == null) { return -1; }
                if (other.Device == null) { return 1; }
                if (!Device.Equals(other.Device)) { return Device.CompareTo(other.Device); }
            }

            if (!(Resolution == null && other.Resolution == null))
            {
                if (Resolution == null) { return -1; }
                if (other.Resolution == null) { return 1; }
                if (!Resolution.Equals(other.Resolution)) { return Resolution.CompareTo(other.Resolution); }
            }

            if (!(Carrier == null && other.Carrier == null))
            {
                if (Carrier == null) { return -1; }
                if (other.Carrier == null) { return 1; }
                if (!Carrier.Equals(other.Carrier)) { return Carrier.CompareTo(other.Carrier); }
            }

            if (!(AppVersion == null && other.AppVersion == null))
            {
                if (AppVersion == null) { return -1; }
                if (other.AppVersion == null) { return 1; }
                if (!AppVersion.Equals(other.AppVersion)) { return AppVersion.CompareTo(other.AppVersion); }
            }

            return 0;
        }
    }
}
