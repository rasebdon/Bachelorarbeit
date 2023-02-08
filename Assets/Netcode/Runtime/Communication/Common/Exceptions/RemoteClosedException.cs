using System;

namespace Netcode.Runtime.Communication.Common.Exceptions
{
    public class RemoteClosedException : Exception
    {
        public RemoteClosedException() : base() { }
        public RemoteClosedException(string message) : base(message) { }
    }
}
