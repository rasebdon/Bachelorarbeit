using Netcode.Behaviour;
using Netcode.Runtime.Channeling;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Integration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Netcode.Channeling
{
    [RequireComponent(typeof(NetworkHandler))]
    public class ChannelHandler : MonoBehaviour
    {
        public static ChannelHandler Instance { get; private set; }

        private readonly Dictionary<NetworkIdentity, HashSet<Channel>> _channelRegistry = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Cannot have multiple instances of NetworkHandler", this);
                return;
            }
            Instance = this;
        }

        public void AddChannels(NetworkIdentity identity, ChannelCollection targetChannelCollection)
        {
            // Check if network identity is has any subscribed channels
            if(_channelRegistry.TryGetValue(identity, out HashSet<Channel> channels))
            {
                AddIfNotInChannel(identity, targetChannelCollection.InteractionChannel, ref channels);
                AddIfNotInChannel(identity, targetChannelCollection.EnvironmentChannel, ref channels);
            }
            // Create network identity with new hashset and channel as content
            else
            {
                channels = new HashSet<Channel>()
                {
                    targetChannelCollection.InteractionChannel,
                    targetChannelCollection.EnvironmentChannel
                };
                _channelRegistry.Add(identity, channels);
            }

            // Add the neighboring environment channels of the target interaction channel
            foreach (Channel neighbor in targetChannelCollection.InteractionChannel.EnvironmentNeighbors)
            {
                AddIfNotInChannel(identity, neighbor, ref channels);
            }
        }

        /// <summary>
        /// Adds the channel to the collection fo the identity and subscribes the identity to the channel
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="channel"></param>
        /// <param name="channels"></param>
        private void AddIfNotInChannel(NetworkIdentity identity, Channel channel, ref HashSet<Channel> channels)
        {
            if (!channels.Contains(channel))
            {
                channels.Add(channel);
                channel.Subscribe(identity);
            }
        }

        public void RemoveChannels(NetworkIdentity identity, ChannelCollection targetChannelCollection)
        {
            // Check if network identity is in registry
            if (_channelRegistry.TryGetValue(identity, out HashSet<Channel> channels))
            {
                // Remove the interaction channel 
                RemoveIfInChannel(identity, targetChannelCollection.InteractionChannel, ref channels);

                // Get neighbors of current interaction channels
                IEnumerable<Channel> expectedEnvironmentChannels = channels
                    .Where(c => c.Type == ChannelType.Interaction)
                    .SelectMany(c => c.EnvironmentNeighbors)
                    .Distinct()
                    .Concat(
                        channels.Where(c => c.Type == ChannelType.Interaction)
                        .Select(c => c.EnvironmentChannel));

                // Remove all environment channels that are currently in the channels of the network object
                // but not in the expected channels
                channels.RemoveWhere(c => c.Type == ChannelType.Environment && !expectedEnvironmentChannels.Contains(c));
            }
        }

        private void RemoveIfInChannel(NetworkIdentity identity, Channel channel, ref HashSet<Channel> channels)
        {
            // Check if the channel is already subscribed
            if (channels.Contains(channel))
            {
                channels.Remove(channel);
                channel.Unsubscribe(identity);
            }
        }

        public void DistributeMessage<T>(NetworkIdentity source, T message, ChannelType type) where T : NetworkMessage
        {
            if(_channelRegistry.TryGetValue(source, out HashSet<Channel> channels))
            {
                IEnumerable<Channel> distribution = channels.Where(c => c.Type == type);

                foreach (Channel channel in distribution)
                {
                    channel.Publish(message);
                }
            }
        }
        
        public void DistributeMessageToPlayers<T>(NetworkIdentity source, T message, ChannelType type) where T : NetworkMessage
        {
            if(_channelRegistry.TryGetValue(source, out HashSet<Channel> channels))
            {
                IEnumerable<Channel> distribution = channels.Where(c => c.Type == type);

                foreach (Channel channel in distribution)
                {
                    channel.PublishToPlayers(message);
                }
            }
        }
    }
}
