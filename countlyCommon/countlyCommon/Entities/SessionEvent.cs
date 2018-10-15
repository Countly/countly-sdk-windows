using System;
using System.Runtime.Serialization;

namespace CountlySDK.Entities
{
    [DataContractAttribute]
    [KnownType(typeof(BeginSession))]
    [KnownType(typeof(UpdateSession))]
    [KnownType(typeof(EndSession))]
    internal abstract class SessionEvent : IComparable<SessionEvent>
    {
        [DataMemberAttribute]
        public string Content { get; set; }

        public int CompareTo(SessionEvent other)
        {
            if(Content == null && other.Content == null)
            {
                return 0;
            }
            return Content.CompareTo(other.Content);
        }
    }
}