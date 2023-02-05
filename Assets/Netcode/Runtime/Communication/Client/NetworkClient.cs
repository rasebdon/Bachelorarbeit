using Netcode.Runtime.Communication.Common;
using Netcode.Runtime.Communication.Common.Logging;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Security;
using Netcode.Runtime.Communication.Server;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEditor.PackageManager;

namespace Netcode.Runtime.Communication.Client
{
    public class NetworkClient : NetworkClientBase<NetworkClient>
    {
        public NetworkClient(
            IMessageProtocolHandler protocolHandler,
            ILogger<NetworkClient> logger) 
            : 
            base(   
                0,
                new TcpClient(), 
                new UdpClient(AddressFamily.InterNetwork),
                protocolHandler,
                new RSAEncryption(),
                logger)
        { 
            OnReceive += OnConnectionInfoMessageReceive;
        }

        public async void Connect(string hostname, ushort tcpPort, ushort udpPort)
        {
            await _tcpClient.ConnectAsync(hostname, tcpPort);
            UdpEndPoint = new IPEndPoint(IPAddress.Parse(hostname), udpPort);

            BeginReceiveTcpAsync();
            BeginReceiveUdpAsync();
        }

        private async void BeginReceiveUdpAsync()
        {
            UdpReceiveResult received = await _udpClient.ReceiveAsync();

            ReceiveDatagramAsync(received.Buffer);
            BeginReceiveUdpAsync();
        }

        #region OnReceive Message Behaviours

        private async void OnConnectionInfoMessageReceive(object sender, NetworkMessageRecieveArgs args)
        {
            if (args.Message is not ConnectionInfoMessage msg)
            {
                return;
            }

            // Set client id from server info
            ClientId = msg.ClientId;

            // Initialize encryption for communication
            _encryption = new AES256Encryption();

            // Initialize MAC for communication
            _macHandler = new HMAC256Handler();

            // Encrypt for transportation with server public key
            byte[] encryptedIV = _asymmetricEncryption.Encrypt(_encryption.IV, msg.ServerPublicKey);
            byte[] encryptedKey = _asymmetricEncryption.Encrypt(_encryption.Key, msg.ServerPublicKey);
            byte[] encryptedMACKey = _asymmetricEncryption.Encrypt(_macHandler.Key, msg.ServerPublicKey);

            // Create information message
            EncryptionInfoMessage answer = new(
                encryptedIV,
                encryptedKey,
                encryptedMACKey);

            await SendUdpAsync(new RegisterUdpMessage(ClientId));

            await SendTcpAsync(answer);

            _encryption.IsConfigured = true;
            _macHandler.IsConfigured = true;

            OnConnect?.Invoke(ClientId);
        }

        #endregion
    }
}
