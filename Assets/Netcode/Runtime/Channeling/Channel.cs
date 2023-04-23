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
        public Action<NetworkMessage, uint?> OnDistribute { get; private set; }

        public ChannelType Type { get; }

        public Channel(ChannelType type)
        {
            Type = type;
        }

        public void Publish<T>(T message, uint? clientId) where T : NetworkMessage
        {
            OnDistribute?.Invoke(message, clientId);
        }

        /// <summary>
        /// Subscribes a <see cref="NetworkIdentity"/> to this channel
        /// </summary>
        /// <param name="netId"></param>
        public void Subscribe(NetworkIdentity netId)
        {
            OnDistribute += netId.OnReceiveMessage.HandleMessage;
        }

        /// <summary>
        /// Unsubscribes a <see cref="NetworkIdentity"/> from this channel
        /// </summary>
        /// <param name="netId"></param>
        public void Unsubscribe(NetworkIdentity netId)
        {
            OnDistribute -= netId.OnReceiveMessage.HandleMessage;
        }
    }
}
