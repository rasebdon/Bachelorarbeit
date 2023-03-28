using Netcode.Runtime.Communication.Common.Messaging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Common.Pipeline
{
    public class AddBatchMessageHeaderStep : IPipelineStep
    {
        public PipelineOutputObject Apply(PipelineOutputObject output)
        {
            output.OutputData.InsertRange(0, BitConverter.GetBytes((short)output.Messages.Count()));
            return output;
        }

        public async Task<PipelineInputObject> Apply(PipelineInputObject input)
        {
            byte[] messageCount = input.InputBuffer.Consume(2);
            input.Messages = new NetworkMessage[BitConverter.ToInt16(messageCount)];
            return input;
        }
    }
}