using Netcode.Runtime.Communication.Common.Messaging;
using System.IO;

namespace Netcode.Runtime.Communication.Common.Pipeline
{
    public struct PipelineOutputObject
    {
        public MemoryStream OutputData;
        public NetworkMessage[] Messages;
    }

    public struct PipelineInputObject
    {
        public MemoryStream InputData;
        public NetworkMessage[] Messages;
    }
}
