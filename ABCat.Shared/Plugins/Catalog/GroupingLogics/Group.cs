using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.ViewModels;

namespace ABCat.Shared.Plugins.Catalog.GroupingLogics
{
    /// <summary>
    ///     Группа записей каталога
    /// </summary>
    public class Group : ViewModelBase
    {
        public readonly HashSet<string> LinkedRecords = new HashSet<string>();

        /// <summary>Логика группировки, создавшая группу</summary>
        public readonly GroupingLogicPluginBase OwnerLogic;

        private bool _isExpanded;
        private bool _isSelected;
        private List<Group> _children = new List<Group>();

        /// <summary>
        ///     Группа записей каталога
        /// </summary>
        /// <param name="ownerLogic">Логика группировки, создавшая группу</param>
        public Group(GroupingLogicPluginBase ownerLogic)
        {
            OwnerLogic = ownerLogic;
        }

        /// <summary>
        ///     Название группы
        /// </summary>
        public string Caption { get; set; }

        /// <summary>
        ///     Список непосредственных потомков группы
        /// </summary>
        public IReadOnlyCollection<Group> Children => _children;

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
        public Group Parent { get; private set; }

        /// <summary>
        ///     Начать асинхронное получение записей
        /// </summary>
        /// <param name="dbContainer">Контейнер БД</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        public async Task<IEnumerable<IAudioBook>> GetRecords(IDbContainer dbContainer,
            CancellationToken cancellationToken)
        {
            return await OwnerLogic.GetRecords(dbContainer, this, cancellationToken);
        }

        public void Add(Group child)
        {
            _children.Add(child);
            child.Parent = this;
        }

        public void OrderByCaption()
        {
            _children.Sort(new ComparerByCaption());
        }

        public void OrderByLinkedRecordsQuantity()
        {
            _children.Sort(new ComparerByQuantity());
        }

        private class ComparerByCaption : IComparer<Group>
        {
            public int Compare(Group x, Group y)
            {
                return x.Caption.CompareTo(y.Caption);
            }
        }

        private class ComparerByQuantity : IComparer<Group>
        {
            public int Compare(Group x, Group y)
            {
                return y.LinkedRecords.Count.CompareTo(x.LinkedRecords.Count);
            }
        }
    }
}