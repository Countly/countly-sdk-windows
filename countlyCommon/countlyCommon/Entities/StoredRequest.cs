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

        [DataMemberAttribute]
        [JsonProperty("idMerge")]
        public bool IdMerge;

        public StoredRequest(String request, bool isIdMerge = false)
        {
            Request = request;
            IdMerge = isIdMerge;
        }

        public int CompareTo(StoredRequest other)
        {
            if (Request == null && other.Request == null)
            {
                return IdMerge.CompareTo(other.IdMerge);
            }

            if (Request == null) { return -1; }
            if (other.Request == null) { return 1; }

            if (Request.Equals(other.Request))
            {
                return IdMerge.CompareTo(other.IdMerge);
            }
            return Request.CompareTo(other.Request);
        }
    }
}
