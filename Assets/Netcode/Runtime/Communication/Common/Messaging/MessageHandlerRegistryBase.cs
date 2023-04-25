using System;
using System.Collections.Generic;
using UnityEditor;

namespace Netcode.Runtime.Communication.Common.Messaging
{
    public abstract class MessageHandlerRegistryBase<T> where T : MessageHandlerBase
    {
        protected Dictionary<Type, Dictionary<Guid, T>> _handlers = new();

        public void RegisterHandler(T handler)
        {
            if (!_handlers.TryGetValue(handler.MessageType, out var handlers) )
            {
                handlers = new();
                _handlers.Add(handler.MessageType, handlers);
            }
            handlers.Add(handler.HandlerId, handler);
        }

        public bool IsHandlerRegistered(T handler) => _handlers.TryGetValue(handler.MessageType, out var handlers) && handlers.ContainsKey(handler.HandlerId);
        public bool IsHandlerRegistered<T_Message>(Guid handlerId) where T_Message : NetworkMessage => _handlers.TryGetValue(typeof(T_Message), out var handlers) && handlers.ContainsKey(handlerId);

        public void UnregisterHandler(T handler)
        {
            if (_handlers.TryGetValue(handler.MessageType, out var registry) && registry.ContainsKey(handler.HandlerId))
                registry.Remove(handler.HandlerId);
        }

        public void UnregisterHandler<T_Message>(Guid handlerId) where T_Message : NetworkMessage
        {
            if(_handlers.TryGetValue(typeof(T_Message), out var registry) && registry.ContainsKey(handlerId))
                registry.Remove(handlerId);
        }

        public void RegisterMessageHandlerIfNotExists (T handler)
        {
            if (!IsHandlerRegistered(handler)) RegisterHandler(handler);
        }
    }

    public class MessageHandlerRegistry : MessageHandlerRegistryBase<MessageHandler>
    {
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
    }
}
