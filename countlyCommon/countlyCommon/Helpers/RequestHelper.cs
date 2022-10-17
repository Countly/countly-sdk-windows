using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.Helpers;
using static CountlySDK.CountlyCommon.CountlyBase;
using static CountlySDK.Helpers.TimeHelper;

namespace CountlySDK.CountlyCommon.Helpers
{
    internal class RequestHelper
    {
        internal static string CreateLocationRequest(string bRequest, string gpsLocation = null, string ipAddress = null, string country_code = null, string city = null)
        {
            Debug.Assert(bRequest != null);
            if (bRequest == null) {
                return null;
            }

            if (gpsLocation != null || ipAddress != null || country_code != null || city != null) {
                string res = null;

                string rGps = gpsLocation == null ? null : string.Format("&location={0}", UtilityHelper.EncodeDataForURL(gpsLocation));
                string rIp = ipAddress == null ? null : string.Format("&ip={0}", UtilityHelper.EncodeDataForURL(ipAddress));
                string rCountry = country_code == null ? null : string.Format("&country_code={0}", UtilityHelper.EncodeDataForURL(country_code));
                string rCity = city == null ? null : string.Format("&city={0}", UtilityHelper.EncodeDataForURL(city));

                res = string.Format("{0}{1}{2}{3}{4}", bRequest, rGps, rIp, rCountry, rCity);
                return res;
            }

            return null;
        }

        internal static string CreateDeviceIdMergeRequest(string bRequest, string oldId)
        {
            Debug.Assert(bRequest != null);
            Debug.Assert(oldId != null);
            if (bRequest == null) {
                return null;
            }

            string res = string.Format("{0}&old_device_id={1}", bRequest, oldId);
            return res;
        }

        internal static string CreateConsentUpdateRequest(string bRequest, Dictionary<ConsentFeatures, bool> updatedConsentChanges)
        {
            Debug.Assert(bRequest != null);
            Debug.Assert(updatedConsentChanges != null);
            Debug.Assert(updatedConsentChanges.Count > 0);
            if (bRequest == null) {
                return null;
            }

            string consentChanges = "{";
            ConsentFeatures[] consents = System.Enum.GetValues(typeof(ConsentFeatures)).Cast<ConsentFeatures>().ToArray();

            for (int a = 0; a < consents.Length; a++) {
                if (a != 0) { consentChanges += ","; }

                ConsentFeatures key = consents[a];
                bool value = updatedConsentChanges.ContainsKey(key) && updatedConsentChanges[key];

                switch (key) {
                    case ConsentFeatures.Crashes:
                        consentChanges += "\"crashes\":" + (value ? "true" : "false");
                        break;
                    case ConsentFeatures.Events:
                        consentChanges += "\"events\":" + (value ? "true" : "false");
                        break;
                    case ConsentFeatures.Location:
                        consentChanges += "\"location\":" + (value ? "true" : "false");
                        break;
                    case ConsentFeatures.Sessions:
                        consentChanges += "\"sessions\":" + (value ? "true" : "false");
                        break;
                    case ConsentFeatures.Users:
                        consentChanges += "\"users\":" + (value ? "true" : "false");
                        break;
                    case ConsentFeatures.Views:
                        consentChanges += "\"views\":" + (value ? "true" : "false");
                        break;
                    case ConsentFeatures.Feedback:
                        consentChanges += "\"feedback\":" + (value ? "true" : "false");
                        break;
                    case ConsentFeatures.StarRating:
                        consentChanges += "\"star-rating\":" + (value ? "true" : "false");
                        break;
                    case ConsentFeatures.Push:
                        consentChanges += "\"push\":" + (value ? "true" : "false");
                        break;
                    case ConsentFeatures.RemoteConfig:
                        consentChanges += "\"remote-config\":" + (value ? "true" : "false");
                        break;
                    default:
                        consentChanges += "\"unknown\":false";
                        break;
                }
            }

            consentChanges += "}";

            string res = string.Format("{0}&consent={1}", bRequest, UtilityHelper.EncodeDataForURL(consentChanges));
            return res;
        }

        internal static string CreateBaseRequest(string appKey, DeviceId deviceId, string sdkVersion, string sdkName, TimeInstant instant)
        {
            string did = UtilityHelper.EncodeDataForURL(deviceId.deviceId);
            return string.Format("/i?app_key={0}&device_id={1}&timestamp={2}&sdk_version={3}&sdk_name={4}&hour={5}&dow={6}&tz={7}&t={8}", appKey, did, instant.Timestamp, sdkVersion, sdkName, instant.Hour, instant.Dow, instant.Timezone, deviceId.Type());
        }

        /// <summary>
        /// Builds request by adding Base params into supplied queryParams parameters.
        /// The data is appended in the URL.
        /// </summary>
        /// <param name="queryParams"></param>
        /// <returns></returns>
        internal static string BuildRequest(IDictionary<string, object> baseParams, IDictionary<string, object> queryParams)
        {
            //Metrics added to each request
            IDictionary<string, object> requestData = baseParams;
            foreach (KeyValuePair<string, object> item in queryParams) {
                requestData.Add(item.Key, item.Value);
            }

            string data = BuildQueryString(requestData);

            return data;
        }

        /// <summary>
        /// Builds query string using supplied queryParams parameters.
        /// </summary>
        /// <param name="queryParams"></param>
        /// <returns></returns>
        private static string BuildQueryString(IDictionary<string, object> queryParams)
        {
            //  Dictionary<string, object> queryParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            StringBuilder requestStringBuilder = new StringBuilder();

            //Query params supplied for creating request
            foreach (KeyValuePair<string, object> item in queryParams) {
                if (!string.IsNullOrEmpty(item.Key) && item.Value != null) {
                    requestStringBuilder.AppendFormat(requestStringBuilder.Length == 0 ? "{0}={1}" : "&{0}={1}", item.Key,
                        Convert.ToString(item.Value));
                }
            }

            string result = "/i?" + requestStringBuilder.ToString();

            return Uri.EscapeUriString(result);
        }
    }
}
