using Netcode.Runtime.Communication.Common.Messaging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Common.Pipeline
{
    public class AddBatchMessageHeaderStep : IPipelineStep
    {
        public async Task<PipelineOutputObject> Apply(PipelineOutputObject input)
        {
            await input.OutputData.WriteAsync(BitConverter.GetBytes(input.Messages.Count()));
            return input;
        }

        public async Task<PipelineOutputObject> Revert(PipelineOutputObject input)
        {
            byte[] buffer = new byte[2];
            await input.OutputData.ReadAsync(buffer, 0, 2);
            input.Messages = new NetworkMessage[BitConverter.ToInt32(buffer)];
            return input;
        }
    }
}