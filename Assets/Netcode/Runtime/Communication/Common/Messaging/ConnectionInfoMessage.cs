using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Common.Messaging
{
    /// <summary>
    /// This message is the first message that is sent to the client after a connection
    /// to the server has been established
    /// </summary>
    public class ConnectionInfoMessage : NetworkMessage
    {
        /// <summary>
        /// The id of the client this message gets sent to
        /// (so that the client knows its id)
        /// </summary>
        public uint ClientId { get; set; }

        /// <summary>
        /// The public encryption key for later AES secret transfer
        /// </summary>
        public byte[] ServerPublicKey { get; set; }

        public ConnectionInfoMessage(uint clientId, byte[] serverPublicKey)
        {
            ClientId = clientId;
            ServerPublicKey = serverPublicKey;
        }
    }
}
