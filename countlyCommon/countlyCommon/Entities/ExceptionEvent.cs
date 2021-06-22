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

using CountlySDK.Entities.EntityBase;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CountlySDK.Entities
{
    /// <summary>
    /// This class holds the data about an application exception
    /// </summary>
    [DataContractAttribute]
    internal class ExceptionEvent : IComparable<ExceptionEvent>
    {
        //device metrics

        [DataMemberAttribute]
        [JsonProperty("_os")]
        public string OS { get; set; }

        [DataMemberAttribute]
        [JsonProperty("_os_version")]
        public string OSVersion { get; set; }

        [DataMemberAttribute]
        [JsonProperty("_manufacture")]
        public string Manufacture { get; set; }

        [DataMemberAttribute]
        [JsonProperty("_device")]
        public string Device { get; set; }

        [DataMemberAttribute]
        [JsonProperty("_resolution")]
        public string Resolution { get; set; }

        [DataMemberAttribute]
        [JsonProperty("_app_version")]
        public string AppVersion { get; set; }

        //state of device

        [DataMemberAttribute]
        [JsonProperty("_orientation")]
        public string Orientation { get; set; }

        [DataMemberAttribute]
        [JsonProperty("_ram_current")]
        public long? RamCurrent { get; set; }

        [DataMemberAttribute]
        [JsonProperty("_ram_total")]
        public long? RamTotal { get; set; }

        //bools

        [DataMemberAttribute]
        [JsonProperty("_online")]
        public bool? Online { get; set; }

        //error info

        [DataMemberAttribute]
        [JsonProperty("_name")]
        public string Name { get; set; }

        [DataMemberAttribute]
        [JsonProperty("_error")]
        public string Error { get; set; }

        [DataMemberAttribute]
        [JsonProperty("_nonfatal")]
        public bool NonFatal { get; set; }

        [DataMemberAttribute]
        [JsonProperty("_logs")]
        public string Logs { get; set; }

        [DataMemberAttribute]
        [JsonProperty("_run")]
        public long Run { get; set; }

        //custom key/values provided by developers
        [DataMemberAttribute]
        [JsonProperty("_custom")]
        public Dictionary<string, string> Custom { get; set; }

        /// <summary>
        /// Needed for JSON deserialization
        /// </summary>
        internal ExceptionEvent()
        { }

        internal ExceptionEvent(string Error, string StackTrace, bool fatal, string breadcrumb, TimeSpan run, string appVersion, Dictionary<string, string> customInfo, DeviceBase DeviceData)
        {
            //device metrics
            OS = DeviceData.OS;
            OSVersion = DeviceData.OSVersion;
            Manufacture = DeviceData.Manufacturer;
            Device = DeviceData.DeviceName;
            Resolution = DeviceData.Resolution;
            AppVersion = appVersion;

            //state of device
            Orientation = DeviceData.Orientation;
            RamCurrent = DeviceData.RamCurrent;
            RamTotal = DeviceData.RamTotal;

            //bools
            Online = DeviceData.Online;

            //error info
            this.Name = Error;
            this.Error = StackTrace;
            if (string.IsNullOrEmpty(this.Error)) {
                //in case stacktrace is empty, replace it with the error name
                this.Error = this.Name;
            }

            NonFatal = !fatal;
            Logs = breadcrumb;
            Run = (long)run.TotalSeconds;
            Custom = customInfo;
        }

        public int CompareTo(ExceptionEvent other)
        {
            if (!(OS == null && other.OS == null)) {
                if (OS == null) { return -1; }
                if (other.OS == null) { return 1; }
                if (!OS.Equals(other.OS)) { return OS.CompareTo(other.OS); }
            }

            if (!(OSVersion == null && other.OSVersion == null)) {
                if (OSVersion == null) { return -1; }
                if (other.OSVersion == null) { return 1; }
                if (!OSVersion.Equals(other.OSVersion)) { return OSVersion.CompareTo(other.OSVersion); }
            }

            if (!(Manufacture == null && other.Manufacture == null)) {
                if (Manufacture == null) { return -1; }
                if (other.Manufacture == null) { return 1; }
                if (!Manufacture.Equals(other.Manufacture)) { return Manufacture.CompareTo(other.Manufacture); }
            }

            if (!(Device == null && other.Device == null)) {
                if (Device == null) { return -1; }
                if (other.Device == null) { return 1; }
                if (!Device.Equals(other.Device)) { return Device.CompareTo(other.Device); }
            }

            if (!(Resolution == null && other.Resolution == null)) {
                if (Resolution == null) { return -1; }
                if (other.Resolution == null) { return 1; }
                if (!Resolution.Equals(other.Resolution)) { return Resolution.CompareTo(other.Resolution); }
            }

            if (!(AppVersion == null && other.AppVersion == null)) {
                if (AppVersion == null) { return -1; }
                if (other.AppVersion == null) { return 1; }
                if (!AppVersion.Equals(other.AppVersion)) { return AppVersion.CompareTo(other.AppVersion); }
            }

            if (!(Orientation == null && other.Orientation == null)) {
                if (Orientation == null) { return -1; }
                if (other.Orientation == null) { return 1; }
                if (!Orientation.Equals(other.Orientation)) { return Orientation.CompareTo(other.Orientation); }
            }

            if (!(RamCurrent == null && other.RamCurrent == null)) {
                if (RamCurrent == null) { return -1; }
                if (other.RamCurrent == null) { return 1; }
                if (!RamCurrent.Equals(other.RamCurrent)) { return RamCurrent.Value.CompareTo(other.RamCurrent.Value); }
            }

            if (!(RamTotal == null && other.RamTotal == null)) {
                if (RamTotal == null) { return -1; }
                if (other.RamTotal == null) { return 1; }
                if (!RamTotal.Equals(other.RamTotal)) { return RamTotal.Value.CompareTo(other.RamTotal.Value); }
            }

            if (!(Online == null && other.Online == null)) {
                if (Online == null) { return -1; }
                if (other.Online == null) { return 1; }
                if (!Online.Equals(other.Online)) { return Online.Value.CompareTo(other.Online.Value); }
            }

            if (!(Name == null && other.Name == null)) {
                if (Name == null) { return -1; }
                if (other.Name == null) { return 1; }
                if (!Name.Equals(other.Name)) { return Name.CompareTo(other.Name); }
            }

            if (!(Error == null && other.Error == null)) {
                if (Error == null) { return -1; }
                if (other.Error == null) { return 1; }
                if (!Error.Equals(other.Error)) { return Error.CompareTo(other.Error); }
            }

            if (!NonFatal.Equals(other.NonFatal)) { return NonFatal.CompareTo(other.NonFatal); }

            if (!(Logs == null && other.Logs == null)) {
                if (Logs == null) { return -1; }
                if (other.Logs == null) { return 1; }
                if (!Logs.Equals(other.Logs)) { return Logs.CompareTo(other.Logs); }
            }

            if (!Run.Equals(other.Run)) { return Run.CompareTo(other.Run); }

            if (!(OS == null && other.OS == null)) {
                if (OS == null) { return -1; }
                if (other.OS == null) { return 1; }
                if (!OS.Equals(other.OS)) { return OS.CompareTo(other.OS); }
            }

            if (!(Custom == null && other.Custom == null)) {
                if (Custom == null) { return -1; }
                if (other.Custom == null) { return 1; }
                if (!Custom.Count.Equals(other.Custom.Count)) { return Custom.Count.CompareTo(other.Custom.Count); }

                foreach (var a in Custom.Keys) {
                    if (!other.Custom.ContainsKey(a)) { return -1; }
                    if (!Custom[a].Equals(other.Custom[a])) { return Custom[a].CompareTo(other.Custom[a]); }
                }
            }

            return 0;
        }
    }
}
