using System.IO;
using System.Security.Cryptography;

namespace Netcode.Runtime.Communication.Common.Security
{
    public class AES256Encryption : IEncryption
    {
        public bool IsConfigured { get; set; }
        public byte[] Key => _aes.Key;
        public byte[] IV => _aes.IV;

        private readonly AesManaged _aes;
        private readonly ICryptoTransform _encryptor;
        private readonly ICryptoTransform _decryptor;

        public AES256Encryption() : this(GenerateIV(), GenerateKey())
        {
        }

        public AES256Encryption(byte[] iv, byte[] key)
        {
            _aes = new()
            {
                IV = iv,
                Key = key,

                Padding = PaddingMode.PKCS7,
                Mode = CipherMode.CBC
            };

            _encryptor = _aes.CreateEncryptor();
            _decryptor = _aes.CreateDecryptor();
        }

        public byte[] Decrypt(byte[] data)
        {
            // Create the streams used for decryption.
            using MemoryStream msDecrypt = new(data);
            using CryptoStream csDecrypt = new(msDecrypt, _decryptor, CryptoStreamMode.Read);
            using MemoryStream decrypted = new();
            csDecrypt.CopyTo(decrypted);

            return decrypted.ToArray();
        }

        public byte[] Encrypt(byte[] data)
        {
            // Create the streams used for encryption.
            using MemoryStream msEncrypt = new();
            using CryptoStream csEncrypt = new(msEncrypt, _encryptor, CryptoStreamMode.Write);

            // Write data and return encrypted array
            csEncrypt.Write(data, 0, data.Length);
            csEncrypt.FlushFinalBlock();

            return msEncrypt.ToArray();
        }

        private static byte[] GenerateIV()
        {
            byte[] iv = new byte[16];
            using RNGCryptoServiceProvider rng = new();
            rng.GetBytes(iv);

            return iv;
        }
        private static byte[] GenerateKey()
        {
            byte[] key = new byte[32];
            using RNGCryptoServiceProvider rng = new();
            rng.GetBytes(key);

            return key;
        }
    }
}
