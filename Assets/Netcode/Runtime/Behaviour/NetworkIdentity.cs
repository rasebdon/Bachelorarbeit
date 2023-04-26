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
        public static Dictionary<Guid, NetworkIdentity> Identities { get; } = new();

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
        public MessageHandlerRegistry OnReceiveMessage { get; } = new();

        public bool IsSpawned { get; private set; }

        // Client side properties
        public bool IsLocalPlayer { get; set; }

        // Server side properties
        public int PrefabId { get; set; }
        public bool IsPlayer { get; set; }
        public uint OwnerClientId { get; set; }

        private void Start()
        {
            Identities.Add(Guid, this);

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

            if(NetworkHandler.Instance.IsServer || NetworkHandler.Instance.IsHost)
            {
                OnReceiveMessage.RegisterHandler(
                    new ActionMessageHandler<InstantiateNetworkObjectMessage>(
                        InstantiateNetworkObjectMessageCallback,
                        Guid.Parse("8E5A2FFA-4B27-4E3E-9DE5-F2B0C6D7A976")));
                OnReceiveMessage.RegisterHandler(
                    new ActionMessageHandler<DestroyNetworkObjectMessage>(
                        DestroyNetworkObjectMessageCallback,
                        Guid.Parse("20689427-5ECC-460A-A5CE-FADD7BDE14B3")));
                OnReceiveMessage.RegisterHandler(
                    new ActionMessageHandler<SyncNetworkVariableMessage>(
                        SyncNetworkVariableMessageCallback,
                        Guid.Parse("DA8F452A-8A6B-48CD-BAF3-F302442DCEA4")));
            }
        }

        private void SyncNetworkVariableMessageCallback(SyncNetworkVariableMessage msg, uint? clientId)
        {
            // Meant for this identity (on server -> set value)
            if (msg.NetworkIdentity == Guid)
            {
                if (_networkVariables.TryGetValue(msg.VariableHash, out var netVar))
                {
                    var value = NetworkHandler.Instance.Serializer.Deserialize(
                        msg.Value,
                        netVar.Value.GetType());
                    netVar.SetValueFromNetworkMessage(value, msg.Reliable, msg.TimeStamp);
                }
            }

            // Send to client if this has a client
            if (IsPlayer)
            {
                if(msg.Reliable.HasValue && msg.Reliable.Value || msg.Reliable == null)
                    NetworkHandler.Instance.SendTcpToClient (msg, OwnerClientId);
                else
                    NetworkHandler.Instance.SendUdpToClient (msg, OwnerClientId);
            }
        }

        private void DestroyNetworkObjectMessageCallback(DestroyNetworkObjectMessage msg, uint? clientId)
        {
            if (IsPlayer)
                NetworkHandler.Instance.SendTcpToClient(msg, OwnerClientId);
        }

        private void InstantiateNetworkObjectMessageCallback(InstantiateNetworkObjectMessage msg, uint? clientId)
        {
            if (IsPlayer)
            {
                // Send message from server to client
                NetworkHandler.Instance.SendTcpToClient(msg, OwnerClientId);
            }

            // Also sync this object to the player
            if (msg.OwnerClientId.HasValue &&
                msg.OwnerClientId != this.OwnerClientId)
            {
                var thisObjectSync = new InstantiateNetworkObjectMessage(
                name, PrefabId, Guid, IsPlayer ? OwnerClientId : null, IsPlayer, transform.rotation, transform.position);

                NetworkHandler.Instance.SendTcpToClient(thisObjectSync, msg.OwnerClientId.Value);
            }
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
                    IsLocalPlayer = IsPlayer && OwnerClientId == NetworkHandler.Instance.ClientId;
                    if(IsLocalPlayer) 
                        NetworkHandler.Instance.LocalPlayer = this;
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
            Identities.Remove(Guid);
        }

        public static NetworkIdentity FindByGuid(Guid id)
        {
            if (Identities.TryGetValue(id, out NetworkIdentity identity))
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
                netVar.SetValueFromNetworkMessage(value, msg.Reliable, msg.TimeStamp);
            }
        }

        public void ForwardTcp<T>(T msg, uint? clientId) where T : NetworkMessage
        {
            if (IsPlayer)
                NetworkHandler.Instance.SendTcpToClient(msg, OwnerClientId);
        }

        public void ForwardUdp<T>(T msg, uint? clientId) where T : NetworkMessage
        {
            if (IsPlayer)
                NetworkHandler.Instance.SendUdpToClient(msg, OwnerClientId);
        }
    }
}
