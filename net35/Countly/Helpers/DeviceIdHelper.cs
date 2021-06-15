using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace CountlySDK.Helpers
{
    internal class DeviceIdHelper
    {
        internal static string GenerateId()
        {
            string cpuId = GetCPUId();
            string diskSerialNumber = GetDiskSerialNumber();
            string windowsInfo = GetWindowsSerialNumber();
            string windowsUsername = GetWindowsUsername();
            string macAddress = GetMacAddress();

            string combinedId = cpuId + "+" + diskSerialNumber + "+" + windowsInfo + "+" + windowsUsername + "+" + macAddress;

            var sha = new SHA1Managed();
            byte[] bs = sha.ComputeHash(Encoding.ASCII.GetBytes(combinedId));

            StringBuilder s = new StringBuilder();
            foreach (byte b in bs)
            {
                s.Append(b.ToString("x2").ToLower());
            }            

            return s.ToString();
        }

        private static string GetCPUId()
        {

            try
            {
                StringBuilder s = new StringBuilder();
                ManagementObjectSearcher _searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                foreach (ManagementObject mo in _searcher.Get())
                {
                    String appendValue = "";
                    if (mo != null && mo["ProcessorId"] != null)
                    {
                        appendValue = mo["ProcessorId"].ToString();
                    }

                    s.Append(appendValue);
                }

                return s.ToString();
            }
            catch (Exception ex)
            {
                UtilityHelper.CountlyLogging("DeviceIdHelper, problem while getting cpu id." + ex.ToString());

                return null;
            }
        }

        private static string GetDiskSerialNumber()
        {
            try
            {
                string driveLetter = "";

                //Choose the letter of the first available drive
                foreach (var availableDrives in DriveInfo.GetDrives())
                {
                    if (availableDrives.IsReady)
                    {
                        driveLetter = availableDrives.RootDirectory.ToString();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(driveLetter))
                {
                    //could not determine valid drive letter
                    UtilityHelper.CountlyLogging("Could not determine valid drive letter while trying to generate device Id");
                    return "";
                }

                //remove ":\" after the drive letter, "C:\" -> "C"
                if (!string.IsNullOrEmpty(driveLetter) && driveLetter.EndsWith(":\\"))
                {
                    driveLetter = driveLetter.Substring(0, driveLetter.Length - 2);
                }
                ManagementObject disk = new ManagementObject(@"win32_logicaldisk.deviceid=""" + driveLetter + @":""");
                disk.Get();

                string volumeSerialNumber = "";

                if (disk["VolumeSerialNumber"] != null)
                {
                    volumeSerialNumber = disk["VolumeSerialNumber"].ToString();
                    disk.Dispose();
                }

                return volumeSerialNumber;
            } catch (Exception ex)
            {
                UtilityHelper.CountlyLogging("DeviceIdHelper, problem while getting disk serial number." + ex.ToString());
                return null;
            }
        }

        public static string GetWindowsSerialNumber()
        {        
            string serialNumber = null;
            try
            {
                ManagementObject mo = new ManagementObject("Win32_OperatingSystem=@");
                if (mo["SerialNumber"] != null)
                {
                    serialNumber = (string) mo["SerialNumber"];
                }
            }
            catch (Exception ex) {
                UtilityHelper.CountlyLogging("DeviceIdHelper, problem while getting serial number." + ex.ToString());
            }
            
            return serialNumber;
        }

        public static string GetWindowsUsername()
        {
            try 
            {
                return Environment.UserName;
            }
            catch (Exception ex) 
            {
                UtilityHelper.CountlyLogging("DeviceIdHelper, problem while getting windows username." + ex.ToString());

                return null;
            }
        }

        public static string GetMacAddress()
        {
            const int MIN_ADDR_LENGTH = 12;
            string chosenMacAddress = null;
            long fastestFoundSpeed = -1;
            try {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    string potentialMacAddress = nic.GetPhysicalAddress().ToString();
                    if (nic.Speed > fastestFoundSpeed && !string.IsNullOrEmpty(potentialMacAddress) && potentialMacAddress.Length >= MIN_ADDR_LENGTH)
                    {
                        chosenMacAddress = potentialMacAddress;
                        fastestFoundSpeed = nic.Speed;
                    }
                }
            }
            catch (Exception ex) {
                UtilityHelper.CountlyLogging("DeviceIdHelper, problem while getting mac adress." + ex.ToString());
            }

            return chosenMacAddress;
        }
    }
}
