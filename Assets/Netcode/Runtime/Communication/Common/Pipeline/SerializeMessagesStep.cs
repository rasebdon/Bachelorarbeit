using Netcode.Runtime.Communication.Common.Serialization;
using System;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Common.Pipeline
{
    public class SerializeMessagesStep : IPipelineStep
    {
        private readonly IMessageSerializer _messageSerializer;

        public SerializeMessagesStep(IMessageSerializer serializer)
        {
            _messageSerializer = serializer;
        }

        public PipelineOutputObject Apply(PipelineOutputObject output)
        {
            foreach (var message in output.Messages)
            {
                var serializedMessage = _messageSerializer.Serialize(message);
                output.OutputData.AddRange(BitConverter.GetBytes(_messageSerializer.GetMessageTypeId(message.GetType())));
                output.OutputData.AddRange(BitConverter.GetBytes(serializedMessage.Length));
                output.OutputData.AddRange(serializedMessage);
            }

            return output;
        }

        public async Task<PipelineInputObject> Apply(PipelineInputObject input)
        {
            for (int i = 0; i < input.Messages.Length; i++)
            {
                byte[] typeBuffer = input.InputBuffer.Consume(2);
                Type messageType = _messageSerializer.GetMessageType(BitConverter.ToInt16(typeBuffer, 0));

                byte[] dataSizeBuffer = input.InputBuffer.Consume(4);
                int dataSize = BitConverter.ToInt32(dataSizeBuffer, 0);

                byte[] messageData = input.InputBuffer.Consume(dataSize);

                input.Messages[i] = _messageSerializer.Deserialize(messageData, messageType);
            }

            return input;
        }
    }
}