using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ABCat.Shared;
using ABCat.Shared.Commands;
using ABCat.Shared.Plugins.Catalog.FilteringLogics;
using ABCat.Shared.Plugins.Catalog.GrouppingLogics;
using ABCat.Shared.Plugins.DataProviders;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.Plugins.Sites;
using ABCat.Shared.Plugins.UI;
using ABCat.Shared.ViewModels;
using JetBrains.Annotations;

namespace ABCat.UI.WPF.Models
{
    public sealed class AbCatViewModel : ViewModelBase, IDisposable
    {
        private IBrowserWindowPlugin _browserWindowPlugin;
        private IDbContainer _dbContainer4Ui;
        private IFilteringLogicPlugin _filter;
        private CancellationTokenSource _getRecordsCancellationTokenSource;
        private GrouppingLogicViewModel _grouppingLogicModel;

        private string _previousFileName;
        private IRecordsListPlugin _recordsListUc;

        public AbCatViewModel()
        {
            RecordsListUc =
                Context.I.ComponentFactory.CreateActual<IRecordsListPlugin>();
            RecordsListUc.Data = new List<IAudioBook>();
            StatusBarStateModel = new StatusBarStateViewModel(IsCanCancelAsyncOperation, CancelAsyncOperation);
            SiteParserModel = new SiteParserViewModel(this,
                Context.I.ComponentFactory.CreateActual<ISiteParserPlugin>(), () => { return SelectedItems; });
            RecordTargetDownloaderModel = new RecordTargetDownloaderViewModel(StatusBarStateModel,
                Context.I.ComponentFactory.CreateActual<IRecordTargetDownloaderPlugin>(), () => SelectedItems,
                () => Filter.BeginUpdateCacheAsync(UpdateTypes.Loaded, ex =>
                {
                    if (ex != null) throw ex;
                }));
            Filter =
                Context.I.ComponentFactory.CreateActual<IFilteringLogicPlugin>();
            Filter.PropertyChanged += FilterPropertyChanged;
            Filter.BeginUpdateCacheAsync(UpdateTypes.Hidden | UpdateTypes.Loaded | UpdateTypes.Values,
                ex => { });
            NormalizationSettingsEditorModel = new NormalizationSettingsEditorViewModel();
            GrouppingLogicModel =
                new GrouppingLogicViewModel(
                    Context.I.ComponentFactory.GetCreators<IGrouppingLogicPlugin>()
                        .Select(item => item.GetInstance<IGrouppingLogicPlugin>()), async group =>
                    {
                        try
                        {
                            var dbContainer = Context.I.CreateDbContainer(false);
                            if (_getRecordsCancellationTokenSource != null)
                                _getRecordsCancellationTokenSource.Cancel();
                            _getRecordsCancellationTokenSource = new CancellationTokenSource();
                            var records = await GetCurrentRecordsAsync(dbContainer, group, Filter,
                                _getRecordsCancellationTokenSource.Token);
                            SetCurrentRecords(dbContainer, records, _getRecordsCancellationTokenSource.Token);
                            OnPropertyChanged();
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    });

            HideSelectedRecordsCommand = new DelegateCommand(HideSelectedRecordsCommandExecute,
                HideSelectedRecordsCommandCanExecute);
            RefreshCommand = new DelegateCommand(parameter => RefreshRecordsListData());
            SetReplacementCommand = new DelegateCommand(SetReplacement, IsCanSetReplacement);
            ShowCachedInBrowserCommand = new DelegateCommand(ShowCachedInBrowserCommandExecute,
                ShowCachedInBrowserCommandCanExecute);
            ConfigCommand = new DelegateCommand(parameter => ConfigViewModel.ShowConfigWindow(null));
        }

        public ICommand OpenOriginalUrlCommand =>
            CommandFactory.Get(() =>
            {
                var selectedItem = SelectedItems.FirstOrDefault();
                if (selectedItem != null)
                {
                    var url = selectedItem.GetRecordPageUrl();
                    Process.Start(url.AbsoluteUri);
                }
            }, () => SelectedItems.AnySafe());

        public IBrowserWindowPlugin BrowserWindowPlugin
        {
            get
            {
                if (_browserWindowPlugin == null)
                {
                    _browserWindowPlugin =
                        Context.I.ComponentFactory.CreateActual<IBrowserWindowPlugin>();
                    _browserWindowPlugin.WindowClosed += BrowserWindowPluginWindowPluginClosed;
                }

                return _browserWindowPlugin;
            }
        }

        public DelegateCommand ConfigCommand { get; }

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

        public GrouppingLogicViewModel GrouppingLogicModel
        {
            get => _grouppingLogicModel;
            set
            {
                if (Equals(value, _grouppingLogicModel)) return;
                _grouppingLogicModel = value;
                OnPropertyChanged();
            }
        }

        [UsedImplicitly] public DelegateCommand HideSelectedRecordsCommand { get; }


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
                OnPropertyChanged("SelectedItems");
            }
        }

        public RecordTargetDownloaderViewModel RecordTargetDownloaderModel { get; }

        [UsedImplicitly] public DelegateCommand RefreshCommand { get; }

        public IEnumerable<IAudioBook> SelectedItems => RecordsListUc.SelectedItems;

        [UsedImplicitly] public DelegateCommand SetReplacementCommand { get; }

        [UsedImplicitly] public DelegateCommand ShowCachedInBrowserCommand { get; }

        public SiteParserViewModel SiteParserModel { get; }
        public StatusBarStateViewModel StatusBarStateModel { get; }

