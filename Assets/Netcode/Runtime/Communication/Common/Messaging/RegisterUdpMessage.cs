namespace Netcode.Runtime.Communication.Common.Messaging
{
    public class RegisterUdpMessage : NetworkMessage
    {
        public uint ClientId { get; set; }
    }
}
