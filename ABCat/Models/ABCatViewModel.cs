using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ABCat.Shared;
using ABCat.Shared.Messages;
using ABCat.Shared.Plugins.Catalog.FilteringLogics;
using ABCat.Shared.Plugins.Catalog.GroupingLogics;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.Plugins.Sites;
using ABCat.Shared.Plugins.UI;
using ABCat.Shared.ViewModels;
using Caliburn.Micro;
using JetBrains.Annotations;

namespace ABCat.UI.WPF.Models
{
    public sealed class AbCatViewModel : ViewModelBase, IDisposable, IHandle<RecordLoadedMessage>
    {
        private IFilteringLogicPlugin _filter;
        private CancellationTokenSource _getRecordsCancellationTokenSource;
        private GroupingLogicViewModel _groupingLogicModel;

        private string _previousFileName;
        private IRecordsListPlugin _recordsListUc;

        public AbCatViewModel()
        {
            RecordsListUc =
                Context.I.ComponentFactory.CreateActual<IRecordsListPlugin>();
            RecordsListUc.Data = new List<IAudioBook>();
            StatusBarStateModel = new StatusBarStateViewModel(IsCanCancelAsyncOperation, CancelAsyncOperation);

            SiteParserModel = new WebSiteParserViewModel(this, () => SelectedItems);

            RecordTargetDownloaderModel = new RecordTargetDownloaderViewModel(StatusBarStateModel,
                Context.I.ComponentFactory.CreateActual<IRecordTargetDownloaderPlugin>(), () => SelectedItems);
            Filter =
                Context.I.ComponentFactory.CreateActual<IFilteringLogicPlugin>();
            Filter.PropertyChanged += FilterPropertyChanged;
            Filter.UpdateCache(UpdateTypes.Hidden | UpdateTypes.Loaded | UpdateTypes.Values);
            NormalizationSettingsEditorModel = new NormalizationSettingsEditorViewModel();
            GroupingLogicModel =
                new GroupingLogicViewModel(
                    Context.I.ComponentFactory.GetCreators<IGroupingLogicPlugin>()
                        .Select(item => item.GetInstance<IGroupingLogicPlugin>()), async group =>
                    {
                        try
                        {
                            _getRecordsCancellationTokenSource?.Cancel();
                            _getRecordsCancellationTokenSource = new CancellationTokenSource();
                            var records = await GetCurrentRecords(group, Filter,
                                _getRecordsCancellationTokenSource.Token);
                            SetCurrentRecords(records, _getRecordsCancellationTokenSource.Token);
                            OnPropertyChanged();
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    });

        }

        public ICommand OpenOriginalUrlCommand =>
            CommandFactory.Get(() =>
            {
                var selectedItem = SelectedItems.FirstOrDefault();
                if (selectedItem != null)
                {
                    var url = SiteParserModel.GetRecordPageUrl(selectedItem);
                    if (url != null)
                        Process.Start(url.AbsoluteUri);
                }
            }, () => SelectedItems.AnySafe());

        public ICommand ConfigCommand => CommandFactory.Get(()=> ConfigViewModel.ShowConfigWindow(null));

        public IFilteringLogicPlugin Filter
        {
            get => _filter;
            private set
            {
                if (Equals(value, _filter)) return;
                _filter = value;
                OnPropertyChanged();
            }
        }

        public GroupingLogicViewModel GroupingLogicModel
        {
            get => _groupingLogicModel;
            set
            {
                if (Equals(value, _groupingLogicModel)) return;
                _groupingLogicModel = value;
                OnPropertyChanged();
            }
        }

        [UsedImplicitly]
        public ICommand HideSelectedRecordsCommand =>
            CommandFactory.Get(async ()=> await HideSelectedRecordsCommandExecute(), HideSelectedRecordsCommandCanExecute);

        public NormalizationSettingsEditorViewModel NormalizationSettingsEditorModel { get; }

        public IRecordsListPlugin RecordsListUc
        {
            get => _recordsListUc;
            private set
            {
                if (Equals(value, _recordsListUc)) return;
                if (_recordsListUc != null) _recordsListUc.ItemDoubleClick -= RecordsListUcItemDoubleClick;
                _recordsListUc = value;
                if (_recordsListUc != null) _recordsListUc.ItemDoubleClick += RecordsListUcItemDoubleClick;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedItems));
            }
        }

        public RecordTargetDownloaderViewModel RecordTargetDownloaderModel { get; }

        [UsedImplicitly] public ICommand RefreshCommand => CommandFactory.Get(RefreshRecordsListData);

        public IReadOnlyCollection<IAudioBook> SelectedItems => RecordsListUc.SelectedItems;

        [UsedImplicitly]
        public ICommand SetReplacementCommand => CommandFactory.Get(SetReplacement, IsCanSetReplacement);

        [UsedImplicitly]
        public ICommand ShowCachedInBrowserCommand =>
            CommandFactory.Get(async ()=> await ShowCachedInBrowserCommandExecute(), ShowCachedInBrowserCommandCanExecute);

        public WebSiteParserViewModel SiteParserModel { get; }
        public StatusBarStateViewModel StatusBarStateModel { get; }

        public void Dispose()
        {
            _filter?.Dispose();
            _getRecordsCancellationTokenSource?.Dispose();
            _recordsListUc?.Dispose();
            NormalizationSettingsEditorModel?.Dispose();
        }

        public void CancelAsyncOperation()
        {
            RecordTargetDownloaderModel.CancelAsyncOperation();
            SiteParserModel.CancelAsyncOperation();
        }

        public bool IsCanCancelAsyncOperation()
        {
            return true;
        }

