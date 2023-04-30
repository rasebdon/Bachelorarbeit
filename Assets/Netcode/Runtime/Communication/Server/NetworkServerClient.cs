using Netcode.Runtime.Communication.Common;
using Netcode.Runtime.Communication.Common.Logging;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Security;
using System;
using System.Net;
using System.Net.Sockets;

namespace Netcode.Runtime.Communication.Server
{
    public class NetworkServerClient : NetworkClientBase<NetworkServerClient>
    {
        public NetworkServerClient(uint clientId, TcpClient client, UdpClient udpClient, IAsymmetricEncryption asymmetricEncryption, ILogger<NetworkServerClient> logger)
            : base(clientId, client, asymmetricEncryption, logger)
        {
            _tcpStream = client.GetStream();
            _udpClient = udpClient;
            MessageHandlerRegistry.RegisterHandler(new ActionMessageHandler<EncryptionInfoMessage>(OnEncryptionInfoMessageReceive, Guid.Parse("025725B8-AB25-4F9F-9EFA-BF819515FF91")));
        }

        private void OnEncryptionInfoMessageReceive(EncryptionInfoMessage msg, uint? senderClientId)
        {
            // Decrypt information from message with asymmetric encryption from server
            byte[] symmetricIV = _asymmetricEncryption.Decrypt(msg.SymmetricIV);
            byte[] symmetricKey = _asymmetricEncryption.Decrypt(msg.SymmetricKey);
            byte[] macKey = _asymmetricEncryption.Decrypt(msg.MACKey);

            _pipeline.AddEncryption(new AES256Encryption(symmetricIV, symmetricKey));
            _pipeline.AddMAC(new HMAC256Handler(macKey));

            _logger.LogInfo($"Encryption for client {ClientId} configured!");

            OnConnect?.Invoke(ClientId);
        }
    }
}
