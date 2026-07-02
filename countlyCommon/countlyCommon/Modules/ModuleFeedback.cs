using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.CountlyCommon.Server.Responses;
using CountlySDK.Helpers;
using Newtonsoft.Json.Linq;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace CountlySDK.CountlyCommon
{
    internal class ModuleFeedback : Feedback
    {
        private readonly RequestHelper requestHelper;
        private readonly string ServerUrl;

        // Server-contract event keys. CountlyBase declares these as `private const`, so they
        // are re-declared here for the module (the exact string values must match, because
        // CheckConsentOnKey routes them to Feedback/StarRating consent).
        private const string NPS_EVENT_KEY = "[CLY]_nps";
        private const string SURVEY_EVENT_KEY = "[CLY]_survey";
        private const string STAR_RATING_EVENT_KEY = "[CLY]_star_rating";

        public ModuleFeedback(RequestHelper requestHelper, string serverUrl)
        {
            this.requestHelper = requestHelper;
            ServerUrl = serverUrl;
        }

        public async Task<CountlyFeedbackWidget[]> GetAvailableFeedbackWidgets()
        {
            UtilityHelper.CountlyLogging("[ModuleFeedback] GetAvailableFeedbackWidgets called");

            IDictionary<string, object> parameters = new Dictionary<string, object> {
                { "method", "feedback" }
            };

            RequestResult requestResult = await Api.Instance.SendDirectRequest(ServerUrl, await requestHelper.BuildRequest(parameters));

            if (requestResult != null && requestResult.responseCode == 200 && requestResult.responseText != null) {
                return FeedbackWidgetParser.ParseAvailableWidgets(requestResult.responseText);
            }

            UtilityHelper.CountlyLogging("[ModuleFeedback] GetAvailableFeedbackWidgets, request failed", LogLevel.ERROR);
            return new CountlyFeedbackWidget[0];
        }

        public async Task<JObject> GetFeedbackWidgetData(CountlyFeedbackWidget widget)
        {
            if (widget == null) {
                UtilityHelper.CountlyLogging("[ModuleFeedback] GetFeedbackWidgetData, widget is null", LogLevel.ERROR);
                return null;
            }
            UtilityHelper.CountlyLogging("[ModuleFeedback] GetFeedbackWidgetData for widget [" + widget.widgetId + "]");

            IDictionary<string, object> parameters = new Dictionary<string, object> {
                { "widget_id", widget.widgetId },
                { "shown", "1" }
            };
            string endpoint = "/o/surveys/" + widget.type + "/widget";

            RequestResult requestResult = await Api.Instance.SendDirectRequest(ServerUrl, await requestHelper.BuildRequest(parameters), endpoint);

            if (requestResult != null && requestResult.responseCode == 200 && requestResult.responseText != null) {
                try {
                    return JObject.Parse(requestResult.responseText);
                } catch {
                    UtilityHelper.CountlyLogging("[ModuleFeedback] GetFeedbackWidgetData, failed to parse response", LogLevel.ERROR);
                }
            }
            return null;
        }

        public async Task ReportFeedbackWidgetManually(CountlyFeedbackWidget widget, JObject widgetData, Dictionary<string, object> widgetResult)
        {
            if (widget == null) {
                UtilityHelper.CountlyLogging("[ModuleFeedback] ReportFeedbackWidgetManually, widget is null, ignoring", LogLevel.ERROR);
                return;
            }

            string eventKey;
            switch (widget.type) {
                case FeedbackWidgetType.nps: eventKey = NPS_EVENT_KEY; break;
                case FeedbackWidgetType.survey: eventKey = SURVEY_EVENT_KEY; break;
                default: eventKey = STAR_RATING_EVENT_KEY; break;
            }

            // Stable Add() order: platform, app_version, widget_id, then result (or closed).
            Segmentation segmentation = new Segmentation();
            segmentation.Add("platform", "windows");
            segmentation.Add("app_version", new IRequestHelperImpl(Countly.Instance).GetAppVersion());
            segmentation.Add("widget_id", widget.widgetId);

            if (widgetResult == null) {
                segmentation.Add("closed", "1");
            } else {
                foreach (KeyValuePair<string, object> kv in widgetResult) {
                    segmentation.Add(kv.Key, Convert.ToString(kv.Value));
                }
            }

            // RecordEventInternal is protected; a module must use the public static entrypoint.
            await Countly.RecordEvent(eventKey, 1, segmentation);
        }

        public async Task<string> ConstructFeedbackWidgetUrl(CountlyFeedbackWidget widget)
        {
            if (widget == null) { return null; }
            IDictionary<string, object> baseParams = await requestHelper.GetBaseParams();
            return WidgetUrlBuilder.BuildFeedbackWidgetUrl(ServerUrl, widget, baseParams);
        }
    }

    internal class MockFeedback : Feedback
    {
        public Task<CountlyFeedbackWidget[]> GetAvailableFeedbackWidgets() { return Task.FromResult(new CountlyFeedbackWidget[0]); }
        public Task<JObject> GetFeedbackWidgetData(CountlyFeedbackWidget widget) { return Task.FromResult<JObject>(null); }
        public Task ReportFeedbackWidgetManually(CountlyFeedbackWidget widget, JObject widgetData, Dictionary<string, object> widgetResult) { return Task.FromResult(0); }
        public Task<string> ConstructFeedbackWidgetUrl(CountlyFeedbackWidget widget) { return Task.FromResult<string>(null); }
    }

    /// <summary>
    /// Defines the contract for retrieving, displaying, and reporting Countly feedback widgets
    /// (Surveys, NPS, Ratings).
    /// </summary>
    public interface Feedback
    {
        /// <summary>Fetches the list of feedback widgets available for the current device.</summary>
        Task<CountlyFeedbackWidget[]> GetAvailableFeedbackWidgets();

        /// <summary>Fetches the raw widget definition JSON (for building a custom UI).</summary>
        Task<JObject> GetFeedbackWidgetData(CountlyFeedbackWidget widget);

        /// <summary>
        /// Reports a widget result manually. Pass <paramref name="widgetResult"/> as null to
        /// mark the widget closed/cancelled without an answer.
        /// </summary>
        Task ReportFeedbackWidgetManually(CountlyFeedbackWidget widget, JObject widgetData, Dictionary<string, object> widgetResult);

        /// <summary>Builds the display URL for a widget (used by the WebView2 UI package).</summary>
        Task<string> ConstructFeedbackWidgetUrl(CountlyFeedbackWidget widget);
    }
}
