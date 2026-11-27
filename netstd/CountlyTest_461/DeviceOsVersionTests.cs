using System;
using CountlySDK.Entities;
using Xunit;

namespace TestProject_common
{
    public class DeviceOsVersionTests
    {
        private static readonly Version windows8 = new Version(6, 2, 9200, 0);

        /// <summary>
        /// Windows reports Windows 8 (6.2) to apps that don't declare support for later versions, as it does to apps on
        /// .NET Core 3.1 and older started from their own exe. The version must then come from the runtime's OS
        /// description, which holds the real one, in the same four-part form the SDK sends on other runtimes.
        /// </summary>
        [Fact]
        public void ResolveVersion_WindowsReportsWindows8_UsesTheDescribedVersion()
        {
            Assert.Equal(new Version(10, 0, 19042, 0), Device.ResolveVersion(PlatformID.Win32NT, windows8, () => "Microsoft Windows 10.0.19042"));
            Assert.Equal(new Version(10, 0, 22631, 0), Device.ResolveVersion(PlatformID.Win32NT, windows8, () => "Microsoft Windows 10.0.22631 "));
        }

        /// <summary>
        /// A reported version other than 6.2 is the real one and is kept, whatever the description says.
        /// </summary>
        [Fact]
        public void ResolveVersion_OtherWindowsVersions_AreKept()
        {
            Assert.Equal(new Version(10, 0, 19042, 0), Device.ResolveVersion(PlatformID.Win32NT, new Version(10, 0, 19042, 0), () => "Microsoft Windows 6.2.9200"));
            Assert.Equal(new Version(6, 1, 7601, 65536), Device.ResolveVersion(PlatformID.Win32NT, new Version(6, 1, 7601, 65536), () => "Microsoft Windows 10.0.19042"));
        }

        /// <summary>
        /// On a real Windows 8 the description has the same version, and on non-Windows platforms the reported version
        /// is never replaced.
        /// </summary>
        [Fact]
        public void ResolveVersion_RealWindows8AndOtherPlatforms_AreKept()
        {
            Assert.Equal(windows8, Device.ResolveVersion(PlatformID.Win32NT, windows8, () => "Microsoft Windows 6.2.9200"));
            Assert.Equal(new Version(6, 2, 0, 0), Device.ResolveVersion(PlatformID.Unix, new Version(6, 2, 0, 0), () => "Linux 6.2.0-39-generic"));
        }

        /// <summary>
        /// A description without a newer version, an empty or missing one, or one that can't be read keeps the reported
        /// version, so the SDK never sends less than before.
        /// </summary>
        [Fact]
        public void ResolveVersion_DescriptionWithoutANewerVersion_KeepsTheReportedVersion()
        {
            Assert.Equal(windows8, Device.ResolveVersion(PlatformID.Win32NT, windows8, () => "Microsoft Windows NT 6.2.9200.0"));
            Assert.Equal(windows8, Device.ResolveVersion(PlatformID.Win32NT, windows8, () => ""));
            Assert.Equal(windows8, Device.ResolveVersion(PlatformID.Win32NT, windows8, () => null));
            Assert.Equal(windows8, Device.ResolveVersion(PlatformID.Win32NT, windows8, () => "Microsoft Windows 99999999999.1.1"));
            Assert.Equal(windows8, Device.ResolveVersion(PlatformID.Win32NT, windows8, () => { throw new PlatformNotSupportedException(); }));
        }
    }
}
