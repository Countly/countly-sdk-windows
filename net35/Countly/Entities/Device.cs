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
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CountlySDK.Entities
{
    /// <summary>
    /// This class provides several static methods to retrieve information about the current device and operating environment.
    /// </summary>
    internal class Device : DeviceBase
    {        
        protected override DeviceId ComputeDeviceID()
        {
            DeviceId dId;

            if (preferredIdMethod == DeviceIdMethodInternal.cpuId)
            {
                String cpuIDValue = OpenUDID.value;
                if (cpuIDValue != null)
                {
                    dId = new DeviceId(cpuIDValue, DeviceIdMethodInternal.cpuId);
                }
                else
                {
                    //fallback
                    dId = new DeviceId(DeviceIdHelper.GenerateId(), DeviceIdMethodInternal.multipleWindowsFields);
                }
            }
            else if (preferredIdMethod == DeviceIdMethodInternal.multipleWindowsFields)
            {
                dId = new DeviceId(DeviceIdHelper.GenerateId(), DeviceIdMethodInternal.multipleWindowsFields);
            }
            else if (preferredIdMethod == DeviceIdMethodInternal.windowsGUID)
            {
                dId = CreateGUIDDeviceId();
            }
            else
            {
                dId = CreateGUIDDeviceId();
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
            return null;
        }

        protected override string GetAppVersion()
        {
            return null;
        }

        protected override string GetResolution()
        {
            try
            {
                return String.Format("{0}x{1}", SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);
            }
            catch (Exception ex)
            {
                if (Countly.IsLoggingEnabled)
                {
                    Debug.WriteLine("Device:GetResolution, problem while getting system virtual screen information." + ex.ToString());
                }

                return null;
            }
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
            try
            {
                return (long)(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory - new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory);
            }
            catch (Exception ex)
            {
                if (Countly.IsLoggingEnabled)
                {
                    Debug.WriteLine("Device:GetRamCurrent, problem while getting physical memory information." + ex.ToString());
                }

                return 0;
            }
        }
        protected override long? GetRamTotal()
        {
            try
            {
                return (long)new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
            }
            catch (Exception ex)
            {
                if (Countly.IsLoggingEnabled)
                {
                    Debug.WriteLine("Device:GetRamTotal, problem while getting physical memory information." + ex.ToString());
                }

                return 0;
            }
        }       

        protected override bool GetOnline()
        {
            try
            {
                return System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            }
            catch (Exception ex)
            {
                if (Countly.IsLoggingEnabled)
                {
                    Debug.WriteLine("Device:GetIsNetworkAvailable, problem while getting network information." + ex.ToString());
                }

                return false;
            }
        }
    }
}
