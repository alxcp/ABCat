using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using ABCat.Shared;
using ABCat.Shared.Plugins.DataProviders;
using Component.Infrastructure.Factory;
using JetBrains.Annotations;
using Shared.Everywhere;

namespace ABCat.Core
{
    internal sealed class CoreContext : IContext
    {
        private readonly ComponentCreatorBase _dbPluginCreatorAttribute;

        public CoreContext()
        {
            SharedContext.I = this;
            ComponentFactory =
                new AbCatComponentFactory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plug-ins"));
            try
            {
                ComponentFactory.Init();
            }
            catch (Exception e)
            {
                var ex = new Exception("An error occurred on initializing ComponentFactory.", e);
                MessageBox.Show(ex.CollectExceptionDetails());
                throw ex;
            }

            _dbPluginCreatorAttribute = ComponentFactory.GetCreators<IDbContainer>().First();
            LoggerFactory = ComponentFactory.CreateActual<ILoggerFactory>();
            LoggerFactory.Init();
            MainLog = LoggerFactory.GetLogger("MainLog");
            DbContainer = CreateDbContainer(true);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public ILog MainLog { get; }

        public IComponentFactory ComponentFactory { get; }

        public ILoggerFactory LoggerFactory { get; }

        private IDbContainer CreateDbContainer(bool autoSave)
        {
            var result = _dbPluginCreatorAttribute.GetInstance<IDbContainer>();
            result.AutoSaveChanges = autoSave;
            return result;
        }

        public IDbContainer DbContainer { get; }

        public DbContainerAutoSave DbContainerAutoSave => new DbContainerAutoSave(DbContainer);

        public string AppDataFolderPath { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ABCat");

        public string GetAppDataFolderPath(string subfolderName)
        {
            return Path.Combine(AppDataFolderPath, subfolderName);
        }

        public IEventAggregatorShared EventAggregator { get; } = new EventAggregatorShared();

        public Encoding DefaultEncoding { get; } = Encoding.GetEncoding(1251);

        [NotifyPropertyChangedInvocator]
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}