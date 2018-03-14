using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CountlySDK.Entities.EntityBase
{
    abstract internal class DeviceBase
    {
        protected const string deviceFilename = "device.xml";

        protected string deviceId;

        internal abstract Task<string> GetDeviceId();

        internal abstract Task SetDeviceId(string providedDeviceId);
    }
}
