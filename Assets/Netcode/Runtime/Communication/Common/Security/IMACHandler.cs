namespace Netcode.Runtime.Communication.Common.Security
{
    public interface IMACHandler
    {
        bool IsConfigured { get; }

        byte[] GenerateMAC(byte[] data);
    }
}
