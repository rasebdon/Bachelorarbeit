using Netcode.Runtime.Communication.Common;
using Netcode.Runtime.Communication.Common.Logging;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Security;
using Netcode.Runtime.Communication.Server;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Client
{
    public class NetworkClient : NetworkClientBase<NetworkClient>
    {
        public NetworkClient(
            ILogger<NetworkClient> logger)
            :
            base(
                0,
                new TcpClient(),
                new UdpClient(AddressFamily.InterNetwork),
                new RSAEncryption(),
                logger)
        {
            OnReceive += OnConnectionInfoMessageReceive;
        }

        public async Task Connect(string hostname, ushort tcpPort, ushort udpPort)
        {
            try
            {
                await _tcpClient.ConnectAsync(hostname, tcpPort);
            }
            catch (SocketException)
            {
                _logger.LogWarning("Connection to server failed, retrying...");
                await Connect(hostname, tcpPort, udpPort);
                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }

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

        private IEncryption _encryption;
        private IMACHandler _macHandler;

        private void OnConnectionInfoMessageReceive(object sender, NetworkMessageRecieveArgs args)
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


            if (!OnMessageSent.ContainsKey(typeof(EncryptionInfoMessage)))
                OnMessageSent.Add(typeof(EncryptionInfoMessage), new List<Action<NetworkMessage>>());

            OnMessageSent[typeof(EncryptionInfoMessage)].Add(OnEncryptionInfoMessageSent);

            SendTcp(answer);
        }

        private void OnEncryptionInfoMessageSent(NetworkMessage message)
        {
            _pipeline.AddEncryption(_encryption);
            _pipeline.AddMAC(_macHandler);
            SendUdp(new RegisterUdpMessage());

            OnConnect?.Invoke(ClientId);
        }

        #endregion
    }
}
