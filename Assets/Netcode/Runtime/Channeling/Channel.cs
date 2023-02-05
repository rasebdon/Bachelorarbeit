using Netcode.Behaviour;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Netcode.Channeling
{
    /// <summary>
    /// Channels forward their messages to their subscribed objects
    /// </summary>
    public class Channel
    {
        [SerializeField] private List<NetworkIdentity> _subscribed;

        public ChannelType Type { get; }
        public HashSet<Channel> EnvironmentNeighbors { get; set; }
        public Channel EnvironmentChannel { get; }

        public Channel(ChannelType type, Channel environmentChannel)
        {
            _subscribed = new();
            Type = type;
            EnvironmentNeighbors = new();
            EnvironmentChannel = environmentChannel;
        }

        public Channel(ChannelType type)
        {
            _subscribed = new();
            Type = type;
            EnvironmentNeighbors = new();
            EnvironmentChannel = this;
        }

        public void Publish<T>(T message) where T : NetworkMessage
        {
            foreach (NetworkIdentity netId in _subscribed)
            {
                netId.OnReceiveMessage?.Invoke(message);
            }
        }

        public void PublishToPlayers<T>(T message) where T : NetworkMessage
        {
            IEnumerable<NetworkIdentity> distribution = _subscribed.Where(id => id.IsPlayer);

            foreach (NetworkIdentity netId in distribution)
            {
                netId.OnReceiveMessage?.Invoke(message);
            }
        }

        /// <summary>
        /// Subscribes a <see cref="NetworkIdentity"/> to this channel
        /// </summary>
        /// <param name="netId"></param>
        public void Subscribe(NetworkIdentity netId)
        {
            _subscribed.Add(netId);
        }

        /// <summary>
        /// Unsubscribes a <see cref="NetworkIdentity"/> from this channel
        /// </summary>
        /// <param name="netId"></param>
        public void Unsubscribe(NetworkIdentity netId)
        {
            if (_subscribed.Contains(netId))
            {
                _subscribed.Remove(netId);
            }
        }
    }
}
