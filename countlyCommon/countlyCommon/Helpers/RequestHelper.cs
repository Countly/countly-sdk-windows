using CountlySDK.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace CountlySDK.CountlyCommon.Helpers
{
    internal class RequestHelper
    {
        internal static String CreateLocationRequest(String bRequest, String gpsLocation = null, String ipAddress = null, String country_code = null, String city = null)
        {
            Debug.Assert(bRequest != null);
            if (bRequest == null) return null;            

            if (gpsLocation != null || ipAddress != null || country_code != null || city != null)
            {
                String res = null;

                String rGps = gpsLocation == null ? null : String.Format("&location={0}", UtilityHelper.EncodeDataForURL(gpsLocation));
                String rIp = ipAddress == null ? null : String.Format("&ip={0}", UtilityHelper.EncodeDataForURL(ipAddress));
                String rCountry = country_code == null ? null : String.Format("&country_code={0}", UtilityHelper.EncodeDataForURL(country_code));
                String rCity = city == null ? null : String.Format("&city={0}", UtilityHelper.EncodeDataForURL(city));

                res = String.Format("{0}{1}{2}{3}{4}", bRequest, rGps, rIp, rCountry, rCity);
                return res;
            }

            return null;
        }

        internal static String CreateDeviceIdMergeRequest(String bRequest, String oldId)
        {
            Debug.Assert(bRequest != null);
            Debug.Assert(oldId != null);
            if (bRequest == null) return null;

            String res = String.Format("{0}&old_device_id={1}", bRequest, oldId);
            return res;
        }

        internal static String CreateConsentUpdateRequest(String bRequest, Dictionary<ConsentFeatures, bool> updatedConsentChanges)
        {
            Debug.Assert(bRequest != null);
            Debug.Assert(updatedConsentChanges != null);
            Debug.Assert(updatedConsentChanges.Count > 0);
            if (bRequest == null) return null;

            String consentChanges = "{";

            KeyValuePair<ConsentFeatures, bool>[] entryChanges = updatedConsentChanges.ToArray();

            for(int a = 0; a < entryChanges.Length; a++)
            {
                if(a != 0) { consentChanges += ","; }

                KeyValuePair<ConsentFeatures, bool> feature = entryChanges[a];
                switch (feature.Key)
                {
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

            String res = String.Format("{0}&consent={1}", bRequest, UtilityHelper.EncodeDataForURL(consentChanges));
            return res;
        }

        internal static String CreateBaseRequest(string appKey, string deviceId, string sdkVersion, string sdkName, long? timestamp = null)
        {
            DateTime dateTime = DateTime.Now.ToUniversalTime();
            timestamp = TimeHelper.ToUnixTime(dateTime);

            int hour = dateTime.TimeOfDay.Hours;
            int dayOfWeek = (int)dateTime.DayOfWeek;
            string timezone = TimeZoneInfo.Local.GetUtcOffset(dateTime).TotalMinutes.ToString(CultureInfo.InvariantCulture);

            return String.Format("/i?app_key={0}&device_id={1}&timestamp={2}&sdk_version={3}&sdk_name={4}&hour={5}&dow={6}&tz={7}", appKey, deviceId, timestamp, sdkVersion, sdkName, hour, dayOfWeek, timezone);
        }
    }
}
