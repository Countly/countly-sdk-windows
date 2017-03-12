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
using Microsoft.Win32;
using System;
using System.Windows.Forms;

namespace CountlySDK.Entitites
{
    /// <summary>
    /// This class provides several static methods to retrieve information about the current device and operating environment.
    /// </summary>
    public static class Device
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
                    if (string.IsNullOrEmpty(deviceId))
                    {
                        return OpenUDID.value;
                    }
                    else
                    {
                        return deviceId;
                    }
                    
                }
                catch
                {
                    return "";
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
                return OSInfo.OsName;
            }
        }

        /// <summary>
        /// Returns the current operating system version as a displayable string
        /// </summary>
        public static string OSVersion
        {
            get
            {
                return OSInfo.OSVersion;
            }
        }

        private static string deviceName;
        /// <summary>
        /// Returns the local machine name
        /// </summary>
        public static string DeviceName
        {
            get
            {
                if (string.IsNullOrEmpty(deviceName))
                {
                    return System.Environment.MachineName;
                }
                else
                {
                    return deviceName;
                }
            }
            set
            {
                deviceName = value;
            }
        }

        /// <summary>
        /// Returns device resolution in <width_px>x<height_px> format
        /// </summary>
        public static string Resolution
        {
            get
            {
                return String.Format("{0}x{1}", SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);
            }
        }

        /// <summary>
        /// Returns available RAM space
        /// </summary>
        public static long RamCurrent
        {
            get
            {
                return (long)(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory - new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory);
            }
        }

        /// <summary>
        /// Returns total RAM size
        /// </summary>
        public static long RamTotal
        {
            get
            {
                return (long)new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
            }
        }

        /// <summary>
        /// Returns current device connection to the internet
        /// </summary>
        public static bool Online
        {
            get
            {
                return System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            }
        }
    }
}
