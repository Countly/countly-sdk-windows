using CountlySDK;
using CountlySDK.CountlyCommon.Helpers;
using CountlySDK.Entities;
using CountlySDK.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Xunit;
using static CountlySDK.CountlyCommon.CountlyBase;
using System.Collections.Specialized;
using CountlySDK.CountlyCommon.Entities;
using Newtonsoft.Json.Linq;

namespace TestProject_common
{
    public class DeviceIdTests : IDisposable
    {
        /// <summary>
        /// Test setup
        /// </summary>
        public DeviceIdTests()
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
        /// <summary>
        /// It validates the functionality of method 'ChangeDeviceId' without server merge.
        /// Case: When 'consentRequired' is'not set in the configuration.
        /// </summary>
        public async void TestChangeDeviceIdWithoutMerge()
        {
            CountlyConfig cc = TestHelper.CreateConfig();

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.deferUpload = true;
            Countly.Instance.SessionBegin().Wait();
            Countly.Instance.Sessions.Clear();

            string oldDeviceId = await Countly.Instance.DeviceData.GetDeviceId();
            Countly.Instance.ChangeDeviceId("new-device-id", false).Wait();

            //End session request
            SessionEvent model = Countly.Instance.Sessions[0];
            NameValueCollection collection = HttpUtility.ParseQueryString(model.Content);

            Assert.Equal("1", collection.Get("end_session"));
            Assert.Equal(oldDeviceId, collection.Get("device_id"));

            string newDeviceId = await Countly.Instance.DeviceData.GetDeviceId();
            model = Countly.Instance.Sessions[1];
            collection = HttpUtility.ParseQueryString(model.Content);

            Assert.Equal("1", collection.Get("begin_session"));
            Assert.Equal("new-device-id", collection.Get("device_id"));
            Assert.Equal("new-device-id", newDeviceId);
        }

        [Fact]
        /// <summary>
        /// It validates the consent removal after changing the device id without merging.
        /// Case: When 'consentRequired' is set in the configuration.
        /// </summary>
        public async void TestConsent_ChangeDeviceIdWithoutMerge()
        {
            Dictionary<ConsentFeatures, bool> consent = new Dictionary<ConsentFeatures, bool>();
            consent.Add(ConsentFeatures.Events, true);
            consent.Add(ConsentFeatures.Location, true);
            consent.Add(ConsentFeatures.Sessions, true);
            consent.Add(ConsentFeatures.Users, true);

            CountlyConfig cc = TestHelper.CreateConfig();
            cc.givenConsent = consent;

            cc.consentRequired = true;
            Countly.Instance.deferUpload = true;

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.SessionBegin().Wait();
            Countly.Instance.Sessions.Clear();
            Countly.Instance.StoredRequests.Clear();

            string oldDeviceId = await Countly.Instance.DeviceData.GetDeviceId();
            Countly.Instance.ChangeDeviceId("new-device-id", false).Wait();

            //End session request
            SessionEvent model = Countly.Instance.Sessions[0];
            NameValueCollection collection = HttpUtility.ParseQueryString(model.Content);

            Assert.Equal("1", collection.Get("end_session"));
            Assert.Equal(oldDeviceId, collection.Get("device_id"));

            // There is no consent request
            Assert.Empty(Countly.Instance.StoredRequests);

            ConsentFeatures[] consents = System.Enum.GetValues(typeof(ConsentFeatures)).Cast<ConsentFeatures>().ToArray();
            foreach (ConsentFeatures c in consents) {
                Assert.False(Countly.Instance.IsConsentGiven(c));
            }

            consent = new Dictionary<ConsentFeatures, bool>();
            consent.Add(ConsentFeatures.Crashes, true);
            consent.Add(ConsentFeatures.Events, true);
            consent.Add(ConsentFeatures.Location, true);
            consent.Add(ConsentFeatures.Sessions, true);
            consent.Add(ConsentFeatures.Users, true);
            Countly.Instance.SetConsent(consent).Wait();

            Assert.Single(Countly.Instance.StoredRequests);
            StoredRequest request = Countly.Instance.StoredRequests.Dequeue();
            collection = HttpUtility.ParseQueryString(request.Request);
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

        [Fact]
        /// <summary>
        /// It validates the functionality of method 'ChangeDeviceId' with server merge.
        /// </summary>
        public async void TestChangeDeviceIdWithMerge()
        {
            CountlyConfig cc = TestHelper.CreateConfig();
            cc.consentRequired = true;

            Countly.Instance.Init(cc).Wait();
            Countly.Instance.SessionBegin().Wait();
            Countly.Instance.Sessions.Clear();
            Countly.Instance.deferUpload = true;

            string oldDeviceId = await Countly.Instance.DeviceData.GetDeviceId();
            Countly.Instance.ChangeDeviceId("new-device-id", true).Wait();
            string newDeviceId = await Countly.Instance.DeviceData.GetDeviceId();

            //End session request
            StoredRequest model = Countly.Instance.StoredRequests.Dequeue();
            NameValueCollection collection = HttpUtility.ParseQueryString(model.Request);

            Assert.Equal(oldDeviceId, collection.Get("old_device_id"));
            Assert.Equal("new-device-id", collection.Get("device_id"));
            Assert.Equal("new-device-id", newDeviceId);
        }
    }
}
