namespace Netcode.Runtime.Communication.Common.Security
{
    public interface IEncryption
    {
        bool IsConfigured { get; set; }
        byte[] Key { get; }
        byte[] IV { get; }

        byte[] Decrypt(byte[] data);
        byte[] Encrypt(byte[] data);
    }
}
