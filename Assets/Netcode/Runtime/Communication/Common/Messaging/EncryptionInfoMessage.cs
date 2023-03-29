namespace Netcode.Runtime.Communication.Common.Messaging
{
    internal class EncryptionInfoMessage : NetworkMessage
    {
        public byte[] SymmetricIV { get; set; }
        public byte[] SymmetricKey { get; set; }
        public byte[] MACKey { get; set; }

        public EncryptionInfoMessage(byte[] symmetricIV, byte[] symmetricKey, byte[] macKey)
        {
            SymmetricIV = symmetricIV;
            SymmetricKey = symmetricKey;
            MACKey = macKey;
        }
    }
}