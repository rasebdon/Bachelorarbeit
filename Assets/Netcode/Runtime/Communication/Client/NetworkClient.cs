using Netcode.Runtime.Communication.Common;
using Netcode.Runtime.Communication.Common.Logging;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Security;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Client
{
    public class NetworkClient : NetworkClientBase<NetworkClient>
    {
        private Task _udpReceiveTask;

        public NetworkClient(
            ILogger<NetworkClient> logger)
            :
            base(
                0,
                new TcpClient(),
                new RSAEncryption(),
                logger)
        {
            UdpIsConfigured = true;
            MessageHandlerRegistry.RegisterHandler(new ActionMessageHandler<ConnectionInfoMessage>(OnConnectionInfoMessageReceive, Guid.Parse("DE17C09E-B072-41E5-B3EE-6D531A63077C")));
        }

        public async Task Connect(string hostname, ushort port)
        {
            ResetPipeline();

            try
            {
                await _tcpClient.ConnectAsync(hostname, port);
                _tcpStream = _tcpClient.GetStream();
            }
            catch (SocketException)
            {
                _logger.LogWarning("Connection to server failed, retrying...");
                await Connect(hostname, port);
                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            _udpClient = new UdpClient((IPEndPoint)_tcpClient.Client.LocalEndPoint);

            BeginReceiveTcp();
            BeginReceiveUdp();
        }

        private void BeginReceiveUdp()
        {
            _udpReceiveTask = Task.Factory.StartNew(() => ReceiveUdpAsync(), TaskCreationOptions.LongRunning);
        }

        private async void ReceiveUdpAsync()
        {
            UdpReceiveResult received = await _udpClient.ReceiveAsync();

            ReceiveDatagram(received.Buffer);
            ReceiveUdpAsync();
        }

        public override void Dispose()
        {
            _udpReceiveTask.Dispose();
            base.Dispose();
        }

        #region OnReceive Message Behaviours

        private IEncryption _encryption;
        private IMACHandler _macHandler;

        private void OnConnectionInfoMessageReceive(ConnectionInfoMessage msg, uint? senderClientId)
        {
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

            OnConnect?.Invoke(ClientId);
        }

        #endregion
    }
}
