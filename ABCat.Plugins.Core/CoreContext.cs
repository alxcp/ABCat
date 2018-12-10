using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
            Context.I = this;
            ComponentFactory =
                new AbCatComponentFactory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plug-ins"));
            ComponentFactory.Init();
            _dbPluginCreatorAttribute = ComponentFactory.GetCreators<IDbContainer>().First();
            Logger = ComponentFactory.CreateActual<ILoggerFactory>().GetLogger("MainLog");
        }

        public static CoreContext I { get; } = new CoreContext();

        public event PropertyChangedEventHandler PropertyChanged;

        public ILog Logger { get; }

        public IComponentFactory ComponentFactory { get; }

        public IDbContainer CreateDbContainer(bool autoSave)
        {
            var result = _dbPluginCreatorAttribute.GetInstance<IDbContainer>();
            result.AutoSaveChanges = autoSave;
            return result;
        }

        public string AuthorDefaultName => "<Не задано>";

        public string GenreDefaultName => "<Не задано>";

        public IEventAggregatorShared EventAggregator { get; } = new EventAggregatorShared();

        public Encoding DefaultEncoding { get; } = Encoding.GetEncoding(1251);

        [NotifyPropertyChangedInvocator]
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}