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
using CountlySDK.Entities.EntityBase;

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
        public async void Dispose()
        {
            await Countly.Instance.HaltInternal();
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

            DeviceId dId = await Countly.Instance.DeviceData.GetDeviceId();
            string oldDeviceId = dId.deviceId;
            Countly.Instance.ChangeDeviceId("new-device-id", false).Wait();

            //End session request
            SessionEvent model = Countly.Instance.Sessions[0];
            NameValueCollection collection = HttpUtility.ParseQueryString(model.Content);

            Assert.Equal("1", collection.Get("end_session"));
            Assert.Equal(oldDeviceId, collection.Get("device_id"));

            string type = collection.Get("t");
            Assert.False(string.IsNullOrEmpty(type));
            Assert.Equal("3", type);

            dId = await Countly.Instance.DeviceData.GetDeviceId();
            string newDeviceId = dId.deviceId;
            model = Countly.Instance.Sessions[1];
            collection = HttpUtility.ParseQueryString(model.Content);

            Assert.Equal("1", collection.Get("begin_session"));
            Assert.Equal("new-device-id", collection.Get("device_id"));
            Assert.Equal("new-device-id", newDeviceId);

            type = collection.Get("t");
            Assert.False(string.IsNullOrEmpty(type));
            Assert.Equal("0", type);
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

            DeviceId dId = await Countly.Instance.DeviceData.GetDeviceId();
            string oldDeviceId = dId.deviceId;
            Countly.Instance.ChangeDeviceId("new-device-id", false).Wait();

            //End session request
            SessionEvent model = Countly.Instance.Sessions[0];
            NameValueCollection collection = HttpUtility.ParseQueryString(model.Content);

            Assert.Equal("1", collection.Get("end_session"));
            Assert.Equal(oldDeviceId, collection.Get("device_id"));

            string type = collection.Get("t");
            Assert.False(string.IsNullOrEmpty(type));
            Assert.Equal("3", type);

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

            type = collection.Get("t");
            Assert.False(string.IsNullOrEmpty(type));
            Assert.Equal("0", type);
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

            DeviceId dId = await Countly.Instance.DeviceData.GetDeviceId();
            string oldDeviceId = dId.deviceId;

            Countly.Instance.ChangeDeviceId("new-device-id", true).Wait();
            dId = await Countly.Instance.DeviceData.GetDeviceId();
            string newDeviceId = dId.deviceId;

            //End session request
            StoredRequest model = Countly.Instance.StoredRequests.Dequeue();
            NameValueCollection collection = HttpUtility.ParseQueryString(model.Request);

            Assert.Equal(oldDeviceId, collection.Get("old_device_id"));
            Assert.Equal("new-device-id", collection.Get("device_id"));
            Assert.Equal("new-device-id", newDeviceId);

            string type = collection.Get("t");
            Assert.False(string.IsNullOrEmpty(type));
            Assert.Equal("0", type);
        }

        [Fact]
        /// <summary>
        /// It validates the type values of device id generation methods.
        /// </summary>
        public void ValidateDeviceIdTypeValue()
        {
            DeviceId did = new DeviceId("device-id", DeviceBase.DeviceIdMethodInternal.none);
            Assert.Equal(9, did.Type());

            did = new DeviceId("device-id", DeviceBase.DeviceIdMethodInternal.cpuId);
            Assert.Equal(1, did.Type());

            did = new DeviceId("device-id", DeviceBase.DeviceIdMethodInternal.multipleWindowsFields);
            Assert.Equal(2, did.Type());

            did = new DeviceId("device-id", DeviceBase.DeviceIdMethodInternal.windowsGUID);
            Assert.Equal(3, did.Type());

            did = new DeviceId("device-id", DeviceBase.DeviceIdMethodInternal.winHardwareToken);
            Assert.Equal(4, did.Type());

            did = new DeviceId("device-id", DeviceBase.DeviceIdMethodInternal.developerSupplied);
            Assert.Equal(0, did.Type());
        }


        /**
         * +--------------------------------------------------+------------------------------------+----------------------+
         * | SDK state at the end of the previous app session | Provided configuration during init | Action taken by SDK  |
         * +--------------------------------------------------+------------------------------------+----------------------+
         * |           Custom      |   SDK used a             |              Custom                |    Flag   |   flag   |
         * |         device ID     |   generated              |            device ID               |    not    |          |
         * |         was set       |       ID                 |             provided               |    set    |   set    |
         * +--------------------------------------------------+------------------------------------+----------------------+
         * |                     First init                   |                   -                |    1      |    -     |
         * +--------------------------------------------------+------------------------------------+----------------------+
         * |                     First init                   |                   x                |    2      |    -     |
         * +--------------------------------------------------+------------------------------------+----------------------+
         * |            x          |             -            |                   -                |    3      |    -     |
         * +--------------------------------------------------+------------------------------------+----------------------+
         * |            x          |             -            |                   x                |    4      |    -     |
         * +--------------------------------------------------+------------------------------------+----------------------+
         * |            -          |             x            |                   -                |    5      |    -     |
         * +--------------------------------------------------+------------------------------------+----------------------+
         * |            -          |             x            |                   x                |    6      |    -     |
         * +--------------------------------------------------+------------------------------------+----------------------+
         */

        private readonly string _serverUrl = "https://xyz.com/";
        private readonly string _appKey = "772c091355076ead703f987fee94490";


        private Countly ConfigureAndInitSDK(string deviceId = null, bool isAutomaticSessionTrackingDisabled = false)
        {
            CountlyConfig configuration = new CountlyConfig {
                appKey = _appKey,
                serverUrl = _serverUrl,
                developerProvidedDeviceId = deviceId,
            };

            Countly.Instance.Init(configuration).Wait();
            return Countly.Instance;
        }

        private async void ValidateDeviceIDAndType(Countly instance, string deviceId, DeviceIdType type, DeviceBase.DeviceIdMethodInternal internalType, bool compareDeviceId = true)
        {
            DeviceId sdkDeviceId = await instance.DeviceData.GetDeviceId();
            Assert.NotNull(sdkDeviceId.deviceId);
            Assert.Equal(type, instance.GetDeviceIDType());
            Assert.Equal(internalType, sdkDeviceId.deviceIdMethod);

            if (compareDeviceId) {
                Assert.Equal(deviceId, sdkDeviceId.deviceId);
            } else {
                Assert.NotEmpty(sdkDeviceId.deviceId);
            }
        }

        /// <summary>
        /// Scenario 1: First time init the SDK without custom device ID and init the SDK second time with custom device ID.
        /// SDK Action: During second init, SDK will not override the device ID generated during first init. 
        /// </summary>
        [Fact]
        public void TestDeviceIdGeneratedBySDK()
        {
            ConfigureAndInitSDK();
            ValidateDeviceIDAndType(Countly.Instance, null, DeviceIdType.SDKGenerated, DeviceBase.DeviceIdMethodInternal.windowsGUID, false);
        }

        /// <summary>
        /// Scenario 2: First time init the SDK with custom device ID and init the SDK second time without device ID.
        /// SDK Action: During second init, SDK will not override the custom device ID provided in first init. 
        /// </summary>
        [Fact]
        public void TestDeviceIdGivenInConfig()
        {
            ConfigureAndInitSDK("device_id");
            ValidateDeviceIDAndType(Countly.Instance, "device_id", DeviceIdType.DeveloperProvided, DeviceBase.DeviceIdMethodInternal.developerSupplied);
        }

        /// <summary>
        /// Scenario 3: First time init the SDK with custom device ID and init the SDK second time without device ID.
        /// SDK Action: During second init, SDK will not override the custom device ID provided in first init. 
        /// </summary>
        [Fact]
        public async void CustomDeviceIDWasSet_CustomDeviceIDNotProvided()
        {
            ConfigureAndInitSDK("device_id");
            ValidateDeviceIDAndType(Countly.Instance, "device_id", DeviceIdType.DeveloperProvided, DeviceBase.DeviceIdMethodInternal.developerSupplied);

            // Destroy instance before init SDK again.
            await Countly.Instance.HaltInternal(false);

            ConfigureAndInitSDK();
            ValidateDeviceIDAndType(Countly.Instance, "device_id", DeviceIdType.DeveloperProvided, DeviceBase.DeviceIdMethodInternal.developerSupplied);

        }

        /// <summary>
        /// Scenario 4: First time init the SDK with custom device ID and init the SDK second time with a new custom device ID.
        /// SDK Action: During second init, SDK will not override the custom device ID provided in first init. 
        /// </summary>
        [Fact]
        public async void CustomDeviceIDWasSet_CustomDeviceIDProvided()
        {
            ConfigureAndInitSDK("device_id");
            ValidateDeviceIDAndType(Countly.Instance, "device_id", DeviceIdType.DeveloperProvided, DeviceBase.DeviceIdMethodInternal.developerSupplied);

            // Destroy instance before init SDK again.
            await Countly.Instance.HaltInternal(false);

            ConfigureAndInitSDK("device_id_new");
            ValidateDeviceIDAndType(Countly.Instance, "device_id", DeviceIdType.DeveloperProvided, DeviceBase.DeviceIdMethodInternal.developerSupplied);
        }


        /// <summary>
        /// Scenario 5: First time init the SDK without custom device ID and init the SDK second time without custom device ID.
        /// SDK Action: During second init, SDK will not override the device ID generated during first init.
        /// </summary>
        [Fact]
        public async void GeneratedDeviceID_CustomDeviceIDNotProvided()
        {
            ConfigureAndInitSDK();
            ValidateDeviceIDAndType(Countly.Instance, null, DeviceIdType.SDKGenerated, DeviceBase.DeviceIdMethodInternal.windowsGUID, false);

            DeviceId deviceID = await Countly.Instance.DeviceData.GetDeviceId();

            // Destroy instance before init SDK again.
            await Countly.Instance.HaltInternal(false);

            ConfigureAndInitSDK();
            ValidateDeviceIDAndType(Countly.Instance, deviceID.deviceId, DeviceIdType.SDKGenerated, DeviceBase.DeviceIdMethodInternal.windowsGUID);
        }

        /// <summary>
        /// Scenario 6: First time init the SDK without custom device ID and init the SDK second time with a custom device ID.
        /// SDK Action: During second init, SDK will not override the device ID generated during first init.
        /// </summary>
        [Fact]
        public async void GeneratedDeviceID_CustomDeviceIDProvided()
        {
            ConfigureAndInitSDK();
            ValidateDeviceIDAndType(Countly.Instance, null, DeviceIdType.SDKGenerated, DeviceBase.DeviceIdMethodInternal.windowsGUID, false);

            DeviceId deviceID = await Countly.Instance.DeviceData.GetDeviceId();

            // Destroy instance before init SDK again.
            await Countly.Instance.HaltInternal(false);

            ConfigureAndInitSDK("device_id_new");
            ValidateDeviceIDAndType(Countly.Instance, deviceID.deviceId, DeviceIdType.SDKGenerated, DeviceBase.DeviceIdMethodInternal.windowsGUID);
        }
    }
}
