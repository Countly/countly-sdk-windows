using CountlySDK;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
