using System;

namespace ipsc6.agent.client
{
    public delegate void ServerSentEventHandler(object sender, ServerSentEventArgs e);
    public delegate void ClosedEventHandler(object sender, EventArgs e);
    public delegate void LostEventHandler(object sender, EventArgs e);
    public delegate void ConnectionStateChangedEventHandler(object sender, ConnectionStateChangedEventArgs e);
    public delegate void ConnectionInfoStateChangedEventHandler(object sender, ConnectionInfoStateChangedEventArgs e);

    public delegate void AgentStateChangedEventHandler(object sender, AgentStateChangedEventArgs e);
    public delegate void TeleStateChangedEventHandler(object sender, TeleStateChangedEventArgs e);
    public delegate void QueueInfoEventHandler(object sender, QueueInfoEventArgs e);
    public delegate void HoldInfoEventHandler(object sender, HoldInfoEventArgs e);

    public delegate void AgentIdAssignedEventHandler(object sender, AgentIdAssignedEventArgs e);
    public delegate void AgentDisplayNameReceivedEventHandler(object sender, AgentDisplayNameReceivedEventArgs e);
    public delegate void ChannelAssignedEventHandler(object sender, ChannelAssignedEventArgs e);
    public delegate void WorkingChannelInfoReceivedEventHandler(object sender, WorkingChannelInfoReceivedEventArgs e);
    public delegate void RingInfoReceivedEventHandler(object sender, RingInfoReceivedEventArgs e);
    public delegate void IvrDataReceivedEventHandler(object sender, IvrDataReceivedEventArgs e);
    public delegate void CustomStringReceivedEventArgsReceivedEventHandler(object sender, CustomStringReceivedEventArgs e);
    public delegate void SipRegistrarListReceivedEventHandler(object sender, SipRegistrarListReceivedEventArgs e);
    public delegate void SipRegisterStateChangedEventHandler(object sender, EventArgs e);

    public class ServerSentEventArgs : EventArgs
    {
        public ServerSentMessage Message { get; }
        public ServerSentEventArgs(ServerSentMessage message) : base()
        {
            Message = message;
        }
    }

    public class StateChangedEventArgs<T> : EventArgs
    {
        public T OldState { get; }
        public T NewState { get; }
        public StateChangedEventArgs(T oldState, T newState) : base()
        {
            OldState = oldState;
            NewState = newState;
        }
    }

    public class ConnectionStateChangedEventArgs : StateChangedEventArgs<ConnectionState>
    {
        public ConnectionStateChangedEventArgs(ConnectionState oldState, ConnectionState newState) : base(oldState, newState) { }
    }

    public class ConnectionInfoStateChangedEventArgs : StateChangedEventArgs<ConnectionState>
    {
        public ConnectionInfo ConnectionInfo { get; }
        public ConnectionInfoStateChangedEventArgs(
            ConnectionInfo connectionInfo,
            ConnectionState oldState, ConnectionState newState
        ) : base(oldState, newState)
        {
            ConnectionInfo = connectionInfo;
        }
    }

    public class BaseConnectorEventArgs : EventArgs { }

    public class ConnectorConnectedEventArgs : BaseConnectorEventArgs { }

    public class ConnectorConnectAttemptFailedEventArgs : BaseConnectorEventArgs { }

    public class ConnectorDisconnectedEventArgs : BaseConnectorEventArgs { }

    public class ConnectorConnectionLostEventArgs : BaseConnectorEventArgs { }

    public class BaseCtiEventArgs<T> : EventArgs
    {
        public ConnectionInfo ConnectionInfo { get; }
        public T Value { get; }
        public BaseCtiEventArgs(ConnectionInfo connectionInfo, T value)
        {
            ConnectionInfo = connectionInfo;
            Value = value;
        }
    }

    public class AgentStateChangedEventArgs : StateChangedEventArgs<AgentStateWorkType>
    {
        public AgentStateChangedEventArgs(AgentStateWorkType oldState, AgentStateWorkType newState) : base(oldState, newState) { }
    }

    public class TeleStateChangedEventArgs : StateChangedEventArgs<TeleState>
    {
        public TeleStateChangedEventArgs(TeleState oldState, TeleState newState) : base(oldState, newState) { }
    }


    public class QueueInfoEventArgs : BaseCtiEventArgs<QueueInfo>
    {
        public QueueInfoEventArgs(ConnectionInfo connectionInfo, QueueInfo value) : base(connectionInfo, value) { }
    }

    public class HoldInfoEventArgs : BaseCtiEventArgs<CallInfo>
    {
        public HoldInfoEventArgs(ConnectionInfo connectionInfo, CallInfo value) : base(connectionInfo, value) { }
    }

    public struct AgentIdName
    {
        public int Id { get; }
        public string DisplayName { get; }
    }

    public class AgentIdAssignedEventArgs : BaseCtiEventArgs<AgentIdName>
    {
        public AgentIdAssignedEventArgs(ConnectionInfo connectionInfo, AgentIdName value) : base(connectionInfo, value) { }
    }

    public class AgentDisplayNameReceivedEventArgs : BaseCtiEventArgs<string>
    {
        public AgentDisplayNameReceivedEventArgs(ConnectionInfo connectionInfo, string value) : base(connectionInfo, value) { }
    }

    public class ChannelAssignedEventArgs : BaseCtiEventArgs<int>
    {
        public ChannelAssignedEventArgs(ConnectionInfo connectionInfo, int value) : base(connectionInfo, value) { }
    }

    public class WorkingChannelInfoReceivedEventArgs : BaseCtiEventArgs<WorkingChannelInfo>
    {
        public WorkingChannelInfoReceivedEventArgs(ConnectionInfo connectionInfo, WorkingChannelInfo value) : base(connectionInfo, value) { }
    }

    public class RingInfoReceivedEventArgs : BaseCtiEventArgs<CallInfo>
    {
        public RingInfoReceivedEventArgs(ConnectionInfo connectionInfo, CallInfo value) : base(connectionInfo, value) { }
    }

    public class IvrDataReceivedEventArgs : BaseCtiEventArgs<IvrData>
    {
        public IvrDataReceivedEventArgs(ConnectionInfo connectionInfo, IvrData value) : base(connectionInfo, value) { }
    }

    public class CustomStringReceivedEventArgs : BaseCtiEventArgs<ServerSentCustomString>
    {
        public CustomStringReceivedEventArgs(ConnectionInfo connectionInfo, ServerSentCustomString value) : base(connectionInfo, value) { }
    }

    public class SipRegistrarListReceivedEventArgs : EventArgs
    {
        public string[] Value { get; }
        public SipRegistrarListReceivedEventArgs(string[] value) : base()
        {
            Value = value;
        }
    }

}