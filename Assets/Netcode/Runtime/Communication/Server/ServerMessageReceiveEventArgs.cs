using Netcode.Runtime.Communication.Common.Messaging;

namespace Netcode.Runtime.Communication.Server
{
    public class ServerMessageReceiveEventArgs
    {
        public NetworkServerClient Client { get; set; }
        public NetworkMessage Message { get; set; }

        public ServerMessageReceiveEventArgs(NetworkServerClient client, NetworkMessage message)
        {
            Client = client;
            Message = message;
        }
    }
}