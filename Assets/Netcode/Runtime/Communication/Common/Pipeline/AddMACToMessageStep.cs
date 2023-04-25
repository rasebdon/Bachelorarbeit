using Netcode.Runtime.Communication.Common.Exceptions;
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

        public PipelineOutputObject Apply(PipelineOutputObject output)
        {
            var mac = _macHandler.GenerateMAC(output.OutputData.ToArray());
            output.OutputData.InsertBeginning(mac);
            output.OutputData.InsertBeginning(BitConverter.GetBytes((short)mac.Length));
            return output;
        }

        public async Task<PipelineInputObject> Apply(PipelineInputObject input)
        {
            var macLengthData = input.InputBuffer.Consume(2);
            var macLength = BitConverter.ToInt16(macLengthData);

            var mac = input.InputBuffer.Consume(macLength);

            if (!_macHandler.GenerateMAC(input.InputBuffer.ToArray()).SequenceEqual(mac))
            {
                throw new InvalidMACException();
            }
            return input;
        }
    }
}