        public async void RefreshRecordsListData()
        {
            try
            {
                var records = await GetCurrentRecords(GroupingLogicModel.SelectedGroup, Filter,
                    _getRecordsCancellationTokenSource.Token);
                SetCurrentRecords(records, _getRecordsCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public async Task ShowInBrowser([NotNull] IAudioBook record)
        {
            var pageHtml =
                await SiteParserModel.DownloadRecordSourcePage(record,
                    CancellationToken.None);

            Application.Current.Dispatcher.CheckAccess(() =>
            {
                ShowInDefaultWebBrowser(pageHtml);
                var dbContainer = Context.I.DbContainer;
                record.OpenCounter++;
                dbContainer.AudioBookSet.AddChangedRecords(record);
            });
        }

        private void ShowInDefaultWebBrowser(string pageHtml)
        {
            if (!string.IsNullOrEmpty(_previousFileName) && File.Exists(_previousFileName))
                File.Delete(_previousFileName);
            _previousFileName = null;

            _previousFileName = Path.GetTempFileName() + ".html";
            File.WriteAllText(_previousFileName, pageHtml);
            Process.Start(_previousFileName);
        }

        private async Task<IEnumerable<IAudioBook>> GetCurrentRecords(
            Group currentGroup,
            IFilteringLogicPlugin filteringLogicPlugin,
            CancellationToken cancellationToken)
        {
            if (currentGroup == null) return null;

            cancellationToken.ThrowIfCancellationRequested();

            var groupedRecords = await currentGroup.GetRecords(Context.I.DbContainer, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (filteringLogicPlugin.IsEnabled)
            {
                var result = await filteringLogicPlugin.Filter(groupedRecords, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }

            return groupedRecords;
        }

        private Timer _updateTimer;

        private void FilterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsOnUpdate") return;

            _updateTimer?.Dispose();
            _updateTimer = new Timer(FilterRecordsAsync, null, 500, Timeout.Infinite);
        }

        private void FilterRecordsAsync(object o)
        {
            try
            {
                _getRecordsCancellationTokenSource?.Cancel();
                _getRecordsCancellationTokenSource = new CancellationTokenSource();

                var records = GetCurrentRecords(GroupingLogicModel.SelectedGroup, Filter,
                    _getRecordsCancellationTokenSource.Token).Result;
                _getRecordsCancellationTokenSource.Token.ThrowIfCancellationRequested();
                SetCurrentRecords(records, _getRecordsCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (AggregateException ex)
            {
                if (!ex.Flatten().InnerExceptions.OfType<TaskCanceledException>().Any())
                    throw;
            }
        }

        private bool HideSelectedRecordsCommandCanExecute()
        {
            return SelectedItems.AnySafe();
        }

        private async Task HideSelectedRecordsCommandExecute()
        {
            var selected = SelectedItems.ToDictionary(item => item.GroupKey + "\\" + item.Key, item => item);

            using (var autoSave = Context.I.DbContainerAutoSave)
            {
                var dbContainer = autoSave.DBContainer;
                var groups = selected.Values.GroupBy(item => item.GroupKey);

                var existedHidden = new Dictionary<string, IHiddenRecord>();

                foreach (var group in groups)
                {
                    foreach (
                        var audioBook in
                        dbContainer.HiddenRecordSet.GetHiddenRecords(group.Key,
                            new HashSet<string>(group.Select(item => item.Key))))
                    {
                        existedHidden.Add(audioBook.RecordGroupKey + "\\" + audioBook.RecordKey, audioBook);
                    }
                }

                var hiddenRecords = new List<IHiddenRecord>();

                foreach (var selectedItem in selected)
                {
                    if (!existedHidden.TryGetValue(selectedItem.Key, out _))
                    {
                        var hiddenRecord = dbContainer.HiddenRecordSet.CreateHiddenRecord();
                        hiddenRecord.RecordGroupKey = selectedItem.Value.GroupKey;
                        hiddenRecord.RecordKey = selectedItem.Value.Key;
                        hiddenRecords.Add(hiddenRecord);
                    }
                }

                dbContainer.HiddenRecordSet.AddHiddenRecord(hiddenRecords.ToArray());

                dbContainer.SaveChanges();
                await Filter.UpdateCache(UpdateTypes.Hidden);
            }
        }

        private bool IsCanSetReplacement()
        {
            return SelectedItems.AnySafe();
        }

        private void SetCurrentRecords(IEnumerable<IAudioBook> records, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                if (_recordsListUc.Dispatcher.CheckAccess())
                {
                    records = records.ToArray();
                    _recordsListUc.Data = records;
                }
                else
                {
                    _recordsListUc.Dispatcher.Invoke(() => SetCurrentRecords(records, cancellationToken));
                }
            }
        }

        private void SetReplacement()
        {
            NormalizationSettingsEditorModel.NormalizationSettingsEditorPlugin.TargetRecordsForEdit =
                SelectedItems.ToArray();
            NormalizationSettingsEditorModel.IsActive = true;
        }

        private bool ShowCachedInBrowserCommandCanExecute()
        {
            return SelectedItems.AnySafe();
        }

        private async Task ShowCachedInBrowserCommandExecute()
        {
            var first = SelectedItems.FirstOrDefault();
            if (first != null)
            {
                await ShowInBrowser(first);
            }
        }

        private void RecordsListUcItemDoubleClick(object sender, ItemDoubleClickRowEventArgs e)
        {
            if (e.Target != null)
            {
                ShowInBrowser(e.Target).ContinueWith(task => { });
            }
        }

        public void Handle(RecordLoadedMessage message)
        {
            Filter.UpdateCache(UpdateTypes.Loaded);
        }
    }
}