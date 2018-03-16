/*
Copyright (c) 2018 Countly

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

using System;
using System.Threading.Tasks;

namespace CountlySDK.Entities.EntityBase
{
    abstract internal class DeviceBase
    {
        protected const string deviceFilename = "device.xml";

        protected string deviceId;

        // Used for thread-safe operations
        protected object sync = new object();        

        /// <summary>
        /// Returns the unique device identificator
        /// </summary>
        internal async Task<string> GetDeviceId()
        {
            try
            {
                if (deviceId != null) return deviceId;

                await LoadDeviceIDFromStorage();                

                if (deviceId == null)
                {
                    deviceId = ComputeDeviceID();
                    await SaveDeviceIDToStorage();
                }

                return deviceId;
            }
            catch
            {
                //todo log
                return String.Empty;
            }
        }

        /// <summary>
        /// Sets the unique device identificator
        /// </summary>
        internal async Task SetDeviceId(string providedDeviceId)
        {
            try
            {
                deviceId = providedDeviceId;

                await SaveDeviceIDToStorage();                
            }
            catch
            {
                //todo log
            }
        }

        protected abstract Task LoadDeviceIDFromStorage();
        protected abstract Task SaveDeviceIDToStorage();   
        protected abstract String ComputeDeviceID();

        /// <summary>
        /// Returns the display name of the current operating system
        /// </summary>
        public string OS
        {
            get
            {
                return GetOS();
            }
        }

        protected abstract string GetOS();

        /// <summary>
        /// Returns the current operating system version as a displayable string
        /// </summary>
        public string OSVersion
        {
            get
            {
                return GetOSVersion();
            }
        }

        protected abstract string GetOSVersion();

        /// <summary>
        /// Returns the current device manufacturer
        /// </summary>
        public string Manufacturer
        {
            get
            {
                return GetManufacturer();
            }
        }

        protected abstract string GetManufacturer();

        private string deviceName;

        /// <summary>
        /// Returns the local machine name
        /// </summary>
        public string DeviceName
        {
            get
            {
                lock (sync)
                {
                    if (string.IsNullOrEmpty(deviceName))
                    {
                        return GetDeviceName();
                    }
                    else
                    {
                        return deviceName;
                    }
                }
            }
            set
            {
                lock (sync)
                {
                    deviceName = value;
                }
            }
        }

        protected abstract string GetDeviceName();

        /// <summary>
        /// Returns application version from Package.appxmanifest
        /// </summary>
        public string AppVersion
        {
            get
            {                
                return GetAppVersion();
            }
        }

        protected abstract string GetAppVersion();

        /// <summary>
        /// Returns device resolution in <width_px>x<height_px> format
        /// </summary>
        public string Resolution
        {
            get
            {
                return GetResolution();
            }
        }

        protected abstract string GetResolution();


        /// <summary>
        /// Returns cellular mobile operator
        /// </summary>
        public string Carrier
        {
            get
            {
                return GetCarrier();
            }
        }

        protected abstract string GetCarrier();

        /// <summary>
        /// Returns current device orientation
        /// </summary>
        public string Orientation
        {
            get
            {
                return GetOrientation();
            }
        }

        protected abstract string GetOrientation();

        /// <summary>
        /// Returns available RAM space
        /// </summary>
        public long? RamCurrent
        {
            get
            {
                return GetRamCurrent();
            }
        }

        protected abstract long? GetRamCurrent();

        /// <summary>
        /// Returns total RAM size
        /// </summary>
        public long? RamTotal
        {
            get
            {
                return GetRamTotal();
            }
        }

        protected abstract long? GetRamTotal();

        /// <summary>
        /// Returns current device connection to the internet
        /// </summary>
        public bool Online
        {
            get
            {
                return GetOnline();
            }
        }

        protected abstract bool GetOnline();
    }
}
