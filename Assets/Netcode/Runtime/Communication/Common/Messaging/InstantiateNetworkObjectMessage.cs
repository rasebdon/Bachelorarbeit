using System;
using UnityEngine;

namespace Netcode.Runtime.Communication.Common.Messaging
{
    public class InstantiateNetworkObjectMessage : NetworkMessage
    {
        public int PrefabId { get; set; }
        public Guid NetworkIdentityGuid { get; set; }
        public uint? ClientId { get; set; }

        public Quaternion Rotation { get; set; }
        public Vector3 Position { get; set; }

        public InstantiateNetworkObjectMessage(
            int prefabId, Guid networkIdentityGuid, uint? clientId, Quaternion rotation, Vector3 position)
        {
            PrefabId = prefabId;
            NetworkIdentityGuid = networkIdentityGuid;
            ClientId = clientId;
            Rotation = rotation;
            Position = position;
        }
    }
}