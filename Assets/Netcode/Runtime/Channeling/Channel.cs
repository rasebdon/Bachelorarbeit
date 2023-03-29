using Netcode.Runtime.Behaviour;
using Netcode.Runtime.Communication.Common.Messaging;
using System;

namespace Netcode.Channeling
{
    /// <summary>
    /// Channels forward their messages to their subscribed objects
    /// </summary>
    public class Channel
    {
        /// <summary>
        /// Distribution list to players
        /// </summary>
        public Action<NetworkMessage> OnDistribute { get; private set; }

        public ChannelType Type { get; }

        public Channel(ChannelType type)
        {
            Type = type;
        }

        public void Publish<T>(T message) where T : NetworkMessage
        {
            OnDistribute?.Invoke(message);
        }

        /// <summary>
        /// Subscribes a <see cref="NetworkIdentity"/> to this channel
        /// </summary>
        /// <param name="netId"></param>
        public void Subscribe(NetworkIdentity netId)
        {
            OnDistribute += netId.OnReceiveMessage;
        }

        /// <summary>
        /// Unsubscribes a <see cref="NetworkIdentity"/> from this channel
        /// </summary>
        /// <param name="netId"></param>
        public void Unsubscribe(NetworkIdentity netId)
        {
            OnDistribute -= netId.OnReceiveMessage;
        }
    }
}
