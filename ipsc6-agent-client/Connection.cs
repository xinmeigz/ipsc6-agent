using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace ipsc6.agent.client
{
    public class Connection : IDisposable
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(Connection));

        public Connection(network.Connector connector, Encoding encoding = null)
        {
            this.connector = connector;
            Encoding = encoding ?? Encoding.Default;
            eventThread = new Thread(new ParameterizedThreadStart(EventThreadStarter));
            Initialize();
        }

        public Connection(ushort localPort = 0, string address = "", Encoding encoding = null)
        {
            connector = network.Connector.CreateInstance(localPort, address);
            Encoding = encoding ?? Encoding.Default;
            eventThread = new Thread(new ParameterizedThreadStart(EventThreadStarter));
            Initialize();
        }

        ~Connection()
        {
            Dispose(false);
        }

        #region IDisposable

        private bool disposedValue = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // 释放托管状态(托管对象)
                    logger.DebugFormat("{0} Dispose - Stop event thread", this);
                    eventThreadCancelSource.Cancel();
                    eventThread.Join();
                    eventThreadCancelSource.Dispose();
                }

                // 释放未托管的资源(未托管的对象)并重写终结器
                // 将大型字段设置为 null
                logger.DebugFormat("{0} Dispose - Deallocate connector", this);
                network.Connector.DeallocateInstance(connector);
                disposedValue = true;
            }
        }
        #endregion

        private void Initialize()
        {
            logger.InfoFormat("{0} Initialize", this);
            connector.OnConnectAttemptFailed += Connector_OnConnectAttemptFailed;
            connector.OnConnected += Connector_OnConnected;
            connector.OnDisconnected += Connector_OnDisconnected;
            connector.OnConnectionLost += Connector_OnConnectionLost;
            connector.OnAgentMessageReceived += Connector_OnAgentMessageReceived;
            eventThread.Start(eventThreadCancelSource.Token);
        }

        private readonly network.Connector connector;

        int agentId = -1;
        public int AgentId
        {
            get
            {
                if (state != ConnectionState.Ok)
                {
                    throw new InvalidOperationException($"Invalid state: {state}");
                }
                return agentId;
            }
        }

        private ConnectionState state = ConnectionState.Init;
        public ConnectionState State => state;
        private void SetState(ConnectionState newState)
        {
            var e = new ConnectionStateChangedEventArgs(state, newState);
            state = newState;
            Task.Run(() => OnConnectionStateChanged?.Invoke(this, e));
        }

        private TaskCompletionSource<object> connectTcs;
        private TaskCompletionSource<object> disconnectTcs;
        private TaskCompletionSource<int> logInTcs;
        private TaskCompletionSource<object> logOutTcs;
        private MessageType pendingReqType = MessageType.NONE;
        private TaskCompletionSource<ServerSentMessage> reqTcs;
        private readonly ConcurrentQueue<object> eventQueue = new ConcurrentQueue<object>();
        private readonly CancellationTokenSource eventThreadCancelSource = new CancellationTokenSource();
        private readonly Thread eventThread;

        string remoteHost;
        public string RemoteHost => remoteHost;
        ushort remotePort;
        public ushort RemotePort => remotePort;

        public bool Connected => connector.Connected;

        public event ServerSentEventHandler OnServerSentEvent;
        public event ClosedEventHandler OnClosed;
        public event LostEventHandler OnLost;
        public event ConnectionStateChangedEventHandler OnConnectionStateChanged;

        private void EventThreadStarter(object obj)
        {
            var token = (CancellationToken)obj;
            while (!token.IsCancellationRequested)
            {
                while (eventQueue.TryDequeue(out object msg))
                {
                    ProcessEventMessage(msg);
                }
                Thread.Sleep(50);
            }
        }

        private void ProcessEventMessage(object msg)
        {
            bool isResponse;  // 是否 Response ?
            if (msg is ConnectorConnectedEventArgs)
            {
                connectTcs.SetResult(msg);
            }
            else if (msg is ConnectorConnectAttemptFailedEventArgs)
            {
                lock (connectLock)
                {
                    SetState(ConnectionState.Failed);
                }
                connectTcs.SetException(new ConnectionFailedException());
            }
            else if ((msg is ConnectorDisconnectedEventArgs) || (msg is ConnectorConnectionLostEventArgs))
            {
                ConnectionState prevState;
                lock (connectLock)
                {
                    prevState = state;
                    if (msg is ConnectorDisconnectedEventArgs)
                    {
                        SetState(ConnectionState.Closed);
                    }
                    else
                    {
                        SetState(ConnectionState.Lost);
                    }
                }

                if (msg is ConnectorDisconnectedEventArgs)
                {
                    OnClosed?.Invoke(this, new EventArgs());
                }
                else
                {
                    OnLost?.Invoke(this, new EventArgs());
                }
                if (prevState == ConnectionState.Closing)
                {
                    disconnectTcs.SetResult(null);
                }
                else if (prevState == ConnectionState.Opening)
                {
                    if (msg is ConnectorDisconnectedEventArgs)
                    {
                        try
                        {
                            connectTcs.SetException(new ConnectionClosedException());
                        }
                        catch (InvalidOperationException)
                        {
                            logInTcs.SetException(new ConnectionClosedException());
                        }
                    }
                    else if (msg is ConnectorConnectionLostEventArgs)
                    {
                        try
                        {
                            connectTcs.SetException(new ConnecttionLostException());
                        }
                        catch (InvalidOperationException)
                        {
                            logInTcs.SetException(new ConnecttionLostException());
                        }
                    }
                }
            }
            else if (msg is ServerSentMessage)
            {
                var msg_ = msg as ServerSentMessage;

                if (msg_.Type == MessageType.REMOTE_MSG_LOGIN)
                {
                    if (msg_.N1 < 1)
                    {
                        var err = new ErrorResponse(msg_);
                        logger.ErrorFormat("Login failed: {0}. Connector will be closed.", err);
                        logInTcs.SetException(new ErrorResponse(msg_));
                        connector.Disconnect();
                        // 登录失败算一种连接失败
                        lock (connectLock)
                        {
                            SetState(ConnectionState.Failed);
                        }
                    }
                    else
                    {
                        lock (connectLock)
                        {
                            agentId = msg_.N2;
                            SetState(ConnectionState.Ok);
                        }
                        logInTcs.SetResult(msg_.N2);
                    }
                }
                else if (msg_.Type == MessageType.REMOTE_MSG_RELEASE)
                {
                    if (msg_.N1 < 1)
                    {
                        // 注销失败
                        ConnectionState prevState;
                        lock (connectLock)
                        {
                            prevState = state;
                            SetState(ConnectionState.Ok);
                        }
                        logOutTcs.SetException(new ErrorResponse(msg_));
                    }
                    else
                    {
                        // 注销成功实际上并不会发生，因为服务器会直接断开
                        logOutTcs.SetResult(null);
                    }
                }
                else
                {
                    lock (requestLock)
                    {
                        isResponse = pendingReqType == msg_.Type;
                    }
                    if (isResponse)
                    {
                        if (msg_.N1 < 1)
                        {
                            var err = new ErrorResponse(msg_);
                            logger.ErrorFormat("{0}", err);
                            reqTcs.SetException(new ErrorResponse(msg_));
                        }
                        else
                        {
                            reqTcs.SetResult(msg_);
                        }
                    }
                    else
                    {
                        /// server->client event
                        OnServerSentEvent?.Invoke(this, new ServerSentEventArgs(msg_));
                    }
                }
            }
        }

        private void Connector_OnAgentMessageReceived(object sender, network.AgentMessageReceivedEventArgs e)
        {
            var data = new ServerSentMessage(e, Encoding);
            logger.DebugFormat("{0} AgentMessageReceived: {1}", this, data);
            eventQueue.Enqueue(data);
        }

        private void Connector_OnConnectionLost(object sender, EventArgs e)
        {
            logger.ErrorFormat("{0} OnConnectionLost", this);
            eventQueue.Enqueue(new ConnectorConnectionLostEventArgs());
        }

        private void Connector_OnDisconnected(object sender, EventArgs e)
        {
            logger.WarnFormat("{0} OnDisconnected", this);
            eventQueue.Enqueue(new ConnectorDisconnectedEventArgs());
        }

        private void Connector_OnConnected(object sender, network.ConnectedEventArgs e)
        {
            logger.InfoFormat("{0} OnConnected", this);
            eventQueue.Enqueue(new ConnectorConnectedEventArgs());
        }

        private void Connector_OnConnectAttemptFailed(object sender, EventArgs e)
        {
            logger.ErrorFormat("{0} OnConnectAttemptFailed", this);
            eventQueue.Enqueue(new ConnectorConnectAttemptFailedEventArgs());
        }

        public Encoding Encoding { get; }

        private readonly object connectLock = new object();

        public void Send(AgentRequestMessage value)
        {
            logger.DebugFormat("{0} Send {1}", this, value);
            connector.SendAgentMessage((int)value.Type, value.N, value.S);
        }

        public const int DefaultRequestTimeoutMilliseconds = 5000;
        public const int DefaultKeepAliveTimeoutMilliseconds = 5000;

        public async Task<int> Open(string remoteHost, ushort remotePort, string workerNumber, string password, uint keepAliveTimeout = DefaultKeepAliveTimeoutMilliseconds, int flag = 0)
        {
            ConnectionState[] allowStates = { ConnectionState.Init, ConnectionState.Closed, ConnectionState.Failed, ConnectionState.Lost };
            lock (connectLock)
            {
                if (allowStates.Any(p => p == State))
                {
                    SetState(ConnectionState.Opening);
                }
                else
                {
                    throw new InvalidOperationException($"Invalid state: {State}");
                }
            }
            logger.InfoFormat("{0} connect \"{1}|{2}\", flag={3} ...", this, remoteHost, remotePort, flag);
            connectTcs = new TaskCompletionSource<object>();
            if (remotePort > 0)
            {
                connector.Connect(remoteHost, remotePort, keepAliveTimeout);
            }
            else
            {
                connector.Connect(remoteHost);
            }
            logger.DebugFormat("{0} connect request was sent", this);
            this.remoteHost = remoteHost;
            this.remotePort = remotePort;
            logger.DebugFormat("{0} await connect >>>", this);
            await connectTcs.Task;
            logger.DebugFormat("{0} await connect <<<", this);
            ///
            /// 登录
            logger.DebugFormat("{0} Log-in \"{1}\" ... ", this, workerNumber);
            var cst = new CancellationTokenSource();
            var reqData = new AgentRequestMessage(MessageType.REMOTE_MSG_LOGIN, flag, $"{workerNumber}|{password}|1|0|{workerNumber}");
            logInTcs = new TaskCompletionSource<int>();
            var timeoutTask = Task.Delay((int)keepAliveTimeout * 3, cst.Token);
            Send(reqData);
            var task = await Task.WhenAny(logInTcs.Task, timeoutTask);
            if (task == timeoutTask)
            {
                logger.ErrorFormat("{0} Log-in timeout", this, workerNumber);
                lock (connectLock)
                {
                    logger.DebugFormat("{0} Log-in timeout : ForceClose", this);
                    disconnectTcs = new TaskCompletionSource<object>();
                    SetState(ConnectionState.Closing);
                    connector.Disconnect(true);
                }
                var cst2 = new CancellationTokenSource();
                var timeoutTask2 = Task.Delay((int)keepAliveTimeout, cst2.Token);
                var task2 = Task.WhenAny(disconnectTcs.Task, timeoutTask2);
                if (task2 != timeoutTask2)
                {
                    cst2.Cancel();
                    logger.DebugFormat("{0} Log-in timeout : ForceClose Timeout", this);
                }
                logger.DebugFormat("{0} Log-in timeout : ForceClose Ok", this);
                throw new ConnectionTimeoutException();
            }
            else
            {
                cst.Cancel();
                var agentid = await logInTcs.Task;
                logger.InfoFormat("{0} Log-in \"{1}\" Succeed, AgentID={2}", this, workerNumber, agentid);
                return agentid;
            }
        }

        public async Task<int> Open(string remoteHost, string workerNumber, string password, uint keepAliveTimeout = DefaultKeepAliveTimeoutMilliseconds, int flag = 0)
        {
            return await Open(remoteHost, 0, workerNumber, password, keepAliveTimeout, flag);
        }

        public async Task Close(bool graceful = true, int requestTimeout = DefaultRequestTimeoutMilliseconds, int flag = 0)
        {
            ConnectionState[] closedStates = { ConnectionState.Closed, ConnectionState.Failed, ConnectionState.Lost };
            ConnectionState[] allowedStates = { ConnectionState.Opening, ConnectionState.Ok };
            lock (connectLock)
            {
                if (closedStates.Any(m => m == State))
                {
                    logger.WarnFormat("{0} Close(graceful) ... Already closed.", this);
                    return;
                }
                if (!allowedStates.Any(m => m == State))
                {
                    throw new InvalidOperationException($"Invalid state: {State}");
                }
                SetState(ConnectionState.Closing);
                disconnectTcs = new TaskCompletionSource<object>();
            }

            if (graceful)
            {
                logger.InfoFormat("{0} Close(graceful) ...", this);
                var cst = new CancellationTokenSource();
                logOutTcs = new TaskCompletionSource<object>();
                var timeoutTask = Task.Delay(requestTimeout, cst.Token);
                Send(new AgentRequestMessage(MessageType.REMOTE_MSG_RELEASE, flag));
                var task = await Task.WhenAny(logOutTcs.Task, disconnectTcs.Task, timeoutTask);
                if (task == timeoutTask)
                {
                    throw new DisconnectionTimeoutException();
                }
                cst.Cancel();
                await task;
            }
            else
            {
                var cst = new CancellationTokenSource();
                var timeoutTask = Task.Delay(requestTimeout, cst.Token);
                logger.InfoFormat("{0} Close(force) ...", this);
                connector.Disconnect();
                var task = await Task.WhenAny(disconnectTcs.Task, timeoutTask);
                if (task == timeoutTask)
                {
                    throw new ConnectionTimeoutException();
                }
                cst.Cancel();
                await task;
            }
        }

        private static readonly object requestLock = new object();

        public bool HasPendingRequest => pendingReqType != MessageType.NONE;

        public async Task<ServerSentMessage> Request(AgentRequestMessage args, int millisecondsTimeout = DefaultRequestTimeoutMilliseconds)
        {
            lock (connectLock)
            {
                if (state != ConnectionState.Ok)
                {
                    throw new InvalidOperationException($"Can not send a request when state is {state}");
                }
            }
            lock (requestLock)
            {
                if (pendingReqType != MessageType.NONE)
                {
                    throw new RequestNotCompleteError($"A pending request exists: {pendingReqType}");
                }
                pendingReqType = args.Type;
            }
            try
            {
                logger.DebugFormat("{0} Request({1})", this, args);
                reqTcs = new TaskCompletionSource<ServerSentMessage>();
                connector.SendAgentMessage((int)args.Type, args.N, args.S);
                var cst = new CancellationTokenSource();
                var task = await Task.WhenAny(reqTcs.Task, Task.Delay(millisecondsTimeout, cst.Token));
                if (task != reqTcs.Task)
                {
                    throw new RequestTimeoutError();
                }
                cst.Cancel();
                return await reqTcs.Task;
            }
            finally
            {
                lock (requestLock)
                {
                    pendingReqType = MessageType.NONE;
                }
            }
        }

        public override string ToString()
        {
            return $"<{GetType().Name} Local={connector.BoundAddress}, Remote={RemoteHost}|{RemotePort}, State={State}, PhysicalConnected={connector.Connected}>";
        }

    }
}
