using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Common.Pipeline
{
    public class ReadWriteStreamStep : IPipelineStep
    {
        public PipelineOutputObject Apply(PipelineOutputObject output)
        {
            output.OutputData.InsertRange(0, BitConverter.GetBytes(output.OutputData.Count));
            return output;
        }

        public async Task<PipelineInputObject> Apply(PipelineInputObject input)
        {
            var bufferSize = new byte[4];
            await input.InputStream.ReadAsync(bufferSize);
            var inputBuffer = new byte[BitConverter.ToInt32(bufferSize)];
            await input.InputStream.ReadAsync(inputBuffer);
            input.InputBuffer = new(inputBuffer);
            return input;
        }
    }
}
