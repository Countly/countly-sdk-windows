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
using System.Runtime.Serialization;

namespace CountlySDK.Entities
{
    [DataContractAttribute]
    internal class ExceptionEvent
    {
        //device metrics

        [DataMemberAttribute]
        [JsonProperty("_os")]
        public string OS { get; set; }

        [DataMemberAttribute]
        [JsonProperty("_os_version")]
        public string OSVersion { get; set; }

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
        [JsonProperty("_ram_current")]
        public long RamCurrent { get; set; }

        [DataMemberAttribute]
        [JsonProperty("_ram_total")]
        public long RamTotal { get; set; }

        //bools

        [DataMemberAttribute]
        [JsonProperty("_online")]
        public bool Online { get; set; }

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

        [JsonProperty("_custom")]
        public Dictionary<string, string> Custom { get; set; }

        /// <summary>
        /// Custom data ready for datacontract serializer
        /// </summary>
        [JsonIgnore]
        [DataMemberAttribute]
        internal List<KeyValuePair<string, string>> _custom
        {
            get
            {
                if (Custom == null) return null;

                List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();

                foreach (var pair in Custom)
                {
                    list.Add(new KeyValuePair<string, string>(pair.Key, pair.Value));
                }

                return list;
            }
            set
            {
                if (value == null)
                {
                    Custom = new Dictionary<string, string>();
                }
                else
                {
                    Dictionary<string, string> dictionary = new Dictionary<string, string>();

                    foreach (var pair in value)
                    {
                        dictionary.Add(pair.Key, pair.Value);
                    }

                    Custom = dictionary;
                }
            }
        }

        public ExceptionEvent(string Error, string StackTrace, bool fatal, string breadcrumb, TimeSpan run, string appVersion, Dictionary<string, string> customInfo)
        {
            //device metrics
            OS = CountlySDK.Entities.Device.OS;
            OSVersion = CountlySDK.Entities.Device.OSVersion;
            Device = CountlySDK.Entities.Device.DeviceName;
            Resolution = CountlySDK.Entities.Device.Resolution;
            AppVersion = appVersion;

            //state of device
            RamCurrent = CountlySDK.Entities.Device.RamCurrent;
            RamTotal = CountlySDK.Entities.Device.RamTotal;

            //bools
            Online = CountlySDK.Entities.Device.Online;

            //error info
            this.Name = Error;
            this.Error = StackTrace;
            NonFatal = !fatal;
            Logs = breadcrumb;
            Run = (long)run.TotalSeconds;
            Custom = customInfo;
        }
    }
}
