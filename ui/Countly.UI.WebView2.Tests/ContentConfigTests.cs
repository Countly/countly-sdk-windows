using CountlySDK.Entities;
using Xunit;

namespace Countly.UI.WebView2.Tests
{
    public class ContentConfigTests
    {
        [Fact]
        public void ZoneTimerInterval_DefaultsTo30_AndRejectsTooSmall()
        {
            CountlyConfig cc = new CountlyConfig();
            Assert.Equal(30, cc.ContentZoneTimerInterval);
            cc.ContentZoneTimerInterval = 10;   // <=15 ignored
            Assert.Equal(30, cc.ContentZoneTimerInterval);
            cc.ContentZoneTimerInterval = 45;
            Assert.Equal(45, cc.ContentZoneTimerInterval);
        }
    }
}
