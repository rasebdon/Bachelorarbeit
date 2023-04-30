using Netcode.Runtime.Communication.Common.Logging;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Security;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
        public Dictionary<string, NetworkServerClient> UdpClientRegistry{ get; }
        private readonly object _clientListLock = new();

        /// <summary>
        /// Returns the next client id
        /// </summary>
        private uint NextClientId { get => _nextClientId++; }
        /// <summary>
        /// Do not use! Use <see cref="NextClientId"/> instead! 
        /// Starts at 1, 0 is the predesignated id for the server
        /// </summary>
        private uint _nextClientId = 1;

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
        public MessageHandlerRegistry MessageHandlerRegistry { get; }

        private TcpListener _tcpServer;

        private UdpClient _udpClient;
        private IPEndPoint _udpEndpoint;

        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<NetworkServer> _logger;

        private bool _stopped = true;

        private readonly IAsymmetricEncryption _asymmetricEncryption;

        public NetworkServer(
            ushort port,
            ushort maxClients,
            ILoggerFactory loggerFactory)
        {
            // Setup properties
            MaxClients = maxClients;
            Clients = new();
            UdpClientRegistry = new();
            MessageHandlerRegistry = new();

            // Setup member variables
            _udpEndpoint = new IPEndPoint(IPAddress.Any, port);
            _nextClientId = 0;
            _stopped = true;

            _asymmetricEncryption = new RSAEncryption();

            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<NetworkServer>();

            // Setup sockets
            _tcpServer = new TcpListener(IPAddress.Any, port);
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
                //_udpClient.BeginReceive(OnUdpReceive, null);
                ReceiveUdpAsync();

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

        public async void ReceiveUdpAsync()
        {
            try
            {
                while(!_stopped)
                {
                    var result = await _udpClient.ReceiveAsync();
                    IPEndPoint remoteEp = result.RemoteEndPoint;
                    byte[] data = result.Buffer;

                    if (data != null && data.Length > 0)
                    {
                        NetworkServerClient client = GetClientByRemoteEndPoint(remoteEp);
                        client.ReceiveDatagramAsync(data);
                    }
                }
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

        public void OnTick()
        {
            lock (_clientListLock)
                foreach (var client in Clients)
                    client.OnTick();
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
                    new UdpClient(),
                    _asymmetricEncryption,
                    _loggerFactory.CreateLogger<NetworkServerClient>());

                _logger.LogInfo("Incoming TCP client connection");
                _logger.LogDetail($"Client Info - ClientId: {client.ClientId}; EndPoint: {(IPEndPoint)tcpClient.Client.RemoteEndPoint}");

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
                    lock (_clientListLock)
                    {
                        Clients.Add(client);
                        UdpClientRegistry.Add(((IPEndPoint)tcpClient.Client.RemoteEndPoint).ToString(), client);
                    }
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
                        lock (_clientListLock)
                        {
                            Clients.Remove(client);
                            UdpClientRegistry.Remove(((IPEndPoint)tcpClient.Client.RemoteEndPoint).ToString());
                        }
                        client.Dispose();
                    };

                    // Setup receive on client
                    client.MessageHandlerRegistry.RegisterHandler(new ProxyMessageHandler(this.MessageHandlerRegistry, Guid.Parse("C476B4CA-EC68-4A54-A615-01C02E3827A9")));

                    // Send connection informations to client
                    client.SendTcp(new ConnectionInfoMessage(client.ClientId, _asymmetricEncryption.PublicKey));

                    client.BeginReceiveTcp();
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

        private NetworkServerClient GetClientByRemoteEndPoint(IPEndPoint remoteEp)
        {
            if(UdpClientRegistry.TryGetValue(remoteEp.ToString(), out var client))
                return client;
            return null;
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

                lock (_clientListLock) Clients.Clear();
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
