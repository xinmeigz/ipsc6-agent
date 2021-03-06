using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.Toolkit.Mvvm.Input;

#pragma warning disable VSTHRD100

namespace ipsc6.agent.wpfapp.ViewModels
{
    public class LoginViewModel : Utils.SingletonObservableObject<LoginViewModel>
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(LoginViewModel));

        private static string workerNum;
        public string WorkerNum
        {
            get => workerNum;
            set
            {
                if (SetProperty(ref workerNum, value))
                {
                    Application.Current.Dispatcher.Invoke(LoginCommand.NotifyCanExecuteChanged);
                }
            }
        }

        private static string password;
        public string Password
        {
            get => password;
            set
            {
                if (SetProperty(ref password, value))
                {
                    Application.Current.Dispatcher.Invoke(LoginCommand.NotifyCanExecuteChanged);
                }
            }
        }

        private static bool isAllowInput = true;
        public bool IsAllowInput
        {
            get => isAllowInput;
            set
            {
                if (!SetProperty(ref isAllowInput, value)) return;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var cmd in new IRelayCommand[] { loginCommand, showConfigWindowCommand, closeCommand })
                    {
                        cmd.NotifyCanExecuteChanged();
                    }
                });
            }
        }

        private static readonly IRelayCommand loginCommand = new AsyncRelayCommand<object>(DoLoginAsync, CanLogin);
        public IRelayCommand LoginCommand => loginCommand;

        private static Window GetOrCreateWindow()
        {
            var window = Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x is Views.LoginWindow);
            return window ?? new Views.LoginWindow();
        }

        private static Window GetSingleWindow()
        {
            var window = Application.Current.Windows.OfType<Window>().Single(x => x is Views.LoginWindow);
            return window;
        }

        public static async Task DoLoginAsync(object parameter)
        {
            IEnumerable<string> serverList = Array.Empty<string>();
            if (parameter != null)
            {
                var realParam = parameter as Tuple<string, string, IEnumerable<string>>;
                Instance.WorkerNum = realParam.Item1;
                password = realParam.Item2;
                serverList = realParam.Item3;
            }

            var dispatcher = Application.Current.Dispatcher;
            var svc = MainViewModel.Instance.MainService;

            using (await Utils.CommandGuard.EnterAsync(loginCommand))
            {
                var window = GetOrCreateWindow() as Views.LoginWindow;
#pragma warning disable CS4014 // ?????????????????????????????????????????????????????????????????????????????????
                dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        window.ShowDialog();
                    }
                    catch (InvalidOperationException) { }
                });
#pragma warning restore CS4014 // ?????????????????????????????????????????????????????????????????????????????????

                try
                {
                    await ExecuteLoginAsync(serverList);
                    window.DialogResult = true;
                    window.Close();
                }
                catch (client.ConnectionException err)
                {
                    logger.ErrorFormat("DoLogin - CTI?????????????????????: {0}", err);
                    string errMsg = err switch
                    {
                        client.ConnectionFailedException =>
                            "??????????????? CTI ????????????\r\n????????????????????????",
                        client.ConnectionTimeoutException =>
                            "?????????????????????\r\n????????????????????????",
                        client.ConnecttionLostException =>
                            "?????????????????????\r\n????????????????????????",
                        client.ConnectionClosedException =>
                            "CTI ???????????????????????????????????????????????????????????????????????????????????????\r\n??????????????????????????????????????????",
                        _ => err.ToString(),
                    };
                    MessageBox.Show(
                        Application.Current.MainWindow,
                        $"CTI ?????????????????????\r\n\r\n{errMsg}",
                        $"{Application.Current.MainWindow.Title} - {window.Title}",
                        MessageBoxButton.OK, MessageBoxImage.Warning
                    );
                }
                catch (client.BaseRequestError err)
                {
                    logger.ErrorFormat("DoLogin - ????????????: {0}", err);
                    string errMsg = err switch
                    {
                        client.ErrorResponse =>
                            $"CTI ????????????????????????????????????: {err.Message}",
                        client.RequestTimeoutError =>
                            "CTI ???????????????????????????",
                        client.RequestNotCompleteError =>
                            "?????????????????????????????????",
                        _ => err.ToString(),
                    };
                    MessageBox.Show(
                        Application.Current.MainWindow,
                        $"????????????\r\n\r\n{errMsg}",
                        $"{Application.Current.MainWindow.Title} - {window.Title}",
                        MessageBoxButton.OK, MessageBoxImage.Warning
                    );
                }
            }
        }

        internal static bool CanLogin(object _)
        {
            if (string.IsNullOrEmpty(workerNum) || string.IsNullOrEmpty(password))
                return false;
            if (Utils.CommandGuard.IsGuarding)
                return false;
            var svc = MainViewModel.Instance.MainService;
            if (svc.GetAgentRunningState() != client.AgentRunningState.Stopped)
                return false;
            return true;
        }

        private static async Task ExecuteLoginAsync(IEnumerable<string> serverList = null)
        {
            var svc = MainViewModel.Instance.MainService;
            var mainViewModel = MainViewModel.Instance;

            if (svc.GetAgentRunningState() != client.AgentRunningState.Stopped)
            {
                throw new InvalidOperationException($"??????????????? {svc.GetAgentRunningState()} ????????????????????????");
            }

            serverList ??= (new string[] { });
            if (serverList.Count() == 0)
            {
                mainViewModel.ReloadConfigure();
                serverList = mainViewModel.cfgIpsc.ServerList;
            }

            logger.Info("ExecuteLoginAsync - ????????????...");
            Instance.IsAllowInput = false;
            try
            {
                await svc.LogInAsync(workerNum, password, serverList);
                logger.Info("ExecuteLoginAsync - ????????????!");
            }
            finally
            {
                password = "";
                Instance.IsAllowInput = true;
            }

            mainViewModel.StartTimer();
        }

        private static readonly IRelayCommand closeCommand = new RelayCommand(DoClose, CanClose);
        public IRelayCommand CloseCommand => closeCommand;

        private static void DoClose()
        {
            var window = GetSingleWindow();
            window.DialogResult = false;
            window.Close();
        }

        private static bool CanClose()
        {
            return isAllowInput;
        }

        private static readonly IRelayCommand showConfigWindowCommand = new RelayCommand(DoShowConfigWindow, CanShowConfigWindow);
        public IRelayCommand ShowConfigWindowCommand => showConfigWindowCommand;

        private static void DoShowConfigWindow()
        {
            new Views.ConfigWindow().ShowDialog();
        }

        private static bool CanShowConfigWindow()
        {
            return isAllowInput;
        }

    }

}

#pragma warning restore VSTHRD100
