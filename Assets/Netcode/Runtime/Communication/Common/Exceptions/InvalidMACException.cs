using System;

namespace Netcode.Runtime.Communication.Common.Exceptions
{
    public class InvalidMACException : Exception
    {
        public InvalidMACException() : base() { }
        public InvalidMACException(string message) : base(message) { }
    }
}
