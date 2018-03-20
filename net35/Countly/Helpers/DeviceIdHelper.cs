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

            Console.WriteLine("Combined ID: " + combinedId);

            var sha = new SHA256Managed();
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
            StringBuilder s = new StringBuilder();
            ManagementObjectSearcher _searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
            foreach (ManagementObject mo in _searcher.Get())
            {
                s.Append(mo["ProcessorId"].ToString());
            }

            return s.ToString();
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
                    if (Countly.IsLoggingEnabled)
                    {
                        Debug.WriteLine("Could not determine valid drive letter while trying to generate device Id");
                    }
                    return "";
                }

                //remove ":\" after the drive letter, "C:\" -> "C"
                if (!string.IsNullOrEmpty(driveLetter) && driveLetter.EndsWith(":\\"))
                {
                    driveLetter = driveLetter.Substring(0, driveLetter.Length - 2);
                }
                ManagementObject disk = new ManagementObject(@"win32_logicaldisk.deviceid=""" + driveLetter + @":""");
                disk.Get();

                string volumeSerialNumber = disk["VolumeSerialNumber"].ToString();
                disk.Dispose();

                return volumeSerialNumber;
            } catch (Exception ex)
            {
                if (Countly.IsLoggingEnabled)
                {
                    Debug.WriteLine("DeviceIdHelper, problem while getting disk serial number." + ex.ToString());
                }
                return "";
            }
        }

        public static string GetWindowsSerialNumber()
        {        
            var serialNumber = "";
            try
            {
                ManagementObject mo = new ManagementObject("Win32_OperatingSystem=@");
                serialNumber = (string)mo["SerialNumber"];
            }
            catch (Exception ex) {
                if (Countly.IsLoggingEnabled)
                {
                    Debug.WriteLine("DeviceIdHelper, problem while getting serial number." + ex.ToString());
                }
            }
            
            return serialNumber;
        }

        public static string GetWindowsUsername()
        {
            return Environment.UserName;
        }

        public static string GetMacAddress()
        {
            const int MIN_ADDR_LENGTH = 12;
            string chosenMacAddress = string.Empty;
            long fastestFoundSpeed = -1;

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                string potentialMacAddress = nic.GetPhysicalAddress().ToString();
                if (nic.Speed > fastestFoundSpeed && !string.IsNullOrEmpty(potentialMacAddress) && potentialMacAddress.Length >= MIN_ADDR_LENGTH)
                {
                    chosenMacAddress = potentialMacAddress;
                    fastestFoundSpeed = nic.Speed;
                }
            }

            return chosenMacAddress;
        }
    }
}
