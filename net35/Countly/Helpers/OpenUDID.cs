using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace CountlySDK.Helpers
{
    internal class OpenUDID
    {
        public enum OpenUDIDErrors
        {
            None = 0,
            OptedOut = 1,
            Compromised = 2
        }
        private static String _cachedValue;
        private static OpenUDIDErrors _lastError;
        private static String _getOpenUDID()
        {
            _lastError = OpenUDIDErrors.None;
            if (_cachedValue == null)
            {
                MD5 _md5 = MD5.Create();
                ManagementObjectSearcher _searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                foreach (ManagementObject mo in _searcher.Get())
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
        public static String valueWithError(out OpenUDIDErrors error)
        {
            String v = value;
            error = _lastError;
            return v;
        }
        public static String CorpIdentifier;
        public static String CorpValue
        {
            get
            {
                MD5 _md5 = MD5.Create();
                byte[] _buf = System.Text.Encoding.UTF8.GetBytes(String.Format("{0}.{1}", CorpIdentifier, value));

                _buf = _md5.ComputeHash(_buf);

                StringBuilder s = new StringBuilder();
                foreach (byte b in _buf)
                {
                    s.Append(b.ToString("x2").ToLower());
                }
                return s.ToString();
            }
        }
        public static String GetCorpUDID(String corpIdentifier)
        {
            CorpIdentifier = corpIdentifier;
            return CorpValue;
        }
    }
}
