namespace Netcode.Runtime.Communication.Common.Security
{
    public interface IMACHandler
    {
        bool IsConfigured { get; set; }
        byte[] Key { get; }
        
        byte[] GenerateMAC(byte[] data);
    }
}
