using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CountlySDK.CountlyCommon.Entities
{
    [DataContractAttribute]
    internal class StoredRequest : IComparable<StoredRequest>
    {
        [DataMemberAttribute]
        [JsonProperty("request")]
        public String Request;

        public StoredRequest(String request)
        {
            Request = request;
        }

        public int CompareTo(StoredRequest other)
        {
            if (Request == null && other.Request == null)
            {
                return 0;
            }
            return Request.CompareTo(other.Request);
        }
    }
}
