using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.CountlyCommon.Server.Responses;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace CountlySDK.CountlyCommon
{
    internal class ModuleRemoteConfig : RemoteConfig
    {
        private readonly RequestHelper requestHelper;
        private readonly string ServerUrl;
        private IDictionary<string, RCData> rcValues = new Dictionary<string, RCData>();
        private object RCLock = new object();
        private bool AutoEnrollEnabled = true;

        public ModuleRemoteConfig(RequestHelper requestHelper, string serverUrl)
        {
            this.requestHelper = requestHelper;
            ServerUrl = serverUrl;
        }

        public Task DownloadKeys(List<string> keysToInclude = null, List<string> keysToOmit = null)
        {
            UtilityHelper.CountlyLogging("[ModuleRemoteConfig] FetchRemoteConfig called");

            IDictionary<string, object> rcParams = new Dictionary<string, object> {
                { "method", "rc" },
                { "metrics", Countly.Instance.GetSessionMetrics().ToString() }
            };

            if (keysToInclude != null && keysToInclude.Count > 0) {
                rcParams.Add("keys", keysToInclude);
            } else if (keysToOmit != null && keysToOmit.Count > 0) {
                rcParams.Add("omit_keys", keysToOmit);
            }

            if (AutoEnrollEnabled) {
                rcParams.Add("oi", "1");
            }


            return Task.Run(async () => {
                RequestResult requestResult = await Api.Instance.SendDirectRequest(ServerUrl, await requestHelper.BuildRequest(rcParams));
                UtilityHelper.CountlyLogging("[ModuleRemoteConfig] FetchRemoteConfig, got server response code: [" + requestResult.responseCode + "], text: [" + requestResult.responseText + "]");
                if (requestResult.responseCode == 200 && requestResult.responseText != null) {
                    try {
                        Dictionary<string, object> newValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestResult.responseText);
                        lock (RCLock) {
                            rcValues.Clear();
                            foreach (KeyValuePair<string, object> kv in newValues) {
                                rcValues[kv.Key] = new RCData {
                                    Value = kv.Value,
                                    IsCurrentUsersData = true
                                };
                            }
                        }
                        UtilityHelper.CountlyLogging("[ModuleRemoteConfig] FetchRemoteConfig succeeded, fetched " + rcValues.Count + " keys.");
                    } catch (Exception ex) {
                        UtilityHelper.CountlyLogging("[ModuleRemoteConfig] FetchRemoteConfig failed to parse response: " + ex.Message, LogLevel.ERROR);
                    }
                } else {
                    UtilityHelper.CountlyLogging("[ModuleRemoteConfig] FetchRemoteConfig request failed request is not success ", LogLevel.ERROR);
                }
            });
        }

        public IDictionary<string, RCData> GetValues()
        {
            lock (RCLock) {
                return new Dictionary<string, RCData>(rcValues);
            }
        }

        public RCData GetValue(string key)
        {
            GetValues().TryGetValue(key, out RCData value);
            return value;
        }
    }

    internal class MockRemoteConfig : RemoteConfig
    {
        internal MockRemoteConfig()
        {

        }

        Task RemoteConfig.DownloadKeys(List<string> includeKeys, List<string> omitKeys)
        {
            return Task.CompletedTask;
        }

        RCData RemoteConfig.GetValue(string key)
        {
            return new RCData();
        }

        IDictionary<string, RCData> RemoteConfig.GetValues()
        {
            return new Dictionary<string, RCData>();
        }
    }

    /// <summary>
    /// Represents a single Remote Config entry and its associated metadata.
    /// </summary>
    public class RCData
    {
        /// <summary>
        /// The value associated with the Remote Config key.
        /// This is a normalized CLR type (e.g. string, int, bool, double, or null).
        /// </summary>
        public object Value { get; internal set; }

        /// <summary>
        /// Indicates whether this value is specific to the current user
        /// </summary>
        public bool IsCurrentUsersData { get; internal set; }
    }

    /// <summary>
    /// Defines the contract for accessing and managing Remote Config data.
    /// </summary>
    public interface RemoteConfig
    {
        /// <summary>
        /// Downloads Remote Config values from the server.
        /// </summary>
        /// <param name="includeKeys">
        /// Optional list of keys to explicitly include in the download.
        /// When specified, only these keys will be requested.
        /// </param>
        /// <param name="omitKeys">
        /// Optional list of keys to exclude from the download.
        /// These keys will be ignored even if they exist on the server.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous download operation.
        /// </returns>
        Task DownloadKeys(List<string> includeKeys = null, List<string> omitKeys = null);

        /// <summary>
        /// Returns a snapshot of all available Remote Config values.
        /// </summary>
        /// <returns>
        /// A dictionary mapping Remote Config keys to their corresponding data objects.
        /// </returns>
        IDictionary<string, RCData> GetValues();

        /// <summary>
        /// Returns the Remote Config data associated with the specified key.
        /// </summary>
        /// <param name="key">
        /// The Remote Config key to retrieve.
        /// </param>
        /// <returns>
        /// The corresponding <see cref="RCData"/> instance if the key exists;
        /// otherwise, <c>null</c>.
        /// </returns>
        RCData GetValue(string key);
    }
}