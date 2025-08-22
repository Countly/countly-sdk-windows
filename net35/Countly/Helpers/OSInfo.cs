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

using System;
using System.Management;
using Microsoft.Win32;

namespace CountlySDK.Helpers
{
    internal class OSInfo
    {
        /// <summary>
        /// Returns the display name of the current operating system
        /// </summary>
        public static String OsName
        {
            get {
                try {
                    using (var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion")) {
                        string productName = reg == null ? null : reg.GetValue("ProductName") as string;
                        string buildStr = reg == null ? null :
                            (reg.GetValue("CurrentBuildNumber") as string) ?? (reg.GetValue("CurrentBuild") as string);

                        int build;
                        if (Int32.TryParse(buildStr, out build) && build >= 22000 &&
                            !string.IsNullOrEmpty(productName) && productName.Contains("Windows 10")) {
                            productName = productName.Replace("Windows 10", "Windows 11");
                        }
                        return productName;
                    }
                } catch (Exception ex) {
                    UtilityHelper.CountlyLogging("OSInfo:GetOSProductName failed: " + ex);
                    return null;
                }
            }
        }
        /// <summary>
        /// Returns the current operating system version as a displayable string
        /// </summary>
        public static string OSVersion
        {
            get {
                return GetOSName();
            }
        }

        private static string GetOSName()
        {
            string version_Os = null;

            try {
                // Get the OS information.
                string os_query = "SELECT * FROM Win32_OperatingSystem";

                ManagementObjectSearcher os_searcher = new ManagementObjectSearcher(os_query);
                foreach (ManagementObject info in os_searcher.Get()) {
                    version_Os = info.Properties["Version"].Value.ToString();
                }
            } catch (Exception ex) {
                UtilityHelper.CountlyLogging("OSInfo:OSVersion, problem while getting Managment information." + ex.ToString());
            }

            return version_Os;
        }
    }
}
