using MessagePack;
using System;

namespace Netcode.Runtime.Communication.Common.Messaging
{
    public abstract class NetworkMessage
    {
        [IgnoreMember] public bool? Reliable { get; set; } = null;
        public DateTime TimeStamp { get; }

        protected NetworkMessage()
        {
            TimeStamp = DateTime.Now;
        }
    }
}
