using System;
using System.Diagnostics;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace CountlySDK.Helpers
{
    public class OpenUDID
    {
        private static String _cachedValue;
        private static String _getOpenUDID()
        {
            if (_cachedValue == null)
            {
                try
                {
                    MD5 _md5 = MD5.Create();
                    ManagementObjectSearcher _searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                    foreach (ManagementObject mo in _searcher.Get())
                    {
                        if (mo != null && mo["ProcessorId"] != null)
                        {
                            byte[] bs = System.Text.Encoding.UTF8.GetBytes(mo["ProcessorId"].ToString());
                            bs = _md5.ComputeHash(bs);
                            StringBuilder s = new StringBuilder();
                            foreach (byte b in bs)
                            {
                                s.Append(b.ToString("x2").ToLower());
                            }

                            _cachedValue = s.ToString();
                        }
                        else
                        {
                            _cachedValue = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    UtilityHelper.CountlyLogging("Threw exception while trying to generate CPU id in OpenUDID");
                    _cachedValue = null;
                }

            }
            return _cachedValue;
        }

        public static String value
        {
            get
            {
                return _getOpenUDID();
            }
        }
    }
}
