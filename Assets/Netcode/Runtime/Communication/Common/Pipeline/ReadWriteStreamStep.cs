using System;
using System.Threading;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Common.Pipeline
{
    public class ReadWriteStreamStep : IPipelineStep
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public ReadWriteStreamStep(CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;
        }

        public PipelineOutputObject Apply(PipelineOutputObject output)
        {
            output.OutputData.InsertBeginning(BitConverter.GetBytes(output.OutputData.Length));
            return output;
        }

        public async Task<PipelineInputObject> Apply(PipelineInputObject input)
        {
            var bufferSize = new byte[4];
            await input.InputStream.ReadAsync(bufferSize, _cancellationTokenSource.Token);
            var inputBuffer = new byte[BitConverter.ToInt32(bufferSize)];
            await input.InputStream.ReadAsync(inputBuffer, _cancellationTokenSource.Token);
            input.InputBuffer = new(inputBuffer);
            return input;
        }
    }
}
