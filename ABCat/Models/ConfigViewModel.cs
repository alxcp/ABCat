using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using ABCat.Shared;
using ABCat.UI.WPF.UI;
using Component.Infrastructure;
using JetBrains.Annotations;

namespace ABCat.UI.WPF.Models
{
    public sealed class ConfigViewModel : INotifyPropertyChanged
    {
        private Config _currentConfig;
        private Config _selectedItem;

        public ConfigViewModel()
        {
            SaveCommand = new DelegateCommand(obj =>
            {
                foreach (var pluginConfig in Configs)
                {
                    pluginConfig.Check(true);
                    pluginConfig.Save();
                }

                var targetWindow = TargetWindow;
                targetWindow?.Close();
            }, obj => Configs.Any(item => item.IsChanged));

            var mainConfig = Config.Load<MainConfig>();
            mainConfig.Check(false);
            Configs.Add(mainConfig);

            foreach (var configCreatorAttribute in Context.I.ComponentFactory.GetConfigAttributes())
            {
                var config = Config.Load(configCreatorAttribute);
                config.Check(false);
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

        public DelegateCommand SaveCommand { get; }

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

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler Saved;

        public static void ShowConfigWindow(Config targetConfigs)
        {
            var configWindow = new ConfigEditorWindow();
            var cvm = new ConfigViewModel();
            configWindow.DataContext = cvm;
            cvm.TargetWindow = configWindow;
            cvm.Saved += CvmSaved;
            configWindow.ShowDialog();
        }

        private static void CvmSaved(object sender, EventArgs e)
        {
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}