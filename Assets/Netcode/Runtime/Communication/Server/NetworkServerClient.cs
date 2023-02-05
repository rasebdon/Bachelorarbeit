using Netcode.Runtime.Communication.Common;
using Netcode.Runtime.Communication.Common.Logging;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Security;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Server
{
    public class NetworkServerClient : NetworkClientBase<NetworkServerClient>
    {
        public NetworkServerClient(uint clientId, TcpClient client, UdpClient udpClient, IMessageProtocolHandler protocolHandler, IAsymmetricEncryption asymmetricEncryption, ILogger<NetworkServerClient> logger) 
            : base(clientId, client, udpClient, protocolHandler, asymmetricEncryption, logger)
        {
            OnReceive += OnEncryptionInfoMessageReceive;
        }

        private void OnEncryptionInfoMessageReceive(object sender, NetworkMessageRecieveArgs args)
        {
            if (args.Message is not EncryptionInfoMessage msg)
            {
                return;
            }

            // Decrypt information from message with asymmetric encryption from server
            byte[] symmetricIV = _asymmetricEncryption.Decrypt(msg.SymmetricIV);
            byte[] symmetricKey = _asymmetricEncryption.Decrypt(msg.SymmetricKey);
            byte[] macKey = _asymmetricEncryption.Decrypt(msg.MACKey);

            _encryption = new AES256Encryption(symmetricIV, symmetricKey);
            _macHandler = new HMAC256Handler(macKey);

            _encryption.IsConfigured = true;
            _macHandler.IsConfigured = true;

            OnConnect?.Invoke(ClientId);
        }
    }
}
