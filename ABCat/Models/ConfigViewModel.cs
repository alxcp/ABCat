using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ABCat.Shared;
using ABCat.Shared.ViewModels;
using ABCat.UI.WPF.UI;
using Component.Infrastructure;

namespace ABCat.UI.WPF.Models
{
    public sealed class ConfigViewModel : ViewModelBase
    {
        private Config _currentConfig;
        private Config _selectedItem;

        public ConfigViewModel()
        {
            var mainConfig = Config.Load<MainConfig>();
            mainConfig.CheckAndFix();
            mainConfig.PropertyChanged += Config_PropertyChanged;
            Configs.Add(mainConfig);

            foreach (var configCreatorAttribute in Context.I.ComponentFactory.GetConfigAttributes())
            {
                var config = Config.Load(configCreatorAttribute);
                config.PropertyChanged += Config_PropertyChanged;
                config.CheckAndFix();
                Configs.Add(config);
            }

            SelectedItem = mainConfig;
        }

        public ObservableCollection<Config> Configs { get; } = new ObservableCollection<Config>();

        public Config CurrentConfig
        {
            get => _currentConfig;
            set
            {
                if (Equals(value, _currentConfig)) return;
                _currentConfig = value;
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand => CommandFactory.Get(() =>
        {
            foreach (var pluginConfig in Configs)
            {
                pluginConfig.CheckAndFix();
                pluginConfig.Save();
            }

            var targetWindow = TargetWindow;
            targetWindow?.Close();
        }, () => Configs.Any(item => item.IsChanged));

        public Config SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (Equals(value, _selectedItem)) return;
                _selectedItem = value;
                OnPropertyChanged();
            }
        }

        public Window TargetWindow { get; set; }

        private void Config_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            CommandFactory.UpdateAll();
        }

        public static void ShowConfigWindow(Config targetConfigs)
        {
            var configWindow = new ConfigEditorWindow();
            var cvm = new ConfigViewModel();
            configWindow.DataContext = cvm;
            cvm.TargetWindow = configWindow;
            configWindow.ShowDialog();
        }
    }
}