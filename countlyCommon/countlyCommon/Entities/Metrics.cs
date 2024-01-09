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
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace CountlySDK.Entities
{
    /// <summary>
    /// Holds device-specific info in json-ready format
    /// </summary>
    internal class Metrics
    {

        public IDictionary<string, string> MetricsDict = new Dictionary<string, string>();

        /// <summary>
        /// Creates Metrics object with provided values
        /// </summary>
        /// <param name="OS">Name of the current operating system</param>
        /// <param name="OSVersion">Current operating system version</param>
        /// <param name="Device">Current device model</param>
        /// <param name="Resolution">Device resolution</param>
        /// <param name="Carrier">Cellular mobile operator</param>
        /// <param name="AppVersion">Application version</param>
        public Metrics(string OS, string OSVersion, string Device, string Resolution, string Carrier, string AppVersion, string Locale)
        {
            AddToMetrics("_os", OS);
            AddToMetrics("_os_version", OSVersion);
            AddToMetrics("_device", Device);
            AddToMetrics("_resolution", Resolution);
            AddToMetrics("_carrier", Carrier);
            AddToMetrics("_app_version", AppVersion);
            AddToMetrics("_locale", Locale);
        }

        public void SetMetricOverride(IDictionary<string, string> metricOverride)
        {
            if (metricOverride == null || metricOverride.Count < 1) {
                return;
            }
            foreach (KeyValuePair<string, string> item in metricOverride) {
                AddToMetrics(item.Key, item.Value);
            }
        }

        private void AddToMetrics(string key, string value)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)) {
                return;
            }

            MetricsDict[key] = value;
        }

        public string GetMetric(string key)
        {
            return MetricsDict[key];
        }

        /// <summary>
        /// Serializes object into json
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(MetricsDict);
        }
    }
}
