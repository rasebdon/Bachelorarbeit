namespace Netcode.Runtime.Communication.Server
{
    public class ServerConnectionEventArgs
    {
        public NetworkServerClient Client { get; set; }

        public ServerConnectionEventArgs(NetworkServerClient client)
        {
            Client = client;
        }
    }
}