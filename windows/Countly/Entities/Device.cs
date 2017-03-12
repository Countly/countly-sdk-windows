/*
Copyright (c) 2012, 2013, 2014 Countly

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using CountlySDK.Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Networking.Connectivity;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.UI.Xaml;

namespace CountlySDK.Entitites
{
    /// <summary>
    /// This class provides several static methods to retrieve information about the current device and operating environment.
    /// </summary>
    internal static class Device
    {
        private static string deviceId;
        /// <summary>
        /// Returns the unique device identificator
        /// </summary>
        public static string DeviceId
        {
            get
            {
                try
                {
                    if (deviceId != null) return deviceId;

                    HardwareToken token = HardwareIdentification.GetPackageSpecificToken(null);
                    IBuffer hardwareId = token.Id;

                    HashAlgorithmProvider hasher = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
                    IBuffer hashed = hasher.HashData(hardwareId);

                    deviceId = CryptographicBuffer.EncodeToHexString(hashed);

                    return deviceId;
                }
                catch
                {
                    return String.Empty;
                }
            }
            set
            {
                deviceId = value;
            }
        }

        /// <summary>
        /// Returns the display name of the current operating system
        /// </summary>
        public static string OS
        {
            get
            {
                EasClientDeviceInformation easClientDeviceInformation = new EasClientDeviceInformation();

                return easClientDeviceInformation.OperatingSystem;
            }
        }

        /// <summary>
        /// Returns the current operating system version as a displayable string
        /// </summary>
        public static string OSVersion
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the current device manufacturer
        /// </summary>
        public static string Manufacturer
        {
            get
            {
                EasClientDeviceInformation easClientDeviceInformation = new EasClientDeviceInformation();

                return easClientDeviceInformation.SystemManufacturer;
            }
        }

        /// <summary>
        /// Returns the current device model
        /// </summary>
        public static string DeviceName
        {
            get
            {
                EasClientDeviceInformation easClientDeviceInformation = new EasClientDeviceInformation();

                return PhoneNameHelper.Resolve(easClientDeviceInformation.SystemManufacturer, easClientDeviceInformation.SystemProductName).FullCanonicalName;
            }
        }

        /// <summary>
        /// Returns application version from Package.appxmanifest
        /// </summary>
        public static string AppVersion
        {
            get
            {
                PackageVersion packageVersion = Package.Current.Id.Version;

                Version version = new Version(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);

                return version.ToString();
            }
        }

        /// <summary>
        /// Returns device resolution in <width_px>x<height_px> format
        /// </summary>
        public static string Resolution
        {
            get
            {
                int width = (int)(Window.Current.Bounds.Width * (int)DisplayInformation.GetForCurrentView().ResolutionScale / 100);
                int height = (int)(Window.Current.Bounds.Height * (int)DisplayInformation.GetForCurrentView().ResolutionScale / 100);

                return width + "x" + height;
            }
        }

        /// <summary>
        /// Returns cellular mobile operator
        /// </summary>
        public static string Carrier
        {
            get
            {
                var result = NetworkInformation.GetConnectionProfiles();

                foreach (var connectionProfile in result)
                {
                    if (connectionProfile.IsWwanConnectionProfile)
                    {
                        foreach (var networkName in connectionProfile.GetNetworkNames())
                        {
                            return networkName;
                        }
                    }
                }

                return String.Empty;
            }
        }

        /// <summary>
        /// Returns current device orientation
        /// </summary>
        public static string Orientation
        {
            get
            {
                return (Window.Current.Bounds.Width > Window.Current.Bounds.Height) ? "landscape" : "portrait";
            }
        }

        /// <summary>
        /// Returns current device connection to the internet
        /// </summary>
        public static bool Online
        {
            get
            {
                ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();

                return connections != null && connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
            }
        }
    }
}
