using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CountlySDK;
using Xunit;
using static CountlySDK.CountlyCommon.CountlyBase;

namespace TestProject_common
{
    /// <summary>
    /// Public entry points must not crash the host app when they are called before Init
    /// (a common race: an async Init running while telemetry calls arrive). In the
    /// never-initialized state Configuration is null, and several guards used to dereference
    /// Configuration.backendMode before the IsInitialized() check, throwing a
    /// NullReferenceException. These tests pin the "no throw, no-op" contract.
    /// </summary>
    public class PreInitSafetyTests : IDisposable
    {
        public PreInitSafetyTests()
        {
            TestHelper.CleanDataFiles();
            Countly.Halt();
            TestHelper.CleanDataFiles();
            // Simulate the genuine never-initialized state: Halt clears ServerUrl/AppKey but
            // intentionally leaves Configuration set, so we null it explicitly here.
            Countly.Instance.Configuration = null;
        }

        public void Dispose()
        {
        }

        [Fact]
        public async Task RecordEvent_BeforeInit_ReturnsFalseWithoutThrowing()
        {
            bool result = await Countly.RecordEvent("pre_init_event");
            Assert.False(result);
        }

        [Fact]
        public async Task RecordView_BeforeInit_ReturnsFalseWithoutThrowing()
        {
            bool result = await Countly.Instance.RecordView("pre_init_view");
            Assert.False(result);
        }

        [Fact]
        public async Task RecordException_BeforeInit_ReturnsFalseWithoutThrowing()
        {
            bool result = await Countly.RecordException("err", "stack", null, false);
            Assert.False(result);
        }

        [Fact]
        public async Task SetLocation_BeforeInit_ReturnsFalseWithoutThrowing()
        {
            bool result = await Countly.Instance.SetLocation("12.34,56.78");
            Assert.False(result);
        }

        [Fact]
        public async Task DisableLocation_BeforeInit_ReturnsFalseWithoutThrowing()
        {
            bool result = await Countly.Instance.DisableLocation();
            Assert.False(result);
        }

        [Fact]
        public void StartEvent_BeforeInit_DoesNotThrow()
        {
            Countly.Instance.StartEvent("pre_init_timed");
        }

        [Fact]
        public void AddCrashBreadCrumb_BeforeInit_DoesNotThrow()
        {
            Countly.Instance.AddCrashBreadCrumb("crumb");
        }

        [Fact]
        public async Task ChangeDeviceId_BeforeInit_DoesNotThrow()
        {
            await Countly.Instance.ChangeDeviceId("new_device_id");
        }

        [Fact]
        public async Task SetConsent_BeforeInit_DoesNotThrow()
        {
            await Countly.Instance.SetConsent(new Dictionary<ConsentFeatures, bool> { { ConsentFeatures.Events, true } });
        }
    }
}
