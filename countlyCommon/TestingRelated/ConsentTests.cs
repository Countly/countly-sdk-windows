using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using CountlySDK;
using CountlySDK.CountlyCommon.Entities;
using CountlySDK.Entities;
using Newtonsoft.Json.Linq;
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
            Countly.Instance.deferUpload = true;
        }

        /// <summary>
        /// Test cleanup
        /// </summary>
        public void Dispose()
        {
            Countly.Halt();
            TestHelper.CleanDataFiles();
        }

        /// <summary>
        /// Assert an array of consent against the expected value.
        /// </summary>
        /// <param name="expectedValue"> an expected values of consents</param>
        /// <param name="consents"> an array consents</param>
        private void AssertConsentArray(ConsentFeatures[] consents, bool expectedValue)
        {
            foreach (ConsentFeatures consent in consents) {
                Assert.Equal(expectedValue, Countly.Instance.IsConsentGiven(consent));
            }
        }

        /// <summary>
        /// Assert all consents against the expected value.
        /// </summary>
        /// <param name="expectedValue">an expected values of consents</param>
        private void AssertConsentAll(bool expectedValue)
        {
            ConsentFeatures[] consents = System.Enum.GetValues(typeof(ConsentFeatures)).Cast<ConsentFeatures>().ToArray();
            AssertConsentArray(consents, expectedValue);
        }

        [Fact]
        public void ConsentSimple()
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
        public void ConsentSimpleDenied()
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
        public void ConsentSimpleAllowed()
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
            Assert.Empty(Countly.Instance.StoredRequests);
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

            Countly.Instance.deferUpload = true;
            Countly.Instance.Init(cc).Wait();

            Assert.Single(Countly.Instance.StoredRequests);
            StoredRequest request = Countly.Instance.StoredRequests.Dequeue();
            NameValueCollection collection = HttpUtility.ParseQueryString(request.Request);
            Assert.False(string.IsNullOrEmpty(collection.Get("t")));
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

            Assert.Single(Countly.Instance.StoredRequests);
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

            Assert.Equal(2, Countly.Instance.StoredRequests.Count);
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
