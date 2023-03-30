using Netcode.Channeling;
using Netcode.Runtime.Behaviour;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Integration;
using System.Collections.Generic;
using UnityEngine;

namespace Netcode.Runtime.Channeling
{
    /// <summary>
    /// The ChannelCollection is a component that has the two channel types
    /// (interaction and environment) as member variables and un-/subscribes
    /// network objects to those channels.
    /// </summary>
    [DisallowMultipleComponent]
    public class Zone : MonoBehaviour
    {
        [SerializeField] private List<Zone> _neighbors = new();
        public List<Zone> Neighbors => _neighbors;

        private Channel InteractionChannel { get; set; }
        private Channel EnvironmentChannel { get; set; }

        private void Awake()
        {
            // Create channels
            EnvironmentChannel = new(ChannelType.Environment);
            InteractionChannel = new(ChannelType.Interaction);
        }

        public void Publish<T>(T message, ChannelType channel, bool publishToNeighbors = true) where T : NetworkMessage
        {
            switch (channel)
            {
                case ChannelType.Interaction:
                    // Publish to own channel
                    InteractionChannel.Publish(message);
                    break;
                case ChannelType.Environment:
                    // Publish to own channel
                    EnvironmentChannel.Publish(message);
                    // Publish to neighbors
                    if (publishToNeighbors)
                    {
                        Neighbors.ForEach(ch => ch.Publish(message, ChannelType.Environment, false));
                    }
                    break;
                default:
                    Debug.Log("Invalid chanel type set! Abort sending message!");
                    break;
            }
        }

        /// <summary>
        /// Subscribes a <see cref="NetworkIdentity"/> to this channel
        /// </summary>
        /// <param name="netId"></param>
        public void Subscribe(NetworkIdentity netId, ChannelType type)
        {
            Debug.Log($"Subscribing {netId} to {name} {type}");

            switch (type)
            {
                case ChannelType.Environment:
                    EnvironmentChannel.Subscribe(netId);
                    break;
                case ChannelType.Interaction:
                    InteractionChannel.Subscribe(netId);
                    break;
                default:
                    Debug.Log("Invalid chanel type set! Abort subscribing!");
                    break;
            }
        }

        /// <summary>
        /// Unsubscribes a <see cref="NetworkIdentity"/> from this channel
        /// </summary>
        /// <param name="netId"></param>
        public void Unsubscribe(NetworkIdentity netId, ChannelType type)
        {
            Debug.Log($"Unsubscribing {netId} to {name} {type}");

            switch (type)
            {
                case ChannelType.Environment:
                    EnvironmentChannel.Unsubscribe(netId);
                    break;
                case ChannelType.Interaction:
                    InteractionChannel.Unsubscribe(netId);
                    break;
                default:
                    Debug.Log("Invalid chanel type set! Abort unsubscribing!");
                    break;
            }
        }

        private void Update()
        {
            if (NetworkHandler.Instance != null &&
                NetworkHandler.Instance.IsClient)
            {
                Destroy(this);
            }
        }
    }
}
