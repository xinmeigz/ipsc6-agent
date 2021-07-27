using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Toolkit.Mvvm.Input;


namespace ipsc6.agent.wpfapp.ViewModels
{
    public class ConfigField<T>
    {
        public T Content { get; set; }

        public ConfigField() { }

        public ConfigField(T content)
        {
            Content = content;
        }
    }

    public class ConfigViewModel : Utils.SingletonObservableObject<ConfigViewModel>
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(LoginViewModel));

        private static Views.ConfigWindow window;

        private static IConfigurationRoot userSettings;

        private static config.Ipsc cfgIpsc;
        private static config.LocalWebServer cfgLocalWebServer;
        private static config.Phone cfgPhone;
        private static config.Startup cfgStartup;

        private static ObservableCollection<ConfigField<string>> ipscServerList = new();
        public ObservableCollection<ConfigField<string>> IpscServerList
        {
            get => ipscServerList;
            set => SetProperty(ref ipscServerList, value);
        }

        internal void Load(object sender)
        {
            window = sender as Views.ConfigWindow;

            // 重新加载!
            userSettings = ConfigManager.GetUserSettings();

            cfgIpsc = new();
            userSettings.GetSection(nameof(config.Ipsc)).Bind(cfgIpsc);
            ipscServerList.Clear();
            foreach (var addr in cfgIpsc.ServerList)
            {
                ipscServerList.Add(new ConfigField<string> { Content = addr });
            }

            cfgLocalWebServer = new();
            userSettings.GetSection(nameof(config.LocalWebServer)).Bind(cfgLocalWebServer);

            cfgPhone = new();
            userSettings.GetSection(nameof(config.Phone)).Bind(cfgPhone);

            cfgStartup = new();
            userSettings.GetSection(nameof(config.Startup)).Bind(cfgStartup);
        }

        private static readonly IRelayCommand newIpscServerCommand = new RelayCommand(DoNewIpscServer);
        public IRelayCommand NewIpscServerCommand => newIpscServerCommand;

        private static void DoNewIpscServer()
        {
            ipscServerList.Add(new ConfigField<string>());
        }

        private static readonly IRelayCommand delIpscServerCommand = new RelayCommand<object>(DoDelIpscServer);
        public IRelayCommand DelIpscServerCommand => delIpscServerCommand;

        private static void DoDelIpscServer(object item)
        {
            var val = item as ConfigField<string>;
            ipscServerList.Remove(val);
        }

        private static readonly IRelayCommand saveCommand = new RelayCommand(DoSave, CanSave);
        public IRelayCommand SaveCommand => saveCommand;
        private static void DoSave()
        {
            cfgIpsc.ServerList = (from m in ipscServerList select m.Content).ToList();

            Dictionary<string, object> d = new()
            {
                { nameof(config.Ipsc), cfgIpsc },
                { nameof(config.LocalWebServer), cfgLocalWebServer },
                { nameof(config.Phone), cfgPhone },
                { nameof(config.Startup), cfgStartup }
            };
            JsonSerializerOptions options = new() { WriteIndented = true };
            var s = JsonSerializer.Serialize(d, options);
            var data = new UTF8Encoding().GetBytes(s);
            using (var fileStream = File.Open(ConfigManager.UserSettingsPath, FileMode.Create))
            {
                fileStream.Write(data, 0, data.Length);
            }

            window.Close();
        }

        private static bool CanSave()
        {
            return true;
        }
    }
}