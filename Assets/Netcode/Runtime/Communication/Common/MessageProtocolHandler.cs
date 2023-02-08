using Netcode.Runtime.Communication.Common.Exceptions;
using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Security;
using Netcode.Runtime.Communication.Common.Serialization;
using Netcode.Runtime.Communication.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting.FullSerializer;

namespace Netcode.Runtime.Communication.Common
{
    public class MessageProtocolHandler : IMessageProtocolHandler
    {
        private readonly IMessageSerializer _serializer;

        public MessageProtocolHandler(IMessageSerializer messageSerializer)
        {
            _serializer = messageSerializer;
        }

        public async Task<NetworkMessage> DeserializeMessageAsync(Stream stream, IMACHandler macHandler = null, IEncryption encryption = null)
        {
            // Get type info
            byte[] messageTypeIdBuffer = new byte[2];
            await stream.ReadAsync(messageTypeIdBuffer, 0, messageTypeIdBuffer.Length);
            short objectTypeId = BitConverter.ToInt16(messageTypeIdBuffer, 0);
            Type messageType = _serializer.GetMessageType(objectTypeId);

            // Get encryption flag
            byte[] isEncryptedBuffer = new byte[1];
            await stream.ReadAsync(isEncryptedBuffer, 0, isEncryptedBuffer.Length);

            // Get MAC data (if mac is configured)
            byte[] macBuffer = new byte[32];
            if (macHandler != null && macHandler.IsConfigured)
            {
                await stream.ReadAsync(macBuffer, 0, macBuffer.Length);
            }

            // Get data size (in bytes)
            byte[] dataSizeBuffer = new byte[4];
            await stream.ReadAsync(dataSizeBuffer, 0, dataSizeBuffer.Length);
            int dataSize = BitConverter.ToInt32(dataSizeBuffer, 0);

            if(dataSize == 0)
            {
                throw new RemoteClosedException("Error in deserialization, message size was 0!");
            }

            // Get data
            byte[] dataBuffer = new byte[dataSize];
            await stream.ReadAsync(dataBuffer, 0, dataBuffer.Length);

            // Check MAC with generated mac
            if (macHandler != null && macHandler.IsConfigured)
            {
                byte[] calculatedMac = macHandler.GenerateMAC(dataBuffer);
                if (!calculatedMac.SequenceEqual(macBuffer))
                {
                    throw new Exception("Invalid MAC, data may have been tampered with.");
                }
            }

            // Decrypt if encryption flag is set
            if (isEncryptedBuffer[0] == 1 && encryption != null)
            {
                dataBuffer = encryption.Decrypt(dataBuffer);
            }

            // Create message and return it
            return _serializer.Deserialize(dataBuffer, messageType);
        }

        public byte[] SerializeMessage<MessageType>(MessageType message, IMACHandler macHandler = null, IEncryption encryption = null) where MessageType : NetworkMessage
        {
            using MemoryStream ms = new();

            // Write type info
            ms.WriteAsync(BitConverter.GetBytes(_serializer.GetMessageTypeId(message.GetType())));

            // Write encryption flag
            bool isEncrypted = encryption != null && encryption.IsConfigured;
            ms.Write(BitConverter.GetBytes(isEncrypted));

            // Get data
            byte[] data = _serializer.Serialize(message);

            // Encrypt data (if configured)
            if (isEncrypted)
            {
                data = encryption.Encrypt(data);
            }

            // Generate and write MAC
            if (macHandler != null && macHandler.IsConfigured)
            {
                byte[] mac = macHandler.GenerateMAC(data);
                ms.Write(mac);
            }

            // Write data size (in bytes)
            ms.Write(BitConverter.GetBytes(data.Length));

            // Write data
            ms.Write(data);

            return ms.ToArray();
        }
    }
}
