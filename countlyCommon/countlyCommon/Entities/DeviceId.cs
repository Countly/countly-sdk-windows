using System;
using System.Runtime.Serialization;
using CountlySDK.Entities.EntityBase;
using Newtonsoft.Json;

namespace CountlySDK.CountlyCommon.Entities
{
    [DataContractAttribute]
    internal class DeviceId : IComparable<DeviceId>
    {
        [DataMemberAttribute]
        public string deviceId { get; set; }

        [DataMemberAttribute]
        public DeviceBase.DeviceIdMethodInternal deviceIdMethod { get; set; }

        internal DeviceId(string deviceId, DeviceBase.DeviceIdMethodInternal deviceIdMethod)
        {
            this.deviceId = deviceId;
            this.deviceIdMethod = deviceIdMethod;
        }

        /// <summary>
        /// Needed for JSON deserialization
        /// </summary>
        [JsonConstructor]
        internal DeviceId() { }

        internal int Type()
        {
            int type = 9;
            switch (deviceIdMethod) {
                case DeviceBase.DeviceIdMethodInternal.cpuId:
                    type = 3;
                    break;
                case DeviceBase.DeviceIdMethodInternal.multipleWindowsFields:
                    type = 3;
                    break;
                case DeviceBase.DeviceIdMethodInternal.windowsGUID:
                    type = 3;
                    break;
                case DeviceBase.DeviceIdMethodInternal.winHardwareToken:
                    type = 3;
                    break;
                case DeviceBase.DeviceIdMethodInternal.developerSupplied:
                    type = 0;
                    break;
                default:
                    break;
            }

            return type;
        }

        public int CompareTo(DeviceId other)
        {
            if (!(deviceId == null && other.deviceId == null)) {
                if (deviceId == null) { return -1; }
                if (other.deviceId == null) { return 1; }
                if (!deviceId.Equals(other.deviceId)) {
                    return deviceId.CompareTo(other.deviceId);
                }
            }

            return deviceIdMethod.CompareTo(other.deviceIdMethod);
        }
    }
}
