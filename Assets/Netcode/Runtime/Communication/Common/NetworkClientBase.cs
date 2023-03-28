using Netcode.Runtime.Communication.Common.Logging;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Security;
using Netcode.Runtime.Communication.Server;
using System;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using Netcode.Runtime.Communication.Common.Exceptions;
using System.Collections.Generic;
using Netcode.Runtime.Communication.Common.Pipeline;

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
        protected readonly IPipeline _pipeline;

        // Asymmetric encryption for initialization
        protected readonly IAsymmetricEncryption _asymmetricEncryption;

        private Queue<NetworkMessage> UdpMessageQueue { get; } = new();
        private Queue<NetworkMessage> TcpMessageQueue { get; } = new();

        protected readonly ILogger<T> _logger;

        public bool Disposed { get; private set; } = false;

        protected Action ExecuteAfterTickOnce { get; set; }

        public NetworkClientBase(
            uint clientId,
            TcpClient client,
            UdpClient udpClient,
            IAsymmetricEncryption asymmetricEncryption,
            ILogger<T> logger)
        {
            ClientId = clientId;
            _asymmetricEncryption = asymmetricEncryption;
            _pipeline = PipelineFactory.CreatePipeline();
            _tcpClient = client;
            _udpClient = udpClient;
            _logger = logger;
            Disposed = false;
        }

        ~NetworkClientBase()
        {
            Dispose();
        }

        private readonly object _tcpWriteLock = new();
        /// <summary>
        /// Sends a network message over the tcp stream of the server-client connection
        /// </summary>
        /// <param name="message"></param>
        public void SendTcp<MessageType>(MessageType message) where MessageType : NetworkMessage
        {
            TcpMessageQueue.Enqueue(message);
        }

        private readonly object _udpWriteLock = new();
        /// <summary>
        /// Sends a UDP datagramm with the bytes of the given network message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public void SendUdp<MessageType>(MessageType message) where MessageType : NetworkMessage
        {
            UdpMessageQueue.Enqueue(message);
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
                    _logger.LogError($"Exception occurred in {nameof(BeginReceiveTcpAsync)}: {ex}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred in {nameof(BeginReceiveTcpAsync)}: {ex}");
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
                    _logger.LogError($"Exception occurred in {nameof(ReceiveDatagramAsync)}: {ex}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred in {nameof(ReceiveDatagramAsync)}: {ex}");
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
                var input = await _pipeline.RunPipeline(
                    new PipelineInputObject
                    {
                        InputStream = stream,
                    });

                foreach (var message in input.Messages)
                {
                    OnReceive?.Invoke(this, new NetworkMessageRecieveArgs(message));
                }
            }
            catch(Exception ex)
            {
                if (ex is ObjectDisposedException or IOException && Disposed)
                {
                    return;
                }

                if (ex is RemoteClosedException)
                {
                    _logger.LogInfo("Remote closed connection!");
                    Dispose();
                    return;
                }

                _logger.LogError($"Fatal exception occurred in {nameof(ReceiveMessage)}: {ex}");
            }
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Disposed = true;
            OnDisconnect?.Invoke(ClientId);
            _tcpClient?.Dispose();
        }

        public void OnTick()
        {
            lock (_tcpWriteLock)
            {
                PipelineOutputObject output = new()
                {
                    Messages = TcpMessageQueue.ToArray(),
                    OutputData = new(),
                };
                _tcpClient.GetStream().Write(_pipeline.RunPipeline(output).OutputData.ToArray());
            }

            lock (_udpWriteLock)
            {
                PipelineOutputObject output = new()
                {
                    Messages = UdpMessageQueue.ToArray(),
                    OutputData = new(),
                };
                output = _pipeline.RunPipeline(output);
                var datagramm = output.OutputData.ToArray();
                _udpClient.Send(datagramm, datagramm.Length);
            }

            ExecuteAfterTickOnce?.Invoke();
            ExecuteAfterTickOnce = null;
        }
    }
}
