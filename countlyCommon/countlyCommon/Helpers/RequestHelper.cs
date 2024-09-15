using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using static CountlySDK.CountlyCommon.CountlyBase;
using static CountlySDK.Helpers.TimeHelper;

namespace CountlySDK.CountlyCommon.Helpers
{
    internal class RequestHelper
    {

        internal interface IRequestHelper
        {
            string GetAppKey();
            string GetSDKName();
            string GetSDKVersion();
            string GetAppVersion();
            Task<DeviceId> GetDeviceId();
            TimeInstant GetTimeInstant();
            string GetSalt();
        }

        private readonly IRequestHelper _interface;

        internal RequestHelper(IRequestHelper exposed)
        {
            _interface = exposed;
        }

        internal async Task<Dictionary<string, object>> GetBaseParams()
        {
            TimeInstant timeInstant = _interface.GetTimeInstant();
            DeviceId deviceId = await _interface.GetDeviceId();
            Dictionary<string, object> baseParams = new Dictionary<string, object>
             {
                {"app_key", _interface.GetAppKey()},
                {"device_id", deviceId.deviceId},
                {"t", deviceId.Type()},
                {"sdk_name", _interface.GetSDKName()},
                {"sdk_version", _interface.GetSDKVersion()},
                {"timestamp", timeInstant.Timestamp},
                {"dow", timeInstant.Dow},
                {"hour", timeInstant.Hour},
                {"tz", timeInstant.Timezone},
            };
            if (!string.IsNullOrEmpty(_interface.GetAppVersion())) {
                baseParams.Add("av", _interface.GetAppVersion());
            }

            return baseParams;
        }


        internal string CreateConsentUpdateRequest(Dictionary<ConsentFeatures, bool> updatedConsentChanges)
        {
            Debug.Assert(updatedConsentChanges != null);
            Debug.Assert(updatedConsentChanges.Count > 0);

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

            return consentChanges;
        }

        /// <summary>
        /// Builds request by adding Base params into supplied queryParams parameters.
        /// The data is appended in the URL.
        /// </summary>
        /// <param name="queryParams"></param>
        /// <returns></returns>
        internal async Task<string> BuildRequest(IDictionary<string, object> queryParams = null)
        {
            IDictionary<string, object> requestData = await GetBaseParams();

            if (queryParams != null && queryParams.Count() > 0) {
                foreach (KeyValuePair<string, object> item in queryParams) {
                    if (!requestData.ContainsKey(item.Key)) {
                        requestData.Add(item.Key, item.Value);
                    }
                }
            }

            string data = BuildQueryString(requestData);
            if (!UtilityHelper.IsNullOrEmptyOrWhiteSpace(_interface.GetSalt())) {
                data = data + "&checksum256=" + UtilityHelper.ComputeChecksum(data + _interface.GetSalt());
            }

            return data;
        }

        /// <summary>
        /// Builds query string using supplied queryParams parameters.
        /// </summary>
        /// <param name="queryParams"></param>
        /// <returns></returns>
        internal string BuildQueryString(IDictionary<string, object> queryParams)
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

        /// <summary>
        /// Converts and object that has JsonSerializer annotations to String
        /// No formatting and null values are ignored
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal static string Json(object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }
    }
}
