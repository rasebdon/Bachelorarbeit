using Netcode.Runtime.Communication.Common.Serialization;
using System.Threading;

namespace Netcode.Runtime.Communication.Common.Pipeline
{
    public static class PipelineFactory
    {
        public static IPipeline CreatePipeline(CancellationTokenSource cancellationTokenSource) => new Pipeline()
            .AddStepLast(new SerializeMessagesStep(new DefaultMessageSerializer(new MessagePackDataSerializer())))
            .AddStepLast(new AddBatchMessageHeaderStep())
            .AddStepLast(new ReadWriteStreamStep(cancellationTokenSource));
    }
}
