using Netcode.Channeling;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Integration;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Netcode.Behaviour
{
    [DisallowMultipleComponent]
    public class NetworkIdentity : MonoBehaviour
    {
        private static readonly Dictionary<Guid, NetworkIdentity> _identities = new();

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

        public bool Instantiated { get; private set; }

        // Client side properties
        public bool IsLocalPlayer { get; set; }

        // Server side properties
        public int PrefabId { get; set; }
        public bool IsPlayer { get; set; }
        public uint ClientId { get; set; }

        private void Start()
        {
            _identities.Add(Guid, this);

            OnReceiveMessage += (msg) =>
            {
                if (IsPlayer)
                {
                    if (msg is InstantiateNetworkObjectMessage inomToPlayer)
                    {
                        // Send message from server to client
                        NetworkHandler.Instance.Send(inomToPlayer, ClientId);
                    }
                    else if (msg is DestroyNetworkObjectMessage dnomToPlayer)
                    {
                        // Send message from server to client
                        NetworkHandler.Instance.Send(dnomToPlayer, ClientId);
                    }
                }

                if (msg is InstantiateNetworkObjectMessage inomToPlayerFromObject)
                {
                    // Also sync this object to the player
                    if (inomToPlayerFromObject.ClientId.HasValue && 
                        inomToPlayerFromObject.ClientId != this.ClientId)
                    {
                        var thisObjectSync = new InstantiateNetworkObjectMessage(
                        name, PrefabId, Guid, IsPlayer ? ClientId : null, transform.rotation, transform.position);

                        NetworkHandler.Instance.Send(thisObjectSync, inomToPlayerFromObject.ClientId.Value);
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
            if (!Instantiated)
            {
                if (NetworkHandler.Instance.IsClient)
                {
                    Instantiated = true;
                }
                else if(ChannelHandler.Instance.HasChannels(this))
                {
                    // Send spawn network object message
                    ChannelHandler.Instance.DistributeMessage(
                        this,
                        new InstantiateNetworkObjectMessage(
                            name, PrefabId, Guid, IsPlayer ? ClientId : null, transform.rotation, transform.position),
                        ChannelType.Environment);
                    Instantiated = true;
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

            ChannelHandler.Instance.RemoveIdentity(this);
            _identities.Remove(Guid);
        }

        public static NetworkIdentity FindByGuid(Guid id)
        {
            if(_identities.TryGetValue(id, out NetworkIdentity identity))
            {
                return identity;
            }
            return null;
        }
    }
}
