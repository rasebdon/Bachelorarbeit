using Netcode.Runtime.Communication.Common;
using Netcode.Runtime.Communication.Common.Logging;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Security;
using Netcode.Runtime.Communication.Server;
using System;
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
            catch(Exception ex)
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

        private void OnConnectionInfoMessageReceive(object sender, NetworkMessageRecieveArgs args)
        {
            if (args.Message is not ConnectionInfoMessage msg)
            {
                return;
            }

            // Set client id from server info
            ClientId = msg.ClientId;

            // Initialize encryption for communication
            var encryption = new AES256Encryption();

            // Initialize MAC for communication
            var macHandler = new HMAC256Handler();

            // Encrypt for transportation with server public key
            byte[] encryptedIV = _asymmetricEncryption.Encrypt(encryption.IV, msg.ServerPublicKey);
            byte[] encryptedKey = _asymmetricEncryption.Encrypt(encryption.Key, msg.ServerPublicKey);
            byte[] encryptedMACKey = _asymmetricEncryption.Encrypt(macHandler.Key, msg.ServerPublicKey);

            // Create information message
            EncryptionInfoMessage answer = new(
                encryptedIV,
                encryptedKey,
                encryptedMACKey);

            SendUdp(new RegisterUdpMessage());
            SendTcp(answer);

            ExecuteAfterTickOnce += () =>
            {
                _pipeline.AddEncryption(encryption);
                _pipeline.AddMAC(macHandler);
            };

            OnConnect?.Invoke(ClientId);
        }

        #endregion
    }
}
