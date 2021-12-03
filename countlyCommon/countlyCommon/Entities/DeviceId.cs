using CountlySDK.Entities.EntityBase;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

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
