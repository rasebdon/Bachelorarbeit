using Netcode.Runtime.Communication.Common.Exceptions;
using Netcode.Runtime.Communication.Common.Logging;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Pipeline;
using Netcode.Runtime.Communication.Common.Security;
using Netcode.Runtime.Communication.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

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

        private List<NetworkMessage> UdpMessageQueue_Out { get; } = new();
        private List<NetworkMessage> TcpMessageQueue_Out { get; } = new();
        private ConcurrentQueue<NetworkMessage> MessageQueue_In { get; } = new();

        protected readonly ILogger<T> _logger;

        public bool Disposed { get; private set; } = false;

        public Dictionary<Type, List<Action<NetworkMessage>>> OnMessageSent { get; set; } = new();

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public NetworkClientBase(
            uint clientId,
            TcpClient client,
            UdpClient udpClient,
            IAsymmetricEncryption asymmetricEncryption,
            ILogger<T> logger)
        {
            OnMessageSent = new();
            ClientId = clientId;
            _asymmetricEncryption = asymmetricEncryption;
            _cancellationTokenSource = new();
            _pipeline = PipelineFactory.CreatePipeline(_cancellationTokenSource);
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
        public void SendTcp<MessageType>(MessageType message) where MessageType : NetworkMessage
        {
            lock (_tcpWriteLock)
            {
                if (message is SyncNetworkVariableMessage syncMessage)
                {
                    var existingSyncVariable = TcpMessageQueue_Out.FirstOrDefault(msg => msg is SyncNetworkVariableMessage sync && sync.VariableHash == syncMessage.VariableHash);

                    if (existingSyncVariable != null)
                    {
                        if (existingSyncVariable.TimeStamp > syncMessage.TimeStamp)
                            return;
                        else
                            TcpMessageQueue_Out.Remove(existingSyncVariable);
                    }
                }
                TcpMessageQueue_Out.Add(message);
            }

        }

        /// <summary>
        /// Sends a UDP datagramm with the bytes of the given network message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public void SendUdp<MessageType>(MessageType message) where MessageType : NetworkMessage
        {
            lock (_udpWriteLock)
            {
                if (message is SyncNetworkVariableMessage syncMessage)
                {
                    var existingSyncVariable = UdpMessageQueue_Out.FirstOrDefault(msg => msg is SyncNetworkVariableMessage sync && sync.VariableHash == syncMessage.VariableHash);

                    if (existingSyncVariable != null)
                    {
                        if (existingSyncVariable.TimeStamp > syncMessage.TimeStamp)
                            return;
                        else
                            UdpMessageQueue_Out.Remove(existingSyncVariable);
                    }
                }
                UdpMessageQueue_Out.Add(message);
            }
        }

        public async void BeginReceiveTcpAsync()
        {
            try
            {
                await ReceiveMessage(_tcpClient.GetStream());

                BeginReceiveTcpAsync();
            }
            catch (ObjectDisposedException ex)
            {
                if (!Disposed)
                {
                    _logger.LogError($"Exception occurred in {nameof(BeginReceiveTcpAsync)}", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred in {nameof(BeginReceiveTcpAsync)}", ex);
            }
        }

        public async void ReceiveDatagramAsync(byte[] data)
        {
            try
            {
                await ReceiveMessage(new MemoryStream(data));
            }
            catch (ObjectDisposedException ex)
            {
                if (!Disposed)
                {
                    _logger.LogError($"Exception occurred in {nameof(ReceiveDatagramAsync)}", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred in {nameof(ReceiveDatagramAsync)}", ex);
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

                for (int i = 0; i < input.Messages.Length; i++)
                {
                    var message = input.Messages[i];
                    if (message != null)
                        MessageQueue_In.Enqueue(message);
                }

            }
            catch (Exception ex)
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

                _logger.LogError($"Fatal exception occurred in {nameof(ReceiveMessage)}", ex);
            }
        }

        public void Dispose()
        {
            if (Disposed)
                return;

            _cancellationTokenSource.Cancel();
            Disposed = true;
            OnDisconnect?.Invoke(ClientId);
            _tcpClient?.Dispose();
        }

        private readonly object _tcpWriteLock = new();
        private readonly object _udpWriteLock = new();
        public void OnTick()
        {
            if (_tcpClient.Connected && TcpMessageQueue_Out.Count > 0)
            {
                lock (_tcpWriteLock)
                {
                    PipelineOutputObject output = new()
                    {
                        Messages = TcpMessageQueue_Out.ToArray(),
                        OutputData = new(),
                    };
                    _tcpClient.GetStream().Write(_pipeline.RunPipeline(output).OutputData.ToArray());

                    for (int i = 0; i < output.Messages.Length; i++)
                    {
                        var message = output.Messages[i];
                        if (OnMessageSent.TryGetValue(message.GetType(), out var action) && action != null)
                            action.ForEach(a => a?.Invoke(message));
                    }

                    TcpMessageQueue_Out.Clear();
                }
            }

            if (UdpMessageQueue_Out.Count > 0 && UdpIsConfigured)
            {
                lock (_udpWriteLock)
                {
                    PipelineOutputObject output = new()
                    {
                        Messages = UdpMessageQueue_Out.ToArray(),
                        OutputData = new(),
                    };
                    var datagramm = _pipeline.RunPipeline(output).OutputData.ToArray();
                    _udpClient.Send(datagramm, datagramm.Length, UdpEndPoint);

                    for (int i = 0; i < output.Messages.Length; i++)
                    {
                        var message = output.Messages[i];
                        if (OnMessageSent.TryGetValue(message.GetType(), out var action) && action != null)
                            action.ForEach(a => a?.Invoke(message));
                    }

                    UdpMessageQueue_Out.Clear();
                }
            }

            while (!MessageQueue_In.IsEmpty)
                if (MessageQueue_In.TryDequeue(out var message))
                    OnReceive?.Invoke(this, new NetworkMessageRecieveArgs(message));
        }
    }
}
