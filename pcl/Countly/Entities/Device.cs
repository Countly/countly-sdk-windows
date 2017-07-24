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
using System.Threading.Tasks;

namespace CountlySDK.Entities
{
    /// <summary>
    /// This class provides static methods to retrieve information about the current device.
    /// </summary>
    public static class Device
    {
        private static string deviceFilename = "device.xml";

        public static string deviceId_;
        /// <summary>
        /// Returns the unique device identificator
        /// </summary>
        public static async Task<string> GetDeviceId()
        {            
            try
            {
                if (deviceId_ != null) return deviceId_;

                deviceId_ = await Storage.LoadFromFile<string>(deviceFilename);

                if (deviceId_ == null)
                {
                    Guid guid = Guid.NewGuid();

                    deviceId_ = guid.ToString().Replace("-", "").ToUpper();

                    await Storage.SaveToFile<string>(deviceFilename, deviceId_);
                }

                return deviceId_;
            }
            catch
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// Sets the unique device identificator
        /// </summary>
        public static async Task SetDeviceId(string deviceId)
        {
            try
            {
                deviceId_ = deviceId;

                await Storage.SaveToFile<string>(deviceFilename, deviceId_);
            }
            catch
            {

            }
        }
    }
}
