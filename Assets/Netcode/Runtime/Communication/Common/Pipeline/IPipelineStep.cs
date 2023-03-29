using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Common.Pipeline
{
    public interface IPipelineStep
    {
        public PipelineOutputObject Apply(PipelineOutputObject output);
        public Task<PipelineInputObject> Apply(PipelineInputObject input);
    }
}
