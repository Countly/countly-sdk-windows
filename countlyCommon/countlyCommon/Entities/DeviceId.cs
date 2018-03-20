using CountlySDK.Entities.EntityBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CountlySDK.CountlyCommon.Entities
{
    [DataContractAttribute]
    internal class DeviceId
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
    }
}
