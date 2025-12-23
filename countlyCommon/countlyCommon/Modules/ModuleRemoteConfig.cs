using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq;
using System.Threading.Tasks;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static CountlySDK.CountlyCommon.CountlyBase;
using static CountlySDK.Entities.EntityBase.DeviceBase;
using static CountlySDK.Helpers.TimeHelper;

namespace CountlySDK.CountlyCommon
{
    internal class ModuleRemoteConfig : RemoteConfig
    {
        private readonly IRequestHelperImpl requestHelper;
        private readonly CountlyBase _cly;
        private IDictionary<string, object> rcValues = new Dictionary<string, object>();
        private const object rcLock = new object();
        private bool autoEnrollEnabled = true;

        public ModuleRemoteConfig(CountlyBase countly)
        {
            _cly = countly;
            requestHelper = new IRequestHelperImpl(Countly.Instance);
        }

        private string GetURLEncodedJson(object obj)
        {
            return UtilityHelper.EncodeDataForURL(JsonConvert.SerializeObject(obj, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
        }

        private string CreateBaseRequest(string extraParams)
        {
            TimeInstant timeInstant = _cly.timeHelper.GetUniqueInstant();
            string did = UtilityHelper.EncodeDataForURL(_cly.device.DeviceID);
            string app = UtilityHelper.EncodeDataForURL(requestHelper.GetAppKey());
            return string.Format("/i?app_key={0}&device_id={1}&sdk_version={2}&sdk_name={3}&hour={4}&dow={5}&tz={6}&timestamp={7}&t=0&av={8}{9}", app, did, requestHelper.GetSDKVersion(), requestHelper.GetSDKName(), timeInstant.Hour, timeInstant.Dow, timeInstant.Timezone, timeInstant.Timestamp, requestHelper.GetAppVersion(), extraParams);
        }

        private string CreateQueryParamsFromDictionary(IDictionary<string, string> parameters)
        {
            string query = string.Empty;

            foreach (KeyValuePair<string, string> kvp in parameters) {
                query += string.Format("&{0}={1}", kvp.Key, UtilityHelper.EncodeDataForURL(kvp.Value));
            }

            return query;
        }

        public Task FetchRemoteConfig(List<string> includeKeys = null, List<string> omitKeys = null)
        {
            UtilityHelper.CountlyLogging("[ModuleRemoteConfig] FetchRemoteConfig called");

            IDictionary<string, string> rcParams = new Dictionary<string, string>();

            if (includeKeys != null && includeKeys.Count > 0) {
                rcParams.Add("keys", JsonConvert.SerializeObject(includeKeys));
            }

            if (omitKeys != null && omitKeys.Count > 0) {
                rcParams.Add("omit_keys", JsonConvert.SerializeObject(omitKeys));
            }

            if(autoEnrollEnabled) {
                rcParams.Add("oi", "1");
            }

            string extraParams = string.Empty;
            if (rcParams.Count > 0) {
                extraParams = CreateQueryParamsFromDictionary(rcParams);
            }

            string requestUrl = CreateBaseRequest(_cly.device.DeviceID, requestHelper.GetAppKey(), extraParams);

            return Task.Run(async () =>
            {
                RequestResult result = await requestHelper.SendRequestAsync(requestUrl);
                if (result.IsSuccess && result.ResponseData != null) {
                    try {
                        JObject json = JObject.Parse(result.ResponseData);
                        JObject rcObj = (JObject)json["rc"];
                        lock(rcLock) {
                            rcValues.Clear();
                            rcValues = rcObj.ToObject<Dictionary<string, object>>();
                        }
                        UtilityHelper.CountlyLogging("[ModuleRemoteConfig] FetchRemoteConfig succeeded, fetched " + rcValues.Count + " keys.");
                    } catch (Exception ex) {
                        UtilityHelper.CountlyLogging("[ModuleRemoteConfig] FetchRemoteConfig failed to parse response: " + ex.Message, LogLevel.ERROR);
                    }
                } else {
                    UtilityHelper.CountlyLogging("[ModuleRemoteConfig] FetchRemoteConfig request failed: " + result.ErrorMessage, LogLevel.ERROR);
                }
            });
        }

        public IDictionary<string, object> GetRemoteConfigValues()
        {
            lock(rcLock) {
                return new Dictionary<string, object>(rcValues);
            }
        }
    }

    public interface RemoteConfig
    {
        // <summary>
        /// 
        /// </summary>
        /// <param name="includeKeys"></param>
        /// <param name="omitKeys"></param>
        Task FetchRemoteConfig(List<string> includeKeys = null, List<string> omitKeys = null);

        IDictionary<string, object> GetRemoteConfigValues();

        object this[string key] {
            get {
                lock(rcLock) {
                    IDictionary<string, object> values = GetRemoteConfigValues();
                    if (values.ContainsKey(key)) {
                        return values[key];
                    }
                    return null;
                }
            }
        }
    }

}

