using CountlySDK;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Xunit;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace TestProject_common
{
    public class ConsentTests : IDisposable
    {
        /// <summary>
        /// Test setup
        /// </summary>
        public ConsentTests()
        {
            CountlyImpl.SetPCLStorageIfNeeded();
            Countly.Halt();
            TestHelper.CleanDataFiles();
            Countly.Instance.deferUpload = false;
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {

        }

        [Fact]
        public async void ConsentSimple()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            cc.consentRequired = true;
            Countly.Instance.Init(cc).Wait();
            Countly.Instance.SessionBegin().Wait();


            TestHelper.ValidateDataPointUpload().Wait();
            Countly.Instance.SessionEnd().Wait();
            TestHelper.ValidateDataPointUpload().Wait();
        }

        [Fact]
        public async void ConsentSimpleDenied()
        {
            Dictionary<ConsentFeatures, bool> consent = new Dictionary<ConsentFeatures, bool>();
            consent.Add(ConsentFeatures.Crashes, false);
            consent.Add(ConsentFeatures.Events, false);
            consent.Add(ConsentFeatures.Location, false);
            consent.Add(ConsentFeatures.Sessions, false);
            consent.Add(ConsentFeatures.Users, false);

            CountlyConfig cc = TestHelper.CreateConfig();
            cc.givenConsent = consent;
            cc.consentRequired = true;
            Countly.Instance.Init(cc).Wait();
            Countly.Instance.SessionBegin().Wait();


            TestHelper.ValidateDataPointUpload().Wait();
            Countly.Instance.SessionEnd().Wait();
            TestHelper.ValidateDataPointUpload().Wait();
        }

        [Fact]
        public async void ConsentSimpleAllowed()
        {
            Dictionary<ConsentFeatures, bool> consent = new Dictionary<ConsentFeatures, bool>();
            consent.Add(ConsentFeatures.Crashes, true);
            consent.Add(ConsentFeatures.Events, true);
            consent.Add(ConsentFeatures.Location, true);
            consent.Add(ConsentFeatures.Sessions, true);
            consent.Add(ConsentFeatures.Users, true);

            CountlyConfig cc = TestHelper.CreateConfig();
            cc.givenConsent = consent;
            cc.consentRequired = true;
            Countly.Instance.Init(cc).Wait();
            Countly.Instance.SessionBegin().Wait();


            TestHelper.ValidateDataPointUpload().Wait();
            Countly.Instance.SessionEnd().Wait();
            TestHelper.ValidateDataPointUpload().Wait();
        }

        [Fact]
        public async void ConsentSimpleAllowedThenRemoved()
        {
            Dictionary<ConsentFeatures, bool> consentGiven = TestHelper.AllConsentValues(true);
            Dictionary<ConsentFeatures, bool> consentRemoved = TestHelper.AllConsentValues(false);

            CountlyConfig cc = TestHelper.CreateConfig();
            cc.givenConsent = consentGiven;
            cc.consentRequired = true;
            Countly.Instance.deferUpload = true;
            Countly.Instance.Init(cc).Wait();
            int previousCount = Countly.Instance.Sessions.Count;
            Countly.Instance.SessionBegin().Wait();

            await Countly.Instance.SetConsent(consentRemoved);
            await Countly.Instance.SetConsent(consentGiven);
            await Countly.Instance.SetConsent(consentRemoved);

            TestHelper.ValidateDataPointUpload().Wait();
            Countly.Instance.SessionEnd().Wait();
            TestHelper.ValidateDataPointUpload().Wait();

            Assert.Equal(2 + previousCount, Countly.Instance.Sessions.Count);
            Assert.Equal(4, Countly.Instance.StoredRequests.Count);
        }

        /// <summary>
        /// It validates the full state of consent request.
        /// </summary>
        [Fact]
        public void TestConsentRequest()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            cc.consentRequired = true;

            Dictionary<ConsentFeatures, bool> consent = new Dictionary<ConsentFeatures, bool>();
            consent.Add(ConsentFeatures.Crashes, true);
            consent.Add(ConsentFeatures.Events, true);
            consent.Add(ConsentFeatures.Location, true);
            consent.Add(ConsentFeatures.Sessions, true);
            consent.Add(ConsentFeatures.Users, true);

            cc.givenConsent = consent;

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.SessionBegin().Wait();
            Countly.Instance.deferUpload = false;

            StoredRequest request = Countly.Instance.StoredRequests.Dequeue();
            NameValueCollection collection = HttpUtility.ParseQueryString(request.Request);
            JObject consentObj = JObject.Parse(collection.Get("consent"));

            Assert.Equal(10, consentObj.Count);
            Assert.False(consentObj.GetValue("push").ToObject<bool>());
            Assert.True(consentObj.GetValue("users").ToObject<bool>());
            Assert.False(consentObj.GetValue("views").ToObject<bool>());
            Assert.True(consentObj.GetValue("events").ToObject<bool>());
            Assert.True(consentObj.GetValue("crashes").ToObject<bool>());
            Assert.True(consentObj.GetValue("sessions").ToObject<bool>());
            Assert.True(consentObj.GetValue("location").ToObject<bool>());
            Assert.False(consentObj.GetValue("feedback").ToObject<bool>());
            Assert.False(consentObj.GetValue("star-rating").ToObject<bool>());
            Assert.False(consentObj.GetValue("remote-config").ToObject<bool>());
        }
    }
}
