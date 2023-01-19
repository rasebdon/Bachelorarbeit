using Netcode.Runtime.Communication.Common;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Security;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEditor.PackageManager;

namespace Netcode.Runtime.Communication.Server
{
    public class NetworkServerClient : IDisposable
    {
        public uint ClientId { get; }

        // Events
        public EventHandler<NetworkMessageRecieveArgs> OnReceive;

        // TCP related member variables
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;

        // UDP related member variables
        /// <summary>
        /// Returns wheter the udp client is already set up or not
        /// </summary>
        public bool UdpIsConfigured { get => UdpEndPoint == null; }
        public IPEndPoint UdpEndPoint { get; set; }
        private readonly UdpClient _udpClient;

        // Message Protocol Variables
        private readonly IMessageProtocolHandler _protocolHandler;

        private readonly IEncryption _encryption;
        private readonly IMACHandler _macHandler;

        public NetworkServerClient(uint clientId, TcpClient client, UdpClient udpClient, IMessageProtocolHandler protocolHandler)
        {
            ClientId = clientId;
            _protocolHandler = protocolHandler;
            _tcpClient = client;
            _udpClient = udpClient;
            _stream = client.GetStream();

            // Setup event handler for AES key and MAC key exchange
        }

        /// <summary>
        /// Sends a network message over the tcp stream of the server-client connection
        /// </summary>
        /// <param name="message"></param>
        public async void SendTcp(NetworkMessage message)
        {
            // Serialize the network message
            byte[] data = _protocolHandler.SerializeMessage(message, _macHandler, _encryption);

            // Write to the tcp network stream
            await _stream.WriteAsync(data, 0, data.Length);
        }

        /// <summary>
        /// Sends a UDP datagramm with the bytes of the given network message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async void SendUdpAsync(NetworkMessage message)
        {
            // Serialize the network message
            byte[] data = _protocolHandler.SerializeMessage(message, _macHandler, _encryption);

            // Send datagramm
            await _udpClient.SendAsync(data, data.Length, UdpEndPoint);
        }

        public async void BeginReceiveTcpAsync()
        {
            // Receive message over tcp stream
            await ReceiveMessage(_tcpClient.GetStream());

            // Start receiving next message
            BeginReceiveTcpAsync();
        }

        public async void ReceiveDatagramAsync(byte[] data)
        {
            // Receive message over datagram (converted to stream for deserialization)
            await ReceiveMessage(new MemoryStream(data));
        }

        /// <summary>
        /// Helper method that handles deserialization over the member protocol and
        /// invokes the OnReceive event
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private async Task ReceiveMessage(Stream stream)
        {
            // Get message from stream
            NetworkMessage message = await _protocolHandler.DeserializeMessageAsync(stream, _macHandler, _encryption);

            // Invoke the OnReceive event
            OnReceive?.Invoke(this, new NetworkMessageRecieveArgs(message));
        }

        public void Dispose()
        {
            _tcpClient.Dispose();
        }
    }
}
