namespace Netcode.Runtime.Communication.Common.Logging
{
    public interface ILoggerFactory
    {
        public ILogger<T> CreateLogger<T>();
    }
}
