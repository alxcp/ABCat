using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Component.Infrastructure;

namespace ABCat.Shared.Plugins.Catalog.GroupingLogics
{
    public interface IGroupingLogicPlugin : IExtComponent, INotifyPropertyChanged
    {
        /// <summary>
        ///     В данный момент выполняется асинхронная операция
        /// </summary>
        bool IsOnUpdate { get; }

        /// <summary>
        ///     Название логики группировки
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Начать асинхронную генерацию дерева групп
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции</param>
        Task<Group> GenerateGroups(CancellationToken cancellationToken);

        ///// <summary>
        /////     Начать асинхронное получение записей, включенных в группу
        ///// </summary>
        ///// <param name="dbContainer">Контейнер БД</param>
        ///// <param name="group">Группа записей</param>
        ///// <param name="cancellationToken">Токен отмены операции</param>
        //Task<IEnumerable<IAudioBook>> GetRecords(IDbContainer dbContainer, Group group,
        //    CancellationToken cancellationToken);
    }
}