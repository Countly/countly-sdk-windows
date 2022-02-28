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

            ConsentFeatures[] consents = System.Enum.GetValues(typeof(ConsentFeatures)).Cast<ConsentFeatures>().ToArray();
            foreach (ConsentFeatures c in consents) {
                Assert.False(Countly.Instance.IsConsentGiven(c));
            }
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
