using Netcode.Channeling;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netcode.Runtime.Behaviour
{
    [DisallowMultipleComponent]
    public class NetworkIdentity : MonoBehaviour
    {
        private static readonly Dictionary<Guid, NetworkIdentity> _identities = new();

        private Dictionary<uint, NetworkVariableBase> _networkVariables = new();

        public Guid Guid
        {
            get
            {
                if (_guid == Guid.Empty && !string.IsNullOrEmpty(_guidAsString))
                {
                    _guid = new Guid(_guidAsString);
                }
                return _guid;
            }
            set
            {
                _guid = value;
                _guidAsString = _guid.ToString();
            }
        }
        [SerializeField] private Guid _guid;
        [SerializeField] private string _guidAsString;

        /// <summary>
        /// Gets invoked when this network identity receives a message
        /// </summary>
        public Action<NetworkMessage> OnReceiveMessage;

        public bool IsSpawned { get; private set; }

        // Client side properties
        public bool IsLocalPlayer { get; set; }

        // Server side properties
        public int PrefabId { get; set; }
        public bool IsPlayer { get; set; }
        public uint OwnerClientId { get; set; }

        private void Start()
        {
            _identities.Add(Guid, this);

            // Get network variables on this gameobject through reflections
            var behaviors = GetComponents<NetworkBehaviour>().ToList();
            foreach (var behavior in behaviors)
            {
                var fields = behavior.GetType().GetFields();

                var filtered = fields.Where(p => p.FieldType.IsSubclassOf(typeof(NetworkVariableBase)))
                    .Select(p => new KeyValuePair<uint, NetworkVariableBase>(
                        (uint)(behavior.GetType().Name + "." + p.Name).GetHashCode(), p.GetValue(behavior) as NetworkVariableBase))
                    .ToList();

                filtered.ForEach(kvp =>
                {
                    kvp.Value.SetNetworkBehaviour(behavior);
                    kvp.Value.Hash = kvp.Key;
                    _networkVariables.Add(kvp.Key, kvp.Value);
                });
            }

            OnReceiveMessage += (msg) =>
            {
                if (IsPlayer)
                {
                    if (msg is InstantiateNetworkObjectMessage inomToPlayer)
                    {
                        // Send message from server to client
                        NetworkHandler.Instance.SendTcp(inomToPlayer, OwnerClientId);
                    }
                    else if (msg is DestroyNetworkObjectMessage dnomToPlayer)
                    {
                        // Send message from server to client
                        NetworkHandler.Instance.SendTcp(dnomToPlayer, OwnerClientId);
                    }
                }

                if (msg is InstantiateNetworkObjectMessage inomToPlayerFromObject)
                {
                    // Also sync this object to the player
                    if (inomToPlayerFromObject.OwnerClientId.HasValue &&
                        inomToPlayerFromObject.OwnerClientId != this.OwnerClientId)
                    {
                        var thisObjectSync = new InstantiateNetworkObjectMessage(
                        name, PrefabId, Guid, IsPlayer ? OwnerClientId : null, IsPlayer, transform.rotation, transform.position);

                        NetworkHandler.Instance.SendTcp(thisObjectSync, inomToPlayerFromObject.OwnerClientId.Value);
                    }
                }

                // Network Variable Handling
                if (msg is SyncNetworkVariableMessage syncNetworkVariableMessage)
                {
                    // Meant for this identity (on server -> set value)
                    if (syncNetworkVariableMessage.NetworkIdentity == Guid)
                    {
                        if (_networkVariables.TryGetValue(syncNetworkVariableMessage.VariableHash, out var netVar))
                        {
                            var value = NetworkHandler.Instance.Serializer.Deserialize(
                                syncNetworkVariableMessage.Value,
                                netVar.Value.GetType());
                            netVar.SetValueFromNetworkMessage(value);
                        }
                    }

                    // Send to client if this has a client
                    if (IsPlayer)
                    {
                        NetworkHandler.Instance.SendTcp(syncNetworkVariableMessage, OwnerClientId);
                    }
                }
            };
        }

        /// <summary>
        /// Network identity registers itself on the server after the physics update
        /// (in the physics update, the OnTriggerX functions are called before the update loop)
        /// </summary>
        private void Update()
        {
            if (!IsSpawned)
            {
                if (NetworkHandler.Instance.IsClient)
                {
                    IsSpawned = true;
                }
                else if (ChannelHandler.Instance.HasChannels(this))
                {
                    // Send spawn network object message
                    ChannelHandler.Instance.DistributeMessage(
                        this,
                        new InstantiateNetworkObjectMessage(
                            name, PrefabId, Guid, IsPlayer ? OwnerClientId : null, IsPlayer, transform.rotation, transform.position),
                        ChannelType.Environment);
                    IsSpawned = true;
                }
            }
        }

        private void OnDestroy()
        {
            if (!NetworkHandler.Instance.IsClient)
            {
                // Send destroy message to clients
                ChannelHandler.Instance.DistributeMessage(
                this,
                new DestroyNetworkObjectMessage(Guid),
                ChannelType.Environment);
            }

            ChannelHandler.Instance.ExitFromAllZones(this);
            _identities.Remove(Guid);
        }

        public static NetworkIdentity FindByGuid(Guid id)
        {
            if (_identities.TryGetValue(id, out NetworkIdentity identity))
            {
                return identity;
            }
            return null;
        }

        public void SetNetworkVariableFromServerOnClient(SyncNetworkVariableMessage msg)
        {
            if (NetworkHandler.Instance.IsHost)
            {
                // Value already set on server side
                return;
            }

            if (_networkVariables.TryGetValue(msg.VariableHash, out var netVar))
            {
                var value = NetworkHandler.Instance.Serializer.Deserialize(
                    msg.Value,
                    netVar.Value.GetType());
                netVar.SetValueFromNetworkMessage(value);
            }
        }
    }
}
