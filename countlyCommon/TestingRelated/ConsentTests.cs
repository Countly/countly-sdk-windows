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

        /// <summary>
        /// Assert an array of consent against the expected value.
        /// </summary>
        /// <param name="expectedValue"> an expected values of consents</param>
        /// <param name="consents"> an array consents</param>
        public void AssertConsentArray(ConsentFeatures[] consents, bool expectedValue)
        {
            foreach (ConsentFeatures consent in consents) {
                Assert.Equal(expectedValue, Countly.Instance.IsConsentGiven(consent));
            }
        }

        /// <summary>
        /// Assert all consents against the expected value.
        /// </summary>
        /// <param name="expectedValue">an expected values of consents</param>
        public void AssertConsentAll(bool expectedValue)
        {
            ConsentFeatures[] consents = System.Enum.GetValues(typeof(ConsentFeatures)).Cast<ConsentFeatures>().ToArray();
            AssertConsentArray(consents, expectedValue);
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
        /// Case: if 'RequiresConsent' is set in the configuration and no consent is given during initialization.
        /// Result: All features shouldn't work.
        /// </summary>
        [Fact]
        public void TestConsentDefaultValuesWithRequiresConsentTrue()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            cc.consentRequired = true;

            Countly.Instance.Init(cc).Wait();
            AssertConsentAll(expectedValue: false);
        }

        /// <summary>
        /// Case: if 'RequiresConsent' isn't set in the configuration during initialization.
        /// Result: All features should work.
        /// </summary>
        [Fact]
        public void TestDefaultStateOfConsents()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            cc.consentRequired = false;

            Dictionary<ConsentFeatures, bool> consent = new Dictionary<ConsentFeatures, bool>();
            consent.Add(ConsentFeatures.Crashes, true);
            consent.Add(ConsentFeatures.Events, true);

            cc.givenConsent = consent;

            Countly.Instance.Init(cc).Wait();
            AssertConsentAll(expectedValue: true);
        }

        /// <summary>
        /// Case: if 'RequiresConsent' isn't set in the configuration during initialization.
        /// Result: Consent request should not send.
        /// </summary>
        [Fact]
        public void TestConsentsRequest_RequiresConsent_IsFalse()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            cc.consentRequired = false;

            Dictionary<ConsentFeatures, bool> consent = new Dictionary<ConsentFeatures, bool>();
            consent.Add(ConsentFeatures.Crashes, true);
            consent.Add(ConsentFeatures.Events, true);

            cc.givenConsent = consent;

            Countly.Instance.Init(cc).Wait();
            Assert.Equal(0, Countly.Instance.StoredRequests.Count);
        }

        /// <summary>
        /// It validates the initial consent request that generates after SDK initialization.
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
            Countly.Instance.deferUpload = true;


            Assert.Equal(1, Countly.Instance.StoredRequests.Count);
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

        /// <summary>
        /// Case: If 'RequiresConsent' is set in the configuration and individual consents are given and removed to multiple features after initialization.
        /// </summary>
        [Fact]
        public void TestGiveIndividualConsents()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            cc.consentRequired = true;

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.deferUpload = true;

            Dictionary<ConsentFeatures, bool> consent = new Dictionary<ConsentFeatures, bool>();
            consent.Add(ConsentFeatures.Crashes, true);
            consent.Add(ConsentFeatures.Events, true);
            consent.Add(ConsentFeatures.Location, true);
            consent.Add(ConsentFeatures.Sessions, true);
            consent.Add(ConsentFeatures.Users, true);
            Countly.Instance.SetConsent(consent).Wait();

            Assert.Equal(1, Countly.Instance.StoredRequests.Count);
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

            Dictionary<ConsentFeatures, bool> consentToRemove = new Dictionary<ConsentFeatures, bool>();
            consentToRemove.Add(ConsentFeatures.Crashes, false);
            consentToRemove.Add(ConsentFeatures.Sessions, false);
            Countly.Instance.SetConsent(consentToRemove).Wait();

            Assert.Equal(1, Countly.Instance.StoredRequests.Count);
            request = Countly.Instance.StoredRequests.Dequeue();
            collection = HttpUtility.ParseQueryString(request.Request);
            consentObj = JObject.Parse(collection.Get("consent"));

            Assert.Equal(10, consentObj.Count);
            Assert.False(consentObj.GetValue("push").ToObject<bool>());
            Assert.True(consentObj.GetValue("users").ToObject<bool>());
            Assert.False(consentObj.GetValue("views").ToObject<bool>());
            Assert.True(consentObj.GetValue("events").ToObject<bool>());
            Assert.False(consentObj.GetValue("crashes").ToObject<bool>());
            Assert.False(consentObj.GetValue("sessions").ToObject<bool>());
            Assert.True(consentObj.GetValue("location").ToObject<bool>());
            Assert.False(consentObj.GetValue("feedback").ToObject<bool>());
            Assert.False(consentObj.GetValue("star-rating").ToObject<bool>());
            Assert.False(consentObj.GetValue("remote-config").ToObject<bool>());
        }
    }
}
