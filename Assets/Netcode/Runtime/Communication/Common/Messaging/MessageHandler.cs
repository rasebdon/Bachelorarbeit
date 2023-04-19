using System;
using System.Collections.Generic;

namespace Netcode.Runtime.Communication.Common.Messaging
{
    public abstract class MessageHandler
    {
        public Guid HandlerId { get; }
        public Type MessageType { get; }

        protected MessageHandler(Guid handlerId, Type messageType)
        {
            HandlerId = handlerId;
            MessageType = messageType;
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

    public class MessageHandlerRegistry
    {
        private Dictionary<Type, Dictionary<Guid, MessageHandler>> _handlers = new();

        public void RegisterHandler(MessageHandler handler)
        {
            if (!_handlers.TryGetValue(handler.MessageType, out var handlers) )
            {
                handlers = new();
                _handlers.Add(handler.MessageType, handlers);
            }
            handlers.Add(handler.HandlerId, handler);
        }

        public bool IsHandlerRegistered(MessageHandler handler) => _handlers.TryGetValue(handler.MessageType, out var handlers) && handlers.ContainsKey(handler.HandlerId);
        public bool IsHandlerRegistered<T>(Guid handlerId) where T : NetworkMessage => _handlers.TryGetValue(typeof(T), out var handlers) && handlers.ContainsKey(handlerId);

        public void UnregisterHandler(MessageHandler handler)
        {
            if (_handlers.TryGetValue(handler.MessageType, out var registry) && registry.ContainsKey(handler.HandlerId))
                registry.Remove(handler.HandlerId);
        }

        public void UnregisterHandler<T>(Guid handlerId) where T : NetworkMessage
        {
            if(_handlers.TryGetValue(typeof(T), out var registry) && registry.ContainsKey(handlerId))
                registry.Remove(handlerId);
        }

        public void HandleMessage<T>(T message, uint? senderClientId) where T : NetworkMessage
        {
            var type = message.GetType();

            if (_handlers.TryGetValue(type, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    handler.Value.HandleMessage(message, senderClientId);
                }
            }

            if (type != typeof(NetworkMessage) &&
                _handlers.TryGetValue(typeof(NetworkMessage), out var generalHandlers))
            {
                foreach (var handler in generalHandlers)
                {
                    handler.Value.HandleMessage(message, senderClientId);
                }
            }
        }

        public void RegisterMessageHandlerIfNotExists (MessageHandler handler)
        {
            if (!IsHandlerRegistered(handler)) RegisterHandler(handler);
        }
    }
}
