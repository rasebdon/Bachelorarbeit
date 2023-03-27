using Netcode.Runtime.Communication.Common.Security;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Common.Pipeline
{
    internal class AddMACToMessageStep : IPipelineStep
    {
        private IMACHandler _macHandler;

        public AddMACToMessageStep(IMACHandler macHandler)
        {
            _macHandler = macHandler;
        }

        public async Task<PipelineOutputObject> Apply(PipelineOutputObject output)
        {
            await output.OutputData.WriteAsync(_macHandler.GenerateMAC(output.OutputData.ToArray()));
            return output;
        }

        public async Task<PipelineInputObject> Apply(PipelineInputObject input)
        {
            var macData = new byte[32];
            await input.InputData.ReadAsync(macData);

            if(!_macHandler.GenerateMAC(input.InputData.ToArray()).SequenceEqual(macData))
            {
                throw new Exception("Invalid MAC, data may have been tampered with!");
            }
            return input;
        }
    }
}