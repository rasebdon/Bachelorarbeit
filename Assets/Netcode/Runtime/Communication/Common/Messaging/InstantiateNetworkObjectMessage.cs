using System;
using UnityEngine;

namespace Netcode.Runtime.Communication.Common.Messaging
{
    public class InstantiateNetworkObjectMessage : NetworkMessage
    {
        public string ObjectName { get; set; }
        public int PrefabId { get; set; }
        public Guid NetworkIdentityGuid { get; set; }
        public uint? OwnerClientId { get; set; }
        public bool IsPlayer { get; set; }

        public Quaternion Rotation { get; set; }
        public Vector3 Position { get; set; }

        public InstantiateNetworkObjectMessage(string objectName, int prefabId,
            Guid networkIdentityGuid, uint? ownerClientId, bool isPlayer, Quaternion rotation, Vector3 position)
        {
            ObjectName = objectName;
            PrefabId = prefabId;
            NetworkIdentityGuid = networkIdentityGuid;
            OwnerClientId = ownerClientId;
            IsPlayer = isPlayer;
            Rotation = rotation;
            Position = position;
        }
    }
}