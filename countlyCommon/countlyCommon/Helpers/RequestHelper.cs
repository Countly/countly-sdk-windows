using CountlySDK.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using static CountlySDK.CountlyCommon.CountlyBase;
using static CountlySDK.Helpers.TimeHelper;

namespace CountlySDK.CountlyCommon.Helpers
{
    internal class RequestHelper
    {
        internal static string CreateLocationRequest(string bRequest, string gpsLocation = null, string ipAddress = null, string country_code = null, String city = null)
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

            KeyValuePair<ConsentFeatures, bool>[] entryChanges = updatedConsentChanges.ToArray();

            for (int a = 0; a < entryChanges.Length; a++) {
                if (a != 0) { consentChanges += ","; }

                KeyValuePair<ConsentFeatures, bool> feature = entryChanges[a];
                switch (feature.Key) {
                    case ConsentFeatures.Crashes:
                        consentChanges += "\"crashes\":" + (feature.Value ? "true" : "false");
                        break;
                    case ConsentFeatures.Events:
                        consentChanges += "\"events\":" + (feature.Value ? "true" : "false");
                        break;
                    case ConsentFeatures.Location:
                        consentChanges += "\"location\":" + (feature.Value ? "true" : "false");
                        break;
                    case ConsentFeatures.Sessions:
                        consentChanges += "\"sessions\":" + (feature.Value ? "true" : "false");
                        break;
                    case ConsentFeatures.Users:
                        consentChanges += "\"users\":" + (feature.Value ? "true" : "false");
                        break;
                    case ConsentFeatures.Views:
                        consentChanges += "\"views\":" + (feature.Value ? "true" : "false");
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

        internal static string CreateBaseRequest(string appKey, string deviceId, string sdkVersion, string sdkName, TimeInstant instant)
        {
            return string.Format("/i?app_key={0}&device_id={1}&timestamp={2}&sdk_version={3}&sdk_name={4}&hour={5}&dow={6}&tz={7}", appKey, deviceId, instant.Timestamp, sdkVersion, sdkName, instant.Hour, instant.Dow, instant.Timezone);
        }
    }
}
