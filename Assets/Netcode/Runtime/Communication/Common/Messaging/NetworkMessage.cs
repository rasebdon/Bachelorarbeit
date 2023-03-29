using System;

namespace Netcode.Runtime.Communication.Common.Messaging
{
    public abstract class NetworkMessage
    {
        public DateTime TimeStamp { get; }

        protected NetworkMessage()
        {
            TimeStamp = DateTime.Now;
        }
    }
}
