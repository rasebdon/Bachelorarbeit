using Netcode.Runtime.Communication.Common.Serialization;
using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Unity.VisualScripting.FullSerializer;

namespace Netcode.Runtime.Communication.Common.Pipeline
{
    public class SerializeMessagesStep : IPipelineStep
    {
        private readonly IMessageSerializer _messageSerializer;

        public SerializeMessagesStep(IMessageSerializer serializer)
        {
            _messageSerializer = serializer;
        }

        public async Task<PipelineOutputObject> Apply(PipelineOutputObject input)
        {
            foreach (var message in input.Messages)
            {
                var serializedMessage = _messageSerializer.Serialize(message);
                await input.OutputData.WriteAsync(BitConverter.GetBytes(serializedMessage.Length));
                await input.OutputData.WriteAsync(BitConverter.GetBytes(_messageSerializer.GetMessageTypeId(message.GetType())));
                await input.OutputData.WriteAsync(serializedMessage);
            }

            return input;
        }

        public async Task<PipelineInputObject> Apply(PipelineInputObject input)
        {
            for (int i = 0; i < input.Messages.Length; i++)
            {
                byte[] typeBuffer = new byte[2];
                await input.InputData.ReadAsync(typeBuffer);
                Type messageType = _messageSerializer.GetMessageType(BitConverter.ToInt16(typeBuffer, 0));

                byte[] dataSizeBuffer = new byte[4];
                await input.InputData.ReadAsync(dataSizeBuffer);
                int dataSize = BitConverter.ToInt32(dataSizeBuffer, 0);

                byte[] messageData = new byte[dataSize];
                await input.InputData.ReadAsync(messageData);

                input.Messages[i] = _messageSerializer.Deserialize(messageData, messageType);
            }

            return input;
        }
    }
}