namespace Netcode.Runtime.Communication.Common.Security
{
    public interface IAsymmetricEncryption
    {
        byte[] PublicKey { get; }

        byte[] Encrypt(byte[] data);
        byte[] Encrypt(byte[] data, byte[] publicKey);
        
        byte[] Decrypt(byte[] data);
    }
}
