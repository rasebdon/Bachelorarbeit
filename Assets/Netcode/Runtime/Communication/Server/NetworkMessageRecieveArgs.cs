using Netcode.Runtime.Communication.Common.Messaging;

namespace Netcode.Runtime.Communication.Server
{
    public class NetworkMessageRecieveArgs
    {
        public NetworkMessage Message { get; set; }

        public NetworkMessageRecieveArgs(NetworkMessage message)
        {
            Message = message;
        }
    }
}