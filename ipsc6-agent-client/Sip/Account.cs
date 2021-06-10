using System;
using System.Collections.Generic;

namespace ipsc6.agent.client.Sip
{
    public class Account : org.pjsip.pjsua2.Account
    {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(Account));
        public int ConnectionIndex { get; }

        public Account(int connectionIndex) : base()
        {
            ConnectionIndex = connectionIndex;
            MakeString();
        }

        ~Account()
        {
            shutdown();
        }

        public event RegisterStateChangedEventHandler OnRegisterStateChanged;
        public event IncomingCallEventHandler OnIncomingCall;
        public event CallDisconnectedEventHandler OnCallDisconnected;

        string _string;
        string MakeString()
        {
            if (isValid())
            {
                var info = getInfo();
                if (info.regIsConfigured)
                {
                    _string = $"<{GetType().Name}@{GetHashCode():x8} Id={info.id}, Uri={info.uri}, RegisterStatus={info.regStatus}>";
                }
                else
                {
                    _string = $"<{GetType().Name}@{GetHashCode():x8} Id={info.id}>";
                }
            }
            else
            {
                _string = $"<{GetType().Name}@{GetHashCode():x8}>";
            }
            return _string;
        }
        public override void onRegState(org.pjsip.pjsua2.OnRegStateParam param)
        {
            logger.DebugFormat("RegState - {0} : {1}", getInfo().uri, param.code);
            MakeString();
            OnRegisterStateChanged?.Invoke(this, new EventArgs());
        }

        private HashSet<Call> calls = new HashSet<Call>();
        public IReadOnlyCollection<Call> Calls => calls;

        public override void onIncomingCall(org.pjsip.pjsua2.OnIncomingCallParam param)
        {
            var call = new Call(this, param.callId);
            if (!calls.Add(call)) throw new InvalidOperationException();
            call.OnCallDisconnected += Call_OnCallDisconnected;
            logger.DebugFormat("IncomingCall - {0} : {1}", this, call);
            OnIncomingCall?.Invoke(this, new IncomingCallEventArgs(call));
        }

        private void Call_OnCallDisconnected(object sender, EventArgs e)
        {
            var call = sender as Call;
            if (!calls.Remove(call)) throw new InvalidOperationException();
            logger.DebugFormat("CallDisconnected - {0} : {1}", this, call);
            OnCallDisconnected?.Invoke(call, new EventArgs());
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(_string) ? base.ToString() : _string;
        }

    }
}
