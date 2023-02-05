using Netcode.Runtime.Communication.Common.Logging;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Security;
using Netcode.Runtime.Communication.Server;
using System;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Runtime.InteropServices.ComTypes;
using Netcode.Runtime.Communication.Common.Exceptions;

namespace Netcode.Runtime.Communication.Common
{
    public abstract class NetworkClientBase<T> : IDisposable
    {
        public uint ClientId { get; protected set; }

        // Events
        public EventHandler<NetworkMessageRecieveArgs> OnReceive;
        public Action<uint> OnDisconnect;
        public Action<uint> OnConnect;

        // TCP related member variables
        protected readonly TcpClient _tcpClient;

        // UDP related member variables
        /// <summary>
        /// Returns wheter the udp client is already set up or not
        /// </summary>
        public bool UdpIsConfigured { get; set; }
        public IPEndPoint UdpEndPoint { get; set; }
        protected readonly UdpClient _udpClient;

        // Message Protocol Variables
        protected readonly IMessageProtocolHandler _protocolHandler;

        // Asymmetric encryption for initialization
        protected readonly IAsymmetricEncryption _asymmetricEncryption;

        protected IEncryption _encryption;
        protected IMACHandler _macHandler;

        protected readonly ILogger<T> _logger;

        public bool Disposed { get; private set; } = false;

        public NetworkClientBase(
            uint clientId,
            TcpClient client,
            UdpClient udpClient,
            IMessageProtocolHandler protocolHandler,
            IAsymmetricEncryption asymmetricEncryption,
            ILogger<T> logger)
        {
            ClientId = clientId;
            _asymmetricEncryption = asymmetricEncryption;
            _protocolHandler = protocolHandler;
            _tcpClient = client;
            _udpClient = udpClient;
            _logger = logger;
            Disposed = false;
        }

        ~NetworkClientBase()
        {
            Dispose();
        }

        /// <summary>
        /// Sends a network message over the tcp stream of the server-client connection
        /// </summary>
        /// <param name="message"></param>
        public async Task SendTcpAsync<MessageType>(MessageType message) where MessageType : NetworkMessage
        {
            // Serialize the network message
            byte[] data = _protocolHandler.SerializeMessage(message, _macHandler, _encryption);

            // Write to the tcp network stream
            await _tcpClient.GetStream().WriteAsync(data, 0, data.Length);

            _logger.LogDetail($"Sending {message.GetType().Name} over TCP");
        }

        /// <summary>
        /// Sends a UDP datagramm with the bytes of the given network message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendUdpAsync(NetworkMessage message)
        {
            _logger.LogDetail($"Sending {message.GetType().Name} over UDP");

            // Serialize the network message
            byte[] data = _protocolHandler.SerializeMessage(message, _macHandler, _encryption);

            // Send datagramm
            await _udpClient.SendAsync(data, data.Length, UdpEndPoint);
        }

        public async void BeginReceiveTcpAsync()
        {
            try
            {
                // Receive message over tcp stream
                await ReceiveMessage(_tcpClient.GetStream());

                // Start receiving next message
                BeginReceiveTcpAsync();
            }
            catch (ObjectDisposedException ex)
            {
                if (!Disposed)
                {
                    _logger.LogError(ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }

        public async void ReceiveDatagramAsync(byte[] data)
        {
            try
            {
                // Receive message over datagram (converted to stream for deserialization)
                await ReceiveMessage(new MemoryStream(data));
            }
            catch (ObjectDisposedException ex)
            {
                if (!Disposed)
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
        /// Helper method that handles deserialization over the member protocol and
        /// invokes the OnReceive event
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected async Task ReceiveMessage(Stream stream)
        {
            try
            {
                // Get message from stream
                NetworkMessage message = await _protocolHandler.DeserializeMessageAsync(stream, _macHandler, _encryption);

                // Invoke the OnReceive event
                OnReceive?.Invoke(this, new NetworkMessageRecieveArgs(message));
            }
            catch(Exception ex)
            {
                if (ex is ObjectDisposedException or IOException && Disposed)
                {
                    return;
                }

                if (ex is ClientDisconnectedException)
                {
                    _logger.LogInfo("Client closed connection!");
                    return;
                }

                _logger.LogError(ex);
            }
        }

        public void Dispose()
        {
            Disposed = true;

            OnDisconnect?.Invoke(ClientId);
            
            _tcpClient?.Dispose();
        }
    }
}