        public void Dispose()
        {
            _browserWindowPlugin?.Dispose();
            _dbContainer4Ui?.Dispose();
            _filter?.Dispose();
            _getRecordsCancellationTokenSource?.Dispose();
            _recordsListUc?.Dispose();
            NormalizationSettingsEditorModel?.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void BrowserWindowPluginWindowPluginClosed(object sender, EventArgs e)
        {
            _browserWindowPlugin.Dispose();
            _browserWindowPlugin = null;
        }

        public void CancelAsyncOperation(object parameter)
        {
            RecordTargetDownloaderModel.CancelAsyncOperation();
            SiteParserModel.CancelAsyncOperation();
        }

        public bool IsCanCancelAsyncOperation(object parameter)
        {
            return true;
        }

        public async void RefreshRecordsListData()
        {
            try
            {
                var dbContainer = Context.I.CreateDbContainer(false);
                var records = await GetCurrentRecordsAsync(dbContainer, GrouppingLogicModel.SelectedGroup, Filter,
                    _getRecordsCancellationTokenSource.Token);
                SetCurrentRecords(dbContainer, records, _getRecordsCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void ShowInBrowser([NotNull] IAudioBook record)
        {
            SiteParserModel.SiteParserPlugin.BeginDownloadRecordSourcePageAsync(record, (pageHtml, ex) =>
            {
                Application.Current.Dispatcher.CheckAccess(() =>
                {
                    ShowInDefaultWebBrowser(pageHtml);
                    using (var dbContainer = Context.I.CreateDbContainer(true))
                    {
                        record.OpenCounter++;
                        dbContainer.AudioBookSet.AddChangedRecords(record);
                    }
                });
            }, CancellationToken.None);
        }

        private void ShowInDefaultWebBrowser(string pageHtml)
        {
            if (!string.IsNullOrEmpty(_previousFileName) && File.Exists(_previousFileName))
                File.Delete(_previousFileName);
            _previousFileName = null;

            pageHtml = "<head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"></head>" +
                       pageHtml;
            _previousFileName = Path.GetTempFileName() + ".html";
            File.WriteAllText(_previousFileName, pageHtml);
            Process.Start(_previousFileName);
        }

        private static async Task<IEnumerable<IAudioBook>> GetCurrentRecordsAsync(
            IDbContainer dbContainer,
            Group currentGroup,
            IFilteringLogicPlugin filteringLogicPlugin,
            CancellationToken cancellationToken)
        {
            if (currentGroup == null) return null;

            cancellationToken.ThrowIfCancellationRequested();

            var grouppedRecords = await currentGroup.BeginGetRecordsAsync(dbContainer, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (filteringLogicPlugin.IsEnabled)
            {
                var result = await filteringLogicPlugin.Filter(grouppedRecords, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return result;
            }

            return grouppedRecords;
        }

        private async void FilterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == "IsOnUpdate") return;
                _getRecordsCancellationTokenSource?.Cancel();
                _getRecordsCancellationTokenSource = new CancellationTokenSource();

                var dbContainer = Context.I.CreateDbContainer(false);
                var records =
                    await
                        GetCurrentRecordsAsync(dbContainer, GrouppingLogicModel.SelectedGroup, Filter,
                            _getRecordsCancellationTokenSource.Token);
                _getRecordsCancellationTokenSource.Token.ThrowIfCancellationRequested();
                SetCurrentRecords(dbContainer, records, _getRecordsCancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private bool HideSelectedRecordsCommandCanExecute()
        {
            return SelectedItems.AnySafe();
        }

        private void HideSelectedRecordsCommandExecute()
        {
            var selected = SelectedItems.ToDictionary(item => item.GroupKey + "\\" + item.Key, item => item);

            using (var dbContainer = Context.I.CreateDbContainer(true))
            {
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

                //existedHidden.AddRange(dbContainer.HiddenRecordSet.GetHiddenRecords(group.Key, new HashSet<string>(group.Select(item => item.Key))));
                //var existedHidden = dbContainer.HiddenRecordSet.GetHiddenRecords(item => selected.Keys.Contains(item.RecordGroupKey + "\\" + item.RecordKey)).ToDictionary(item => item.RecordGroupKey + "\\" + item.RecordKey, item => item);

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
                Filter.BeginUpdateCacheAsync(UpdateTypes.Hidden, ex => { });
            }
        }

        private bool IsCanSetReplacement(object arg)
        {
            return SelectedItems.AnySafe();
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SetCurrentRecords(IDbContainer dbContainer, IEnumerable<IAudioBook> records,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                if (_recordsListUc.Dispatcher.CheckAccess())
                {
                    records = records.ToArray();
                    _recordsListUc.Data = records;
                    if (_dbContainer4Ui != null) _dbContainer4Ui.Dispose();
                    _dbContainer4Ui = dbContainer;
                }
                else
                {
                    _recordsListUc.Dispatcher.Invoke(() => SetCurrentRecords(dbContainer, records, cancellationToken));
                }
            }
        }

        private void SetReplacement(object obj)
        {
            NormalizationSettingsEditorModel.NormalizationSettingsEditorPlugin.TargetRecordsForEdit =
                SelectedItems.ToArray();
            NormalizationSettingsEditorModel.IsActive = true;
        }

        private bool ShowCachedInBrowserCommandCanExecute(object parameter)
        {
            return SelectedItems.AnySafe();
        }

        private void ShowCachedInBrowserCommandExecute(object parameter)
        {
            var first = SelectedItems.FirstOrDefault();
            if (first != null)
            {
                ShowInBrowser(first);
            }
        }

        private void RecordsListUcItemDoubleClick(object sender, ItemDoubleClickRowEventArgs e)
        {
            if (e.Target != null) ShowInBrowser(e.Target);
        }
    }
}