using Netcode.Runtime.Communication.Common.Logging;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Security;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

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

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<NetworkServer> _logger;

        private bool _stopped = true;

        private readonly IAsymmetricEncryption _asymmetricEncryption;

        public NetworkServer(
            ushort tcpPort,
            ushort udpPort,
            ushort maxClients,
            ILoggerFactory loggerFactory)
        {
            // Setup properties
            MaxClients = maxClients;
            Clients = new();

            // Setup member variables
            _udpEndpoint = new IPEndPoint(IPAddress.Any, udpPort);
            _nextClientId = 0;
            _stopped = true;

            _asymmetricEncryption = new RSAEncryption();

            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<NetworkServer>();

            // Setup sockets
            _tcpServer = new TcpListener(IPAddress.Any, tcpPort);
        }

        /// <summary>
        /// Starts the network server and listens for incoming TCP connections and UDP datagrams
        /// </summary>
        public void Start()
        {
            try
            {
                // Start listening on the udp port
                _udpClient = new UdpClient(_udpEndpoint);
                _udpClient.BeginReceive(OnUdpReceive, null);

                // Start listening on the tcp port
                _tcpServer.Start();
                _tcpServer.BeginAcceptTcpClient(OnTcpConnect, null);

                _stopped = false;

                _logger.LogInfo("Started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }

        public void OnTick()
        {
            foreach (var client in Clients)
            {
                client.OnTick();
            }
        }

        private void OnTcpConnect(IAsyncResult result)
        {
            try
            {
                // Accept the client
                TcpClient tcpClient = _tcpServer.EndAcceptTcpClient(result);
                NetworkServerClient client = new(
                    NextClientId,
                    tcpClient,
                    _udpClient,
                    _asymmetricEncryption,
                    _loggerFactory.CreateLogger<NetworkServerClient>());

                _logger.LogInfo("Incoming TCP client connection");
                _logger.LogDetail($"Client Info - ClientId: {client.ClientId}; EndPoint: {((IPEndPoint)tcpClient.Client.RemoteEndPoint)}");

                // Check if the server has space for additional clients
                if (Clients.Count == MaxClients)
                {
                    // Send the client that the server is currently full
                    client.SendTcp(new ServerFullMessage());
                    client.Dispose();

                    _logger.LogInfo("Client rejected because server is full");
                    _logger.LogDetail($"Current/Max clients: {Clients.Count}/{MaxClients}");
                }
                else
                {
                    // Client connection started
                    Clients.Add(client);
                    client.OnConnect += (uint clientId) =>
                    {
                        _logger.LogInfo($"Client {clientId} connected!");

                        OnServerClientConnect.Invoke(client, new ServerConnectionEventArgs(client));
                    };

                    // Setup disconnect event
                    client.OnDisconnect += (uint clientId) =>
                    {
                        _logger.LogInfo($"Client {clientId} disconnecting!");

                        OnServerClientDisconnect?.Invoke(this, new ServerConnectionEventArgs(client));
                        Clients.Remove(client);

                        client.Dispose();
                    };

                    // Setup receive on client
                    client.OnReceive += (object sender, NetworkMessageRecieveArgs args) =>
                    {
                        NetworkServerClient client = (NetworkServerClient)sender;
                        _logger.LogDetail($"Message received for client {client.ClientId} of type {args.Message.GetType().Name}");

                        ServerMessageReceiveEventArgs newArgs = new(client, args.Message);
                        OnServerMessageReceive?.Invoke(this, newArgs);
                    };

                    // Send connection informations to client
                    client.SendTcp(new ConnectionInfoMessage(client.ClientId, _asymmetricEncryption.PublicKey));

                    client.BeginReceiveTcpAsync();
                }

                // Begin to listen again
                _tcpServer.BeginAcceptTcpClient(OnTcpConnect, null);
            }
            catch (ObjectDisposedException ex)
            {
                if (!_stopped)
                {
                    _logger.LogError("", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }

        private void OnUdpReceive(IAsyncResult result)
        {
            try
            {
                IPEndPoint remoteEp = new(IPAddress.Any, _udpEndpoint.Port);

                byte[] data = _udpClient.EndReceive(result, ref remoteEp);

                _logger.LogInfo($"Incoming UDP datagram from {remoteEp}");

                if (data != null && data.Length > 0)
                {
                    NetworkServerClient client = Clients.Find(c => c.UdpIsConfigured && c.UdpEndPoint.Address == remoteEp.Address);

                    if (client == null)
                    {
                        _logger.LogInfo($"Could not find client with udp address {remoteEp}, registering client...");

                        client.UdpEndPoint = remoteEp;
                        client.UdpIsConfigured = true;
                    }

                    client.ReceiveDatagramAsync(data);
                }

                _udpClient.BeginReceive(OnUdpReceive, null);
            }
            catch (ObjectDisposedException ex)
            {
                if (!_stopped)
                {
                    _logger.LogError(ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }

        /// <summary>
        /// Stops the server and disposes its referenced objects (<see cref="Clients"/>, <see cref="TcpListener"/>, <see cref="UdpClient"/>)
        /// </summary>
        public void Stop()
        {
            if (_stopped)
                return;

            try
            {
                _stopped = true;

                // Dispose clients
                NetworkServerClient[] clients = new NetworkServerClient[Clients.Count];
                Clients.CopyTo(clients);

                for (int i = 0; i < clients.Length; i++)
                {
                    clients[i]?.Dispose();
                }

                Clients.Clear();
                _udpClient?.Dispose();
                _tcpServer?.Stop();

                _logger.LogInfo("Stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }

        /// <summary>
        /// Equivalent to <see cref="Stop"/>
        /// </summary>
        public void Dispose()
        {
            Stop();
        }
    }
}
