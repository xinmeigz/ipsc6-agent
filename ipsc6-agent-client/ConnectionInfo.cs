using System;
using System.Collections.Generic;
using System.Text;

namespace ipsc6.agent.client
{
    public class ConnectionInfo: IEquatable<ConnectionInfo>
    {
        public readonly string Host;
        public readonly ushort Port;

        public ConnectionInfo(string host, ushort port = 0)
        {
            Host = host;
            Port = port;
        }

        public bool Equals(ConnectionInfo other)
        {
            return Host == other.Host && Port == other.Port;
        }

        public override string ToString()
        {
            return string.Format(
                "<{0} at 0x{1:x8} Host={2}, Port={3}>",
                GetType().Name, GetHashCode(), Host, Port);
        }
    }
}