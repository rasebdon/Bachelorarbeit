namespace Netcode.Runtime.Communication.Common.Messaging
{
    public class RegisterUdpMessage : NetworkMessage
    {
        public RegisterUdpMessage(uint clientId)
        {
            ClientId = clientId;
        }

        public uint ClientId { get; set; }
    }
}
