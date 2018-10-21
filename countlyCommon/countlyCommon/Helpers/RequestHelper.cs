using CountlySDK.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CountlySDK.CountlyCommon.Helpers
{
    internal class RequestHelper
    {
        internal static String CreateLocationRequest(String bRequest, String gpsLocation = null, String ipAddress = null, String country_code = null, String city = null)
        {
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

        internal static String CreateBaseRequest(string appKey, string deviceId, long? timestamp = null)
        {
            if (timestamp == null)
            {
                timestamp = TimeHelper.ToUnixTime(DateTime.Now.ToUniversalTime());
            }
            return String.Format("/i?app_key={0}&device_id={1}&timestamp={2}", appKey, deviceId, timestamp);
        }
    }
}
