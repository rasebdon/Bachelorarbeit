using System.Security.Cryptography;
using System.Text;

namespace Netcode.Runtime.Communication.Common.Security
{
    internal class RSAEncryption : IAsymmetricEncryption
    {
        private readonly RSACryptoServiceProvider _rsa;
        public byte[] PublicKey { get; }

        internal RSAEncryption()
        {
            _rsa = new RSACryptoServiceProvider();

            string xmlString = _rsa.ToXmlString(false);
            PublicKey = Encoding.UTF8.GetBytes(xmlString);
        }

        public byte[] Decrypt(byte[] data)
        {
            return _rsa.Decrypt(data, false);
        }

        public byte[] Encrypt(byte[] data, byte[] publicKey)
        {
            string xmlKey = Encoding.UTF8.GetString(publicKey);
            using RSACryptoServiceProvider rsa = new();
            rsa.FromXmlString(xmlKey);

            return rsa.Encrypt(data, false);
        }

        public byte[] Encrypt(byte[] data)
        {
            return _rsa.Encrypt(data, false);
        }
    }
}
