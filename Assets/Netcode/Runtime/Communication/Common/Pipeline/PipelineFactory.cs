using Netcode.Runtime.Communication.Common.Pipeline;
using Netcode.Runtime.Communication.Common.Serialization;

namespace Netcode.Runtime.Communication.Common.Pipeline
{
    public static class PipelineFactory
    {
        public static IPipeline CreatePipeline() => new Pipeline()
            .AddStepLast(new SerializeMessagesStep(new DefaultMessageSerializer(new MessagePackDataSerializer())))
            .AddStepLast(new AddBatchMessageHeaderStep())
            .AddStepLast(new ReadWriteStreamStep());
    }
}
