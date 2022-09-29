﻿using CountlySDK.CountlyCommon.Entities;
using CountlySDK.CountlyCommon.Server;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon.Server.Responses;
using System.Globalization;
using static CountlySDK.Helpers.TimeHelper;

namespace CountlySDK.CountlyCommon.Server
{
    abstract class ApiBase
    {
        internal const int maxLengthForDataInUrl = 2000;

        protected abstract Task DoSleep(int sleepTime);

        public async Task<RequestResult> SendSession(string serverUrl, SessionEvent sessionEvent, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = string.Empty;

            if (userDetails != null) {
                userDetailsJson = "&user_details=" + UtilityHelper.EncodeDataForURL(JsonConvert.SerializeObject(userDetails, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
            }

            return await Call(serverUrl + sessionEvent.Content + userDetailsJson);
        }

        public async Task<RequestResult> SendEvents(string serverUrl, string appKey, DeviceId deviceId, string sdkVersion, string sdkName, List<CountlyEvent> events, TimeInstant timeInstant, CountlyUserDetails userDetails = null)
        {
            string eventsJson = JsonConvert.SerializeObject(events, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });

            string userDetailsJson = string.Empty;

            if (userDetails != null) {
                userDetailsJson = "&user_details=" + UtilityHelper.EncodeDataForURL(JsonConvert.SerializeObject(userDetails, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
            }

            string did = UtilityHelper.EncodeDataForURL(deviceId.deviceId);
            return await Call(string.Format("{0}/i?app_key={1}&device_id={2}&events={3}&sdk_version={4}&sdk_name={5}&hour={6}&dow={7}&tz={8}&timestamp={9}{10}", serverUrl, appKey, did, UtilityHelper.EncodeDataForURL(eventsJson), sdkVersion, sdkName, timeInstant.Hour, timeInstant.Dow, timeInstant.Timezone, timeInstant.Timestamp, userDetailsJson));
        }

        public async Task<RequestResult> SendException(string serverUrl, string appKey, DeviceId deviceId, string sdkVersion, string sdkName, ExceptionEvent exception, TimeInstant timeInstant)
        {
            string exceptionJson = JsonConvert.SerializeObject(exception, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            string did = UtilityHelper.EncodeDataForURL(deviceId.deviceId);
            return await Call(string.Format("{0}/i?app_key={1}&device_id={2}&crash={3}&sdk_version={4}&sdk_name={5}&hour={6}&dow={7}&tz={8}&timestamp={9}", serverUrl, appKey, did, UtilityHelper.EncodeDataForURL(exceptionJson), sdkVersion, sdkName, timeInstant.Hour, timeInstant.Dow, timeInstant.Timezone, timeInstant.Timestamp));
        }

        public async Task<RequestResult> UploadUserDetails(string serverUrl, string appKey, DeviceId deviceId, string sdkVersion, string sdkName, TimeInstant timeInstant, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = string.Empty;

            if (userDetails != null) {
                userDetailsJson = JsonConvert.SerializeObject(userDetails, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }

            string did = UtilityHelper.EncodeDataForURL(deviceId.deviceId);
            return await Call(string.Format("{0}/i?app_key={1}&device_id={2}&user_details={3}&sdk_version={4}&sdk_name={5}&hour={6}&dow={7}&tz={8}&timestamp={9}", serverUrl, appKey, did, userDetailsJson, sdkVersion, sdkName, timeInstant.Hour, timeInstant.Dow, timeInstant.Timezone, timeInstant.Timestamp));
        }

        public async Task<RequestResult> UploadUserPicture(string serverUrl, string appKey, DeviceId deviceId, string sdkVersion, string sdkName, Stream imageStream, TimeInstant timeInstant, CountlyUserDetails userDetails = null)
        {
            string userDetailsJson = string.Empty;

            if (userDetails != null) {
                userDetailsJson = "=" + JsonConvert.SerializeObject(userDetails, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            }

            string did = UtilityHelper.EncodeDataForURL(deviceId.deviceId);
            return await Call(string.Format("{0}/i?app_key={1}&device_id={2}&user_details{3}&sdk_version={4}&sdk_name={5}&hour={6}&dow={7}&tz={8}&timestamp={9}", serverUrl, appKey, did, userDetailsJson, sdkVersion, sdkName, timeInstant.Hour, timeInstant.Dow, timeInstant.Timezone, timeInstant.Timestamp), imageStream);
        }

        public async Task<RequestResult> SendStoredRequest(string serverUrl, StoredRequest request)
        {
            Debug.Assert(serverUrl != null);
            Debug.Assert(request != null);

            return await Call(string.Format("{0}{1}", serverUrl, request.Request));
        }

        /// <summary>
        /// Platform specific task wrapper
        /// </summary>
        /// <param name="address"></param>
        /// <param name="imageData"></param>
        /// <returns></returns>
        protected abstract Task<RequestResult> Call(string address, Stream imageData = null);

        /// <summary>
        /// Common job handler
        /// </summary>
        /// <param name="address"></param>
        /// <param name="imageData"></param>
        /// <returns></returns>
        protected async Task<RequestResult> CallJob(string address, Stream imageData = null)
        {
            Debug.Assert(address != null);
            TaskCompletionSource<RequestResult> tcs = new TaskCompletionSource<RequestResult>();

            try {
                string rData = null;

                if (address.Length > maxLengthForDataInUrl) {
                    //request url was too long, split off the data and pass it as form data
                    string[] splitData = address.Split('?');
                    address = splitData[0];
                    rData = splitData[1];
                }

                RequestResult requestResult = await RequestAsync(address, rData, imageData);
                tcs.SetResult(requestResult);

                if (requestResult.responseText != null) {
                    UtilityHelper.CountlyLogging(requestResult.responseText);
                } else {
                    UtilityHelper.CountlyLogging("Received null response");
                }
            } catch (Exception ex) {
                RequestResult requestResult = new RequestResult();
                requestResult.responseText = "Encountered an exception while making a request, " + ex;
                UtilityHelper.CountlyLogging(requestResult.responseText);
            }

            return await tcs.Task;
        }

        /// <summary>
        /// Platform specific networking code
        /// </summary>
        /// <param name="address"></param>
        /// <param name="requestData"></param>
        /// <param name="imageData"></param>
        /// <returns></returns>
        protected abstract Task<RequestResult> RequestAsync(string address, string requestData, Stream imageData = null);
    }
}
