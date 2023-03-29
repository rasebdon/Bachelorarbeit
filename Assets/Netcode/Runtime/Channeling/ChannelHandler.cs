using Netcode.Runtime.Behaviour;
using Netcode.Runtime.Channeling;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Integration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netcode.Channeling
{
    [RequireComponent(typeof(NetworkHandler))]
    public class ChannelHandler : MonoBehaviour
    {
        public static ChannelHandler Instance { get; private set; }

        private readonly Dictionary<NetworkIdentity, ChannelRegistryEntry> _channelRegistry = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("Cannot have multiple instances of NetworkHandler", this);
                return;
            }
            Instance = this;
        }

        public void AddChannels(NetworkIdentity identity, ChannelComposition channelComposition)
        {
            // Check if network identity is registered in channel registry
            if (!_channelRegistry.TryGetValue(identity, out var entry))
            {
                entry = CreateAndInsertEntry(identity);
            }

            if (!entry.HasChannel(channelComposition))
            {
                entry.AddChannel(channelComposition);
                channelComposition.Subscribe(identity, ChannelType.Interaction);

                if (entry.ChannelCount == 1)
                {
                    channelComposition.Subscribe(identity, ChannelType.Environment);
                }
            }
        }

        public void RemoveChannels(NetworkIdentity identity, ChannelComposition channelComposition)
        {
            // Check if network identity is in registry
            if (_channelRegistry.TryGetValue(identity, out var entry) && entry.HasChannel(channelComposition))
            {
                entry.RemoveChannel(channelComposition);

                channelComposition.Unsubscribe(identity, ChannelType.Interaction);
                channelComposition.Unsubscribe(identity, ChannelType.Environment);
            }
        }

        public void RemoveIdentity(NetworkIdentity identity)
        {
            // Check if network identity is in registry
            if (_channelRegistry.TryGetValue(identity, out var entry))
            {
                entry.UnsubscribeAll();
                _channelRegistry.Remove(identity);
            }
        }

        public void DistributeMessage<T>(NetworkIdentity source, T message, ChannelType type) where T : NetworkMessage
        {
            // Create registry entry with global channel
            if (!_channelRegistry.TryGetValue(source, out var entry))
            {
                CreateAndInsertEntry(source);
            }

            // Distribute to channels
            if (entry.HasChannels)
            {
                // Distribute to main channels
                entry.CurrentChannel.Publish(message, type, true);

                // If there are more interactions (transitioning)
                if (type == ChannelType.Interaction)
                {
                    entry.GetWithoutCurrent().ToList().ForEach(ch => ch.Publish(message, type, false));
                }
            }
        }

        public ChannelComposition GetNextCollection(NetworkIdentity identity)
        {
            if (_channelRegistry.TryGetValue(identity, out var entry))
            {
                return entry.CurrentChannel;
            }
            return null;
        }

        public bool HasChannels(NetworkIdentity identity)
        {
            return _channelRegistry.ContainsKey(identity) && _channelRegistry[identity].HasChannels;
        }

        // Registry Helper

        ChannelRegistryEntry CreateAndInsertEntry(NetworkIdentity identity)
        {
            var entry = new ChannelRegistryEntry(identity);
            _channelRegistry.Add(identity, entry);
            return entry;
        }
    }

    public class ChannelRegistryEntry
    {
        public NetworkIdentity Identity { get; }
        public bool HasChannels { get => _channels.Any(); }
        public ChannelComposition CurrentChannel { get => _channels.FirstOrDefault(); }
        public int ChannelCount { get => _channels.Count; }

        private readonly List<ChannelComposition> _channels = new();

        public IEnumerable<ChannelComposition> GetWithoutCurrent()
        {
            return _channels.Where(ch => ch != CurrentChannel);
        }

        public bool HasChannel(ChannelComposition composition) => _channels.Contains(composition);

        public void AddChannel(ChannelComposition composition)
        {
            _channels.Add(composition);
        }

        public void RemoveChannel(ChannelComposition composition)
        {
            _channels.Remove(composition);
        }

        internal void UnsubscribeAll()
        {
            _channels.ForEach(ch => { ch.Unsubscribe(Identity, ChannelType.Environment); ch.Unsubscribe(Identity, ChannelType.Interaction); });
        }

        public ChannelRegistryEntry(NetworkIdentity identity)
        {
            Identity = identity;
        }
    }
}
