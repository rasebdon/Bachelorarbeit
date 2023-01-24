using Netcode.Runtime.Communication.Common.Logging;

namespace Netcode.Runtime.Integration
{
    public class UnityLoggerFactory : ILoggerFactory
    {
        private readonly LogLevel _logLevel = LogLevel.Error;

        public UnityLoggerFactory(LogLevel logLevel)
        {
            _logLevel = logLevel;
        }

        public ILogger<T> CreateLogger<T>()
        {
            return new UnityLogger<T>(_logLevel);
        }
    }
}
