using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ipsc6.agent.wpfapp")]
namespace ipsc6.agent.services
{
#pragma warning disable VSTHRD200
    public class Service
    {

        #region Demo methods
        public string Echo(string s)
        {
            OnEchoTriggered?.Invoke(this, new EchoTriggeredEventArgs() { S = s });
            return s;
        }
        public void Throw() => throw new Exception();

        public async Task<string> DelayEcho(string s, int milliseconds)
        {
            OnEchoTriggered?.Invoke(this, new EchoTriggeredEventArgs() { S = s });
            await Task.Delay(milliseconds);
            return s;
        }

        public event EventHandler OnEchoTriggered;
        #endregion

        #region 内部方法

        internal client.Agent agent;

        internal Models.Model Model = new();

        internal void Destroy()
        {
            if (agent == null) return;
            agent.Dispose();
            agent = null;
            client.Agent.Release();
        }

        internal void Create(IEnumerable<string> addresses, ushort localPort, string localAddress)
        {
            if (agent != null) throw new InvalidOperationException();
            client.Agent.Initial();
            agent = new client.Agent(addresses, localPort, localAddress);
            agent.OnAgentStateChanged += Agent_OnAgentStateChanged;
            agent.OnAgentDisplayNameReceived += Agent_OnAgentDisplayNameReceived;
            agent.OnGroupReceived += Agent_OnGroupReceived;
            agent.OnSignedGroupsChanged += Agent_OnSignedGroupsChanged;
        }
        #endregion

        #region status

        public event EventHandler OnLoginCompleted;

        private void Agent_OnAgentDisplayNameReceived(object sender, client.AgentDisplayNameReceivedEventArgs e)
        {
            Model.DisplayName = agent.DisplayName;
            Model.WorkerNumber = agent.WorkerNumber;
            OnLoginCompleted?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<StatusChangedEventArgs> OnStatusChanged;

        private void Agent_OnAgentStateChanged(object sender, client.AgentStateChangedEventArgs e)
        {
            var evt = new StatusChangedEventArgs()
            {
                OldState = e.OldState.AgentState,
                OldWorkType = e.OldState.WorkType,
                NewState = e.NewState.AgentState,
                NewWorkType = e.NewState.WorkType,
            };
            OnStatusChanged?.Invoke(this, evt);
        }

        internal async Task LogInAsync(string workerNumber, string password)
        {
            await agent.StartUpAsync(workerNumber, password);
        }

        public async Task SetBusy(client.WorkType workType = client.WorkType.PauseBusy)
        {
            await agent.SetBusyAsync(workType);
        }

        public async Task SetIdle()
        {
            await agent.SetIdleAsync();
        }

        public Models.Model GetAgentFull()
        {
            return Model;
        }

        #endregion

        #region AgentGroup

        public event EventHandler OnSignedGroupsChanged;

        private void Agent_OnSignedGroupsChanged(object sender, EventArgs e)
        {
            ReloadGroups();
        }

        private void ReloadGroups()
        {
            Model.Groups = (
                from x in agent.Groups
                select new Models.Group() { Id = x.Id, Name = x.Name, IsSigned = x.IsSigned }
            ).ToList();
            OnSignedGroupsChanged?.Invoke(this, new EventArgs());
        }

        private void Agent_OnGroupReceived(object sender, EventArgs e)
        {
            ReloadGroups();
        }

        public async Task SignGroups(bool isSignIn = true)
        {
            if (isSignIn)
            {
                await agent.SignInAsync();
            }
            else
            {
                await agent.SignOutAsync();
            }
        }

        public async Task SignGroup(string id, bool isSignIn = true)
        {
            if (isSignIn)
            {
                await agent.SignInAsync(id);
            }
            else
            {
                await agent.SignOutAsync(id);
            }
        }

        public async Task SignGroups(IEnumerable<string> ids, bool isSignIn = true)
        {
            if (isSignIn)
            {
                await agent.SignInAsync(ids);
            }
            else
            {
                await agent.SignOutAsync(ids);
            }
        }

        public IEnumerable<Models.Group> GetGroups()
        {
            return Model.Groups.ToList();
        }

        #endregion
    }
#pragma warning restore VSTHRD200
}
