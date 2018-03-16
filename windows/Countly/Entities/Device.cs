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

using CountlySDK.Entities.EntityBase;
using CountlySDK.Helpers;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Graphics.Display;
using Windows.Networking.Connectivity;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.UI.Xaml;

namespace CountlySDK.Entities
{
    /// <summary>
    /// This class provides several static methods to retrieve information about the current device and operating environment.
    /// </summary>
    internal class Device : DeviceBase
    {
        protected override async Task LoadDeviceIDFromStorage()
        {
            
        }
        protected override async Task SaveDeviceIDToStorage()
        {
            
        }
        protected override String ComputeDeviceID()
        {
            HardwareToken token = HardwareIdentification.GetPackageSpecificToken(null);
            IBuffer hardwareId = token.Id;

            HashAlgorithmProvider hasher = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            IBuffer hashed = hasher.HashData(hardwareId);

            return CryptographicBuffer.EncodeToHexString(hashed);
        }

        protected override string GetOS()
        {
            EasClientDeviceInformation easClientDeviceInformation = new EasClientDeviceInformation();        
            return easClientDeviceInformation.OperatingSystem;
        }

        protected override string GetOSVersion()
        {
            return null;
        }

        protected override string GetManufacturer()
        {
            EasClientDeviceInformation easClientDeviceInformation = new EasClientDeviceInformation();       
            return easClientDeviceInformation.SystemManufacturer;
        }

        protected override string GetDeviceName()
        {
            EasClientDeviceInformation easClientDeviceInformation = new EasClientDeviceInformation();        
            return PhoneNameHelper.Resolve(easClientDeviceInformation.SystemManufacturer, easClientDeviceInformation.SystemProductName).FullCanonicalName;
        }

        protected override string GetAppVersion()
        {
            PackageVersion packageVersion = Package.Current.Id.Version;
            Version version = new Version(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
            return version.ToString();
        }

        private string resolution;
        protected override string GetResolution()
        {
            if (Window.Current != null && DisplayInformation.GetForCurrentView() != null)
            {
                int width = (int)(Window.Current.Bounds.Width * (int)DisplayInformation.GetForCurrentView().ResolutionScale / 100);
                int height = (int)(Window.Current.Bounds.Height * (int)DisplayInformation.GetForCurrentView().ResolutionScale / 100);

                resolution = width + "x" + height;

                return resolution;
            }
            else
            {
                return resolution ?? String.Empty;
            }
        }       

        protected override string GetCarrier()
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
    
        private string orientation;
        protected override string GetOrientation()
        {
            if (Window.Current != null)
            {
                orientation = (Window.Current.Bounds.Width > Window.Current.Bounds.Height) ? "landscape" : "portrait";

                return orientation;
            }
            else
            {
                return orientation ?? String.Empty;
            }
        }

        protected override long? GetRamCurrent()
        {
            return null;
        }
        protected override long? GetRamTotal()
        {
            return null;
        }

        protected override bool GetOnline()
        {
            ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();

            return connections != null && connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
        }
    }
}
