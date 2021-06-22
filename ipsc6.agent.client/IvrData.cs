namespace ipsc6.agent.client
{
    public class IvrData : ServerSideData
    {
        public int N { get; }
        public string S { get; }
        public IvrData(CtiServer connectionInfo, int n, string s) : base(connectionInfo)
        {
            N = n;
            S = s;
        }
    }
}
