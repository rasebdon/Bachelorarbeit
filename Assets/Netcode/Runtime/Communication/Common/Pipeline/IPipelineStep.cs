using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Common.Pipeline
{
    public interface IPipelineStep
    {
        public Task<PipelineOutputObject> Apply(PipelineOutputObject output);
        public Task<PipelineInputObject> Apply(PipelineInputObject input);
    }
}
