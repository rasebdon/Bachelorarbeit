using System;

namespace Netcode.Runtime.Communication.Common.Messaging
{
    public class InstantiateNetworkObjectMessage : NetworkMessage
    {
        public int PrefabId { get; set; }
        public Guid Guid { get; set; }

        public InstantiateNetworkObjectMessage(int prefabId, Guid guid)
        {
            PrefabId = prefabId;
            Guid = guid;
        }
    }
}