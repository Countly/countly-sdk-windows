using System;
/*
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;*/

namespace OpenUDIDPhone
{
    public static class OpenUDID
    {
        public enum OpenUDIDErrors
        {
            None = 0,
            OptedOut = 1,
            Compromised = 2
        }
        private static String _cachedValue;
        private static String _cachedDeviceUniqueId;
        private static OpenUDIDErrors _lastError;
        private static String _getOldDeviceUniqueId()
        {
            if (_cachedDeviceUniqueId == null)
            {
                byte[] myDeviceId;// = (byte[])
                object tmp;
                bool success = Microsoft.Phone.Info.DeviceExtendedProperties.TryGetValue("DeviceUniqueId", out tmp);
                myDeviceId = (byte[])tmp;
                _cachedDeviceUniqueId = Convert.ToBase64String(myDeviceId);

            }
            return _cachedDeviceUniqueId;
        }
        private static String _getOpenUDID()
        {
            _lastError = OpenUDIDErrors.None;
            if (_cachedValue == null)
            {
                _cachedValue = MD5Core.GetHashString(_getOldDeviceUniqueId());

            }
            return _cachedValue;
        }

        public static String OldDeviceId
        {
            get
            {
                return _getOldDeviceUniqueId();
            }
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
                return  MD5Core.GetHashString(String.Format("{0}.{1}", CorpIdentifier, value));
            }
        }
        public static String GetCorpUDID(String corpIdentifier)
        {
            CorpIdentifier = corpIdentifier;
            return CorpValue;
        }
    }
}
