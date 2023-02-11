using System;

namespace Netcode.Runtime.Communication.Common.Messaging
{
    public class DestroyNetworkObjectMessage : NetworkMessage
    {
        public DestroyNetworkObjectMessage(Guid identity)
        {
            Identity = identity;
        }

        public Guid Identity { get; set; }
    }
}
