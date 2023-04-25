using System;

namespace Netcode.Runtime.Communication.Common.Messaging
{
    public abstract class MessageHandlerBase
    {
        public Guid HandlerId { get; }
        public Type MessageType { get; }

        protected MessageHandlerBase(Guid handlerId, Type messageType)
        {
            HandlerId = handlerId;
            MessageType = messageType;
        }
    }

    public abstract class MessageHandler : MessageHandlerBase
    {
        protected MessageHandler(Guid handlerId, Type messageType) : base(handlerId, messageType)
        {
        }

        public abstract void HandleMessage(NetworkMessage message, uint? senderClientId = null);
    }

    public abstract class TypedMessageHandler<T> : MessageHandler where T : NetworkMessage
    {
        protected TypedMessageHandler(Guid handlerId) : base(handlerId, typeof(T))
        {
        }

        public override void HandleMessage(NetworkMessage message, uint? senderClientId = null)
        {
            HandleGenericMessage(message as T, senderClientId);
        }

        public abstract void HandleGenericMessage(T message, uint? senderClientId = null);
    }
}
