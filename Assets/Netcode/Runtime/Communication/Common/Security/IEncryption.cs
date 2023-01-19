namespace Netcode.Runtime.Communication.Common.Security
{
    public interface IEncryption
    {
        bool IsConfigured { get; }

        byte[] Decrypt(byte[] data);
        byte[] Encrypt(byte[] data);
    }
}
