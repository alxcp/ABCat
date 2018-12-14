using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.ViewModels;
using JetBrains.Annotations;

namespace ABCat.Shared.Plugins.Catalog.GrouppingLogics
{
    /// <summary>
    ///     Группа записей каталога
    /// </summary>
    public class Group : ViewModelBase
    {
        public readonly HashSet<string> LinkedRecords = new HashSet<string>();

        /// <summary>Логика группировки, создавшая группу</summary>
        public readonly GrouppingLogicPluginBase OwnerLogic;

        private bool _isExpanded;
        private bool _isSelected;

        /// <summary>
        ///     Группа записей каталога
        /// </summary>
        /// <param name="ownerLogic">Логика группировки, создавшая группу</param>
        public Group(GrouppingLogicPluginBase ownerLogic)
        {
            OwnerLogic = ownerLogic;
            Children = new List<Group>();
        }

        /// <summary>
        ///     Название группы
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        ///     Список непосредственных потомков группы
        /// </summary>
        public List<Group> Children { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }

                // Expand all the way up to the root.
                if (_isExpanded && Parent != null)
                    Parent.IsExpanded = true;
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        ///     Уровень иерархии в дереве
        /// </summary>
        public int Level { get; set; }

        public string LinkedObjectString { get; set; }

        /// <summary>
        ///     Родительская группа
        /// </summary>
        public Group Parent { get; set; }

        /// <summary>
        ///     Начать асинхронное получение записей
        /// </summary>
        /// <param name="dbContainer">Контейнер БД</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        public async Task<IEnumerable<IAudioBook>> BeginGetRecordsAsync(IDbContainer dbContainer,
            CancellationToken cancellationToken)
        {
            return await OwnerLogic.BeginGetRecordsAsync(dbContainer, this, cancellationToken);
        }
    }
}