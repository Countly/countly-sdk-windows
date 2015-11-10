using System.Runtime.Serialization;

namespace CountlySDK.Entities
{
    [DataContractAttribute]
    [KnownType(typeof(BeginSession))]
    [KnownType(typeof(UpdateSession))]
    [KnownType(typeof(EndSession))]
    internal abstract class SessionEvent
    {
        [DataMemberAttribute]
        public string Content { get; set; }
    }
}
