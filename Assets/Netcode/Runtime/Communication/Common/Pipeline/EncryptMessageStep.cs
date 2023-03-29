using Netcode.Runtime.Communication.Common.Security;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Common.Pipeline
{
    public class EncryptMessageStep : IPipelineStep
    {
        private readonly IEncryption _encryption;

        public EncryptMessageStep(IEncryption encryption)
        {
            this._encryption = encryption;
        }

        public PipelineOutputObject Apply(PipelineOutputObject output)
        {
            var encrypted = _encryption.Encrypt(output.OutputData.ToArray());
            output.OutputData.Clear();
            output.OutputData.AddRange(encrypted);
            return output;
        }

        public async Task<PipelineInputObject> Apply(PipelineInputObject input)
        {
            input.InputBuffer = new(_encryption.Decrypt(input.InputBuffer.ToArray()));
            return input;
        }
    }
}