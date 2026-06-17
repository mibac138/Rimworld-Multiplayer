using System;

namespace Multiplayer.Common
{
    public class PacketReadException : Exception
    {
        public PacketReadException(string msg) : base(msg)
        {
        }

        public PacketReadException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class PacketBadIdException(int id) : PacketReadException($"Bad packet id: {id}");
}
