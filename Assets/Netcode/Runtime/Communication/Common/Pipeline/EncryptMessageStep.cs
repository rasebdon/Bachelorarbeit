using Netcode.Runtime.Communication.Common.Security;
using System;
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

        public async Task<PipelineOutputObject> Apply(PipelineOutputObject input)
        {
            var encrypted = _encryption.Encrypt(input.OutputData.ToArray());
            await input.OutputData.FlushAsync();
            await input.OutputData.WriteAsync(BitConverter.GetBytes(encrypted.Length));
            await input.OutputData.WriteAsync(encrypted);
            return input;
        }

        public async Task<PipelineInputObject> Apply(PipelineInputObject input)
        {
            var dataLength = new byte[4];
            await input.InputData.ReadAsync(dataLength);

            var data = new byte[BitConverter.ToInt32(dataLength)];
            await input.InputData.ReadAsync(data);
            await input.InputData.FlushAsync();
            await input.InputData.WriteAsync(_encryption.Decrypt(data));
            return input;
        }
    }
}