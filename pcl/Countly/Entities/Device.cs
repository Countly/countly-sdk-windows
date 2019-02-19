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

using CountlySDK.CountlyCommon.Entities;
using CountlySDK.Entities.EntityBase;
using CountlySDK.Helpers;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Networking.Connectivity;
using Windows.Security.ExchangeActiveSyncProvisioning;

namespace CountlySDK.Entities
{
    /// <summary>
    /// This class provides static methods to retrieve information about the current device.
    /// </summary>
    internal class Device : DeviceBase
    {   
        protected override DeviceId ComputeDeviceID()
        {
            DeviceId dId;

            //the only possible method is Guid and developer supplied
            //if it's dev supplied and we still need to generate it, then
            //there is no other way
            dId = CreateGUIDDeviceId();

            return dId;
        }        

        protected override string GetOS()
        {
            return "Windows (PCL)";
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

        protected override string GetResolution()
        {
            return null;
        }

        protected override string GetCarrier()
        {
            return null;
        }

        protected override string GetOrientation()
        {
            return null;
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
