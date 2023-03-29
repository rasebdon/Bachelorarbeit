using Netcode.Runtime.Communication.Common.Messaging;
using Netcode.Runtime.Communication.Common.Security;
using Netcode.Runtime.Communication.Common.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Common.Pipeline
{
    public interface IPipeline
    {
        public PipelineOutputObject RunPipeline(PipelineOutputObject output);
        public Task<PipelineInputObject> RunPipeline(PipelineInputObject input);
        IPipeline AddStepBefore(IPipelineStep step, IEnumerable<Type> insertBefore);
        IPipeline AddStepAfter(IPipelineStep step, IEnumerable<Type> insertAfter);
        IPipeline AddStepFirst(IPipelineStep step);
        IPipeline AddStepLast(IPipelineStep step);
        IPipeline AddEncryption(IEncryption encryption);
        IPipeline AddMAC(IMACHandler macHandler);
    }

    public class Pipeline : IPipeline
    {
        public IPipeline AddEncryption(IEncryption encryption)
        {
            return AddStepBefore(new EncryptMessageStep(encryption),
                new Type[]
                {
                    typeof(ReadWriteStreamStep),
                    typeof(AddMACToMessageStep)
                });
        }

        public IPipeline AddMAC(IMACHandler macHandler)
        {
            return AddStepBefore(new AddMACToMessageStep(macHandler), 
                new Type[]
                {
                    typeof(ReadWriteStreamStep),
                });
        }

        private List<IPipelineStep> _steps = new();

        public IPipeline AddStepAfter(IPipelineStep step, IEnumerable<Type> insertAfter)
        {
            int index = _steps.Count;
            if (insertAfter != null && insertAfter.Count() > 0)
            {
                foreach (var type in insertAfter)
                {
                    int typeIndex = _steps.FindIndex(s => s.GetType() == type);
                    if (index <= typeIndex && typeIndex != -1)
                        index = typeIndex + 1;
                }
            }

            return AddStep(step, index);
        }
        public IPipeline AddStepBefore(IPipelineStep step, IEnumerable<Type> insertBefore)
        {
            int index = _steps.Count;
            if (insertBefore != null && insertBefore.Count() > 0)
            {
                foreach (var type in insertBefore)
                {
                    int typeIndex = _steps.FindIndex(s => s.GetType() == type);
                    if (index > typeIndex && typeIndex != -1)
                        index = typeIndex;
                }
            }

            return AddStep(step, index);
        }
        public IPipeline AddStepFirst(IPipelineStep step)
        {
            return AddStep(step, 0);
        }
        public IPipeline AddStepLast(IPipelineStep step)
        {
            return AddStep(step, _steps.Count);
        }
        private IPipeline AddStep(IPipelineStep step, int index)
        {
            _steps.Insert(index, step);
            return this;
        }

        public async Task<PipelineInputObject> RunPipeline(PipelineInputObject input)
        {
            for (int i = 0; i < _steps.Count; i++)
            {
                var step = _steps[_steps.Count - 1 - i];
                input = await step.Apply(input);
            }
            return input;
        }

        public PipelineOutputObject RunPipeline(PipelineOutputObject output)
        {
            foreach (var step in _steps)
            {
                output = step.Apply(output);
            }
            return output;
        }
    }
}
