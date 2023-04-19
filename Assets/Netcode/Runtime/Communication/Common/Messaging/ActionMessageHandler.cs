using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Common.Messaging
{
    public class ActionMessageHandler<T> : TypedMessageHandler<T> where T : NetworkMessage
    {
        private readonly Action<T, uint?> _action;

        public ActionMessageHandler(Action<T, uint?> action, Guid handlerId) : base(handlerId)
        {
            _action = action;
        }

        public override void HandleGenericMessage(T message, uint? senderClientId = null)
        {
            _action.Invoke(message, senderClientId);
        }
    }
}
