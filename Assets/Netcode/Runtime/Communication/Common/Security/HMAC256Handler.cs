using System.Security.Cryptography;

namespace Netcode.Runtime.Communication.Common.Security
{
    public class HMAC256Handler : IMACHandler
    {
        public bool IsConfigured { get; set; }

        public byte[] Key => _hmacSHA256.Key;

        private readonly HMACSHA256 _hmacSHA256;

        public HMAC256Handler() : this(GenerateKey()) { }

        private static byte[] GenerateKey()
        {
            // Generate HMAC Secret Key
            byte[] key = new byte[64];
            using RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(key);

            return key;
        }

        public HMAC256Handler(byte[] key)
        {
            _hmacSHA256 = new HMACSHA256(key);
        }

        public byte[] GenerateMAC(byte[] data)
        {
            return _hmacSHA256.ComputeHash(data);
        }
    }
}
