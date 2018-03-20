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
using System.Windows.Forms;

namespace CountlySDK.Entities
{
    /// <summary>
    /// This class provides several static methods to retrieve information about the current device and operating environment.
    /// </summary>
    internal class Device : DeviceBase
    {
        protected override async Task LoadDeviceIDFromStorage()
        {
            DeviceId dId = Storage.LoadFromFile<DeviceId>(deviceFilename);

            if(dId != null && dId.deviceId != null)
            {
                deviceId = dId.deviceId;
                usedIdMethod = dId.deviceIdMethod;
            }
        }
        protected override async Task SaveDeviceIDToStorage()
        {
            //only try saving if the id is not null
            if(deviceId != null)
            {
                DeviceId dId = new DeviceId(deviceId, usedIdMethod);

                Storage.SaveToFile(deviceFilename, dId);
            }            
        }
        protected override DeviceId ComputeDeviceID()
        {
            DeviceId dId;

            if(preferredIdMethod == DeviceIdMethodInternal.cpuId)
            {
                dId = new DeviceId(OpenUDID.value, DeviceIdMethodInternal.cpuId);
            } else if(preferredIdMethod == DeviceIdMethodInternal.multipleWindowsFields)
            {
                dId = new DeviceId(DeviceIdHelper.GenerateId(), DeviceIdMethodInternal.multipleWindowsFields);
            } else
            {
                dId = new DeviceId(OpenUDID.value, DeviceIdMethodInternal.cpuId);
            }

            return dId;
        }

        protected override string GetOS()
        {
            return OSInfo.OsName;
        }

        protected override string GetOSVersion()
        {
            return OSInfo.OSVersion;
        }       

        protected override string GetManufacturer()
        {
            return null;
        }

        protected override string GetDeviceName()
        {
            return System.Environment.MachineName;
        }

        protected override string GetAppVersion()
        {
            return null;
        }

        protected override string GetResolution()
        {
            return String.Format("{0}x{1}", SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);
        }

        protected override string GetCarrier()
        {
            return null;
        }        

        protected override long? GetRamCurrent()
        {
            return (long)(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory - new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory);
        }
        protected override long? GetRamTotal()
        {
            return (long)new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
        }

        protected override string GetOrientation()
        {
            return null;
        }

        protected override bool GetOnline()
        {
            return System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
        }
    }
}
