using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Common
{
    public interface IMessageProtocolHandler
    {
        Task<NetworkMessage> DeserializeMessageAsync(Stream stream, IMACHandler macHandler = null, IEncryption encryption = null);
        byte[] SerializeMessage(NetworkMessage message, IMACHandler macHandler = null, IEncryption encryption = null);
    }
}
