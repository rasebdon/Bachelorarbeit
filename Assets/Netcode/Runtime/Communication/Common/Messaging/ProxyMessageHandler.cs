using System;

namespace Netcode.Runtime.Communication.Common.Messaging
{
    public class ProxyMessageHandler : MessageHandler
    {
        private readonly MessageHandlerRegistry _targetRegistry;

        public ProxyMessageHandler(MessageHandlerRegistry targetRegistry, Guid handlerId) : base(handlerId, typeof(NetworkMessage))
        {
            _targetRegistry = targetRegistry;
        }

        public override void HandleMessage(NetworkMessage message, uint? senderClientId = null)
        {
            _targetRegistry.HandleMessage(message, senderClientId);
        }
    }
}
