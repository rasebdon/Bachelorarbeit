using System;
using System.Collections.Generic;
using UnityEngine;

namespace Netcode.Channeling
{
    public class ChannelHandler : MonoBehaviour
    {
        private readonly Dictionary<ushort, Channel> _channels = new();

        // Will be a sync var of some sorts
        private static ushort _nextChannelId = 0;

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        /// <summary>
        /// Registeres a channel for communication
        /// </summary>
        /// <param name="channel"></param>
        public void RegisterChannel(Channel channel)
        {
            _channels.Add(channel.ChannelId, channel);
        }

        public void UnregisterChannel(ushort channelId)
        {
            if(_channels.ContainsKey(channelId))
            {
                _channels.Remove(channelId);
            }
        }

        // Receive publish
        public void ReceivePublish(string message, ushort channelId)
        {
            Debug.Log("Receiving message from NetworkLayer...");

            if (_channels.TryGetValue(channelId, out Channel channel))
            {
                channel.ReceivePublish(message);
            }
        }

        // Send publish
        public void SendPublish(string message, Channel channel)
        {
            Debug.Log("Sending message to NetworkLayer...");

            // Simulate NetworkLayer...
            ReceivePublish(message, channel.ChannelId);
        }
    }
}
