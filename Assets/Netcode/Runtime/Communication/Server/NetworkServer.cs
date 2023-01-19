using Netcode.Runtime.Communication.Common;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Security;
using Netcode.Runtime.Communication.Common.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Server
{
    /// <summary>
    /// The socket server that serves with UDP and TCP on the specified ports
    /// </summary>
    public class NetworkServer : IDisposable
    {
        /// <summary>
        /// The list of all currently connected clients
        /// </summary>
        public List<NetworkServerClient> Clients { get; }

        /// <summary>
        /// Returns the next client id
        /// </summary>
        private uint NextClientId { get => _nextClientId++; }
        /// <summary>
        /// Do not use! Use <see cref="NextClientId"/> instead!
        /// </summary>
        private uint _nextClientId = 0;

        public ushort MaxClients { get; }

        /// <summary>
        /// Gets invoked whenever a client successfully connects to the server
        /// </summary>
        public EventHandler<ServerConnectionEventArgs> OnServerClientConnect;

        /// <summary>
        /// Gets invoked whenever a client loses its connection to the server
        /// </summary>
        public EventHandler<ServerConnectionEventArgs> OnServerClientDisconnect;

        /// <summary>
        /// Gets invoked whenever the server receives a message from a client
        /// </summary>
        public EventHandler<ServerMessageReceiveEventArgs> OnServerMessageReceive;

        private TcpListener _tcpServer;

        private UdpClient _udpClient;
        private IPEndPoint _udpEndpoint;

        /// <summary>
        /// The serialzer that is used for serialization of network messages
        /// </summary>
        private readonly IMessageProtocolHandler _messageProtocolHandler;

        public NetworkServer(ushort tcpPort, ushort udpPort, ushort maxClients)
        {
            // Setup properties
            MaxClients = maxClients;
            Clients = new();

            // Setup member variables
            _udpEndpoint = new IPEndPoint(IPAddress.Any, udpPort);
            _nextClientId = 0;

            // TODO :
            // messageSerializer = new MessagePackSerialzer();
            // _messageProtocolHandler = new MessageProtocolHandler(messageSerializer);

            // Setup sockets
            _tcpServer = new TcpListener(IPAddress.Any, tcpPort);
        }

        public void Start()
        {
            _udpClient = new UdpClient(_udpEndpoint);
            _udpClient.BeginReceive(OnUdpReceive, null);

            // Start listening on the tcp port
            _tcpServer.Start();
            _tcpServer.BeginAcceptTcpClient(OnTcpConnect, null);
        }

        private void OnTcpConnect(IAsyncResult result)
        {
            // Accept the client
            TcpClient tcpClient = _tcpServer.EndAcceptTcpClient(result);
            NetworkServerClient client = new(NextClientId, tcpClient, _udpClient, _messageProtocolHandler);

            // Check if the server has space for additional clients
            if (Clients.Count == MaxClients)
            {
                // Send the client that the server is currently full
                client.SendTcp(new ServerFullMessage());
                client.Dispose();
            }
            else
            {
                // Client connection successful
                Clients.Add(client);
                OnServerClientConnect?.Invoke(this, new ServerConnectionEventArgs(client));

                // Setup receive on client
                client.OnReceive += (object sender, NetworkMessageRecieveArgs args) =>
                {
                    ServerMessageReceiveEventArgs newArgs = new((NetworkServerClient)sender, args.Message);
                    OnServerMessageReceive?.Invoke(this, newArgs);
                };
                client.BeginReceiveTcpAsync();
            }

            // Begin to listen again
            _tcpServer.BeginAcceptTcpClient(OnTcpConnect, null);
        }

        private async void OnUdpReceive(IAsyncResult result)
        {
            IPEndPoint remoteEp = new(IPAddress.Any, _udpEndpoint.Port);

            byte[] data = _udpClient.EndReceive(result, ref remoteEp);
            
            if (data != null && data.Length > 0)
            {
                // Find client with IPEndPoint of receiver
                NetworkServerClient client = Clients.Find(c => c.UdpIsConfigured && c.UdpEndPoint.Address == remoteEp.Address);

                if(client == null)
                {
                    // Check if the message that was received was a RegisterUdpMessage
                    NetworkMessage message = await _messageProtocolHandler.DeserializeMessageAsync(new MemoryStream(data));

                    if(message is RegisterUdpMessage msg)
                    {
                        client = Clients.Find(c => c.ClientId == msg.ClientId);
                        
                        if(client == null)
                        {
                            // Throw some error
                        }

                        // Set the client endpoint to the received message endpoint
                        client.UdpEndPoint = remoteEp;
                    }
                }

                // Call the on receive method for datagrams on the server client
                client.ReceiveDatagramAsync(data);
            }

            // Begin to receive datagrams again
            _udpClient.BeginReceive(OnUdpReceive, null);
        }

        public void Stop()
        {
            // Dispose clients
            Clients.ForEach(c => c.Dispose());

            _tcpServer.Stop();
            _udpClient.Dispose();
        }

        public void Dispose()
        {
            // Dispose clients
            Clients.ForEach(c => c.Dispose());

            _tcpServer.Stop();
            _udpClient.Dispose();
        }
    }
}
