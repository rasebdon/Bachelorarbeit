using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netcode.Runtime.Communication.Common.Logging
{
    public interface ILogger<T>
    {
        public void LogDetail(object message);
        public void LogInfo(object message);
        public void LogWarning(object message);
        public void LogError(object message);
        public void LogFatal(object message);
    }
}
