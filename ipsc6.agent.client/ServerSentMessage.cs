using System.Text;

namespace ipsc6.agent.client
{
    public class ServerSentMessage
    {
        public MessageType Type { get; }
        public int N1 { get; }
        public int N2 { get; }
        public string S { get; }

        public ServerSentMessage(MessageType type, int n1, int n2, string s)
        {
            Type = type;
            N1 = n1;
            N2 = n2;
            S = s;
        }
        public ServerSentMessage(int type, int n1, int n2, string s)
        {
            Type = (MessageType)type;
            N1 = n1;
            N2 = n2;
            S = s;
        }

        public ServerSentMessage(network.AgentMessageReceivedEventArgs e, Encoding encoding = null)
        {
            Type = (MessageType)e.CommandType;
            N1 = e.N1;
            N2 = e.N2;
            /* UTF-8 转当前编码 */
            var utfBytes = (encoding ?? Encoding.Default).GetBytes(e.S);
            e.S = Encoding.UTF8.GetString(utfBytes, 0, utfBytes.Length);
            S = e.S;
        }

        public override string ToString()
        {
            return $"<{GetType().Name}@{GetHashCode():x8} Command={Type}, N1={N1}, N2={N2}, S=\"{S}\">";
        }
    }

}
