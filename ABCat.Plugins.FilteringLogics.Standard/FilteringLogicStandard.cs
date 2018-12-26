using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using ABCat.Shared;
using ABCat.Shared.Plugins.Catalog.FilteringLogics;
using ABCat.Shared.Plugins.Catalog.ParsingLogics;
using ABCat.Shared.Plugins.DataSets;
using Component.Infrastructure;
using Component.Infrastructure.Factory;
using JetBrains.Annotations;

namespace ABCat.Plugins.FilteringLogics.Standard
{
    [SingletoneComponentInfo("1.0")]
    public class FilteringLogicStandard : FilteringLogicPluginBase
    {
        private static readonly INaturalTimeSpanParserPlugin TimeSpanParser;

        static FilteringLogicStandard()
        {
            TimeSpanParser = Context.I.ComponentFactory.CreateActual<INaturalTimeSpanParserPlugin>();
        }

        private readonly ConcurrentDictionary<string, ObservableCollection<string>> _filterValuesCache =
            new ConcurrentDictionary<string, ObservableCollection<string>>();

        private IEnumerable<IAudioBook> _booksCache;
        private Dictionary<string, bool> _hiddenCache;
        private Dictionary<string, bool> _loadedCache;

        [UsedImplicitly]
        public FilteringLogicStandard()
        {
            RegisterFilteredComboboxesSource();
        }

        public override bool IsEmpty
        {
            get
            {
                var fields = FilterFields;
                return fields == null || fields.IsEmpty;
            }
        }

        [Category("Произведение")]
        [DisplayName("Автор")]
        [Description("Автор произведения")]
        [Editor(typeof(FilteredComboBoxEditor), typeof(FilteredComboBoxEditor))]
        public string Author
        {
            get => FilterFields.GetValue("Author");
            set
            {
                FilterFields.SetValue("Author", value);
                OnPropertyChanged();
            }
        }

        [Category("Аудиокнига")]
        [DisplayName("Битрейт")]
        [Description("Битрейт (качество аудикниги)")]
        [Editor(typeof(FilteredComboBoxEditor), typeof(FilteredComboBoxEditor))]
        public string Bitrate
        {
            get => FilterFields.GetValue("Bitrate");
            set
            {
                FilterFields.SetValue("Bitrate", value);
                OnPropertyChanged();
            }
        }

        [Category("Произведение")]
        [DisplayName("Описание")]
        [Description("Описание произведения")]
        [Editor(typeof(FilteredComboBoxEditor), typeof(FilteredComboBoxEditor))]
        public string Description
        {
            get => FilterFields.GetValue("Description");
            set
            {
                FilterFields.SetValue("Description", value);
                OnPropertyChanged();
            }
        }

        [Category("Произведение")]
        [DisplayName("Жанр")]
        [Description("Жанр произведения")]
        [Editor(typeof(FilteredComboBoxEditor), typeof(FilteredComboBoxEditor))]
        public string Genre
        {
            get => FilterFields.GetValue("Genre");
            set
            {
                FilterFields.SetValue("Genre", value);
                OnPropertyChanged();
            }
        }

        [Category("Каталог")]
        [DisplayName("Скрыто")]
        [Description("Было скрыто из списков")]
        public bool IsHidden
        {
            get => FilterFields.GetValue("IsHidden") == "1";
            set
            {
                if (value)
                    FilterFields.SetValue("IsHidden", "1");
                else
                    FilterFields.ClearValue("IsHidden");

                OnPropertyChanged();
            }
        }

        [Category("Каталог")]
        [DisplayName("Загружено")]
        [Description("Было загружено")]
        public bool IsLoaded
        {
            get => FilterFields.GetValue("IsLoaded") == "1";
            set
            {
                if (value)
                    FilterFields.SetValue("IsLoaded", "1");
                else
                    FilterFields.ClearValue("IsLoaded");

                OnPropertyChanged();
            }
        }

        [Category("Аудиокнига")]
        [DisplayName("Длительность")]
        [Description("Длительность воспроизведения")]
        [Editor(typeof(FilteredComboBoxEditor), typeof(FilteredComboBoxEditor))]
        public string ParsedLength
        {
            get => FilterFields.GetValue("ParsedLength");
            set
            {
                FilterFields.SetValue("ParsedLength", value);
                OnPropertyChanged();
            }
        }

        [Category("Аудиокнига")]
        [DisplayName("Издатель")]
        [Description("Издатель произведения")]
        [Editor(typeof(FilteredComboBoxEditor), typeof(FilteredComboBoxEditor))]
        public string Publisher
        {
            get => FilterFields.GetValue("Publisher");
            set
            {
                FilterFields.SetValue("Publisher", value);
                OnPropertyChanged();
            }
        }

        [Category("Аудиокнига")]
        [DisplayName("Исполнитель")]
        [Description("Исполнитель произведения")]
        [Editor(typeof(FilteredComboBoxEditor), typeof(FilteredComboBoxEditor))]
        public string Reader
        {
            get => FilterFields.GetValue("Reader");
            set
            {
                FilterFields.SetValue("Reader", value);
                OnPropertyChanged();
            }
        }

        [Category("Произведение")]
        [DisplayName("Название")]
        [Description("Назавание произведения")]
        [Editor(typeof(FilteredComboBoxEditor), typeof(FilteredComboBoxEditor))]
        public string Title
        {
            get => FilterFields.GetValue("Title");
            set
            {
                FilterFields.SetValue("Title", value);
                OnPropertyChanged();
            }
        }


        public override bool CheckForConfig(bool correct, out Config incorrectConfig)
        {
            incorrectConfig = null;
            return true;
        }

        public override bool FilterRecord(IAudioBook audioBook)
        {
            var fields = FilterFields;
            if (fields == null)
            {
                throw new Exception("Filter is not initialized");
            }

            var result = (string.IsNullOrEmpty(Author) || Filter(Author, audioBook.Author))
                         && (string.IsNullOrEmpty(Genre) || Filter(Genre, audioBook.Genre))
                         && (string.IsNullOrEmpty(Bitrate) || Filter(Bitrate, audioBook.Bitrate))
                         && (string.IsNullOrEmpty(Description) || Filter(Description, audioBook.Description))
                         && (string.IsNullOrEmpty(ParsedLength) || FilterTimeSpan(ParsedLength, audioBook.ParsedLength))
                         && (string.IsNullOrEmpty(Publisher) || Filter(Publisher, audioBook.Publisher))
                         && (string.IsNullOrEmpty(Reader) || Filter(Reader, audioBook.Reader))
                         && (string.IsNullOrEmpty(Title) || Filter(Title, audioBook.Title));

            if (result)
            {
                if (!IsHidden)
                {
                    while (_hiddenCache == null)
                    {
                        Thread.Sleep(500);
                    }

                    if (!_hiddenCache.TryGetValue(audioBook.Key, out var isHidden))
                    {
                        UpdateCacheInternal(UpdateTypes.Hidden | UpdateTypes.Values);
                        if (!_hiddenCache.TryGetValue(audioBook.Key, out isHidden)) return false;
                    }

                    if (isHidden) return false;
                }

                if (!IsLoaded)
                {
                    while (_loadedCache == null)
                    {
                        Thread.Sleep(500);
                    }

                    if (!_loadedCache.TryGetValue(audioBook.Key, out var isLoaded))
                    {
                        UpdateCacheInternal(UpdateTypes.Loaded | UpdateTypes.Values);
                        if (!_loadedCache.TryGetValue(audioBook.Key, out isLoaded)) return false;
                    }

                    if (isLoaded) return false;
                }
            }

            return result;
        }

        protected override void UpdateCacheInternal(UpdateTypes updateType)
        {
            var dbContainer = Context.I.DbContainer;
            if (updateType.HasFlag(UpdateTypes.Values))
            {
                _booksCache = dbContainer.AudioBookSet.GetRecordsAllWithCache();

                FillFilterValueCollection("Author",
                    _booksCache.Select(item => item.Author).Distinct().OrderBy(item => item));
                FillFilterValueCollection("Bitrate",
                    _booksCache.Select(item => item.Bitrate).Distinct().OrderBy(item => item));

                FillFilterValueCollection("Genre", GetGenres(_booksCache));

                FillFilterValueCollection("Publisher",
                    _booksCache.Select(item => item.Publisher).Distinct().OrderBy(item => item));
                FillFilterValueCollection("Reader",
                    _booksCache.Select(item => item.Reader).Distinct().OrderBy(item => item));
            }

            var updateHidden = (updateType & UpdateTypes.Hidden) == UpdateTypes.Hidden;
            var updateLoaded = (updateType & UpdateTypes.Loaded) == UpdateTypes.Loaded;

            if (updateLoaded || updateHidden)
            {
                var keys =
                    dbContainer.AudioBookSet.GetRecordsAllWithCache()
                        .ToDictionary(item => item.GroupKey + "\\" + item.Key, item => item.Key);

                if (updateHidden)
                {
                    _hiddenCache = keys.ToDictionary(item => item.Value, item => false);

                    foreach (var hiddenRecord in dbContainer.HiddenRecordSet.GetHiddenRecordsAll())
                    {
                        var key = hiddenRecord.RecordGroupKey + "\\" + hiddenRecord.RecordKey;
                        if (keys.TryGetValue(key, out var recordKey))
                        {
                            _hiddenCache[recordKey] = true;
                        }
                    }

                    var replacementStrings =
                        dbContainer.ReplacementStringSet.GetReplacementStringsAll()
                            .GroupBy(item => item.ReplaceValue);

                    foreach (var hiddenValue in dbContainer.HiddenValueSet.GetHiddenValuesAll())
                    {
                        var replacementString =
                            replacementStrings.FirstOrDefault(item => item.Key == hiddenValue.Value);
                        if (replacementString == null) continue;

                        var hiddenValues =
                            new HashSet<string>(replacementString.Select(item => item.PossibleValue));
                        hiddenValues.Add(hiddenValue.Value);

                        foreach (var audioBook in _booksCache)
                        {
                            if (_hiddenCache[audioBook.Key]) continue;

                            var hidden = false;

                            string value = null;
                            switch (hiddenValue.PropertyName)
                            {
                                case "bitrate":
                                    value = audioBook.Bitrate;
                                    break;
                                case "publisher":
                                    value = audioBook.Publisher;
                                    break;
                                case "reader":
                                    value = audioBook.Reader;
                                    break;
                                case "author":
                                    value = audioBook.Author;
                                    break;
                                case "genre":
                                    value = audioBook.Genre;
                                    break;
                            }

                            if (!Extensions.IsNullOrEmpty(value))
                            {
                                if (hiddenValue.IgnoreCase)
                                {
                                    if (hiddenValue.WholeWord)
                                    {
                                        foreach (var hiddenValue1 in hiddenValues)
                                        {
                                            hidden =
                                                string.Compare(value, hiddenValue1,
                                                    StringComparison.OrdinalIgnoreCase) == 0;
                                            if (hidden) break;
                                        }
                                    }
                                    else
                                    {
                                        foreach (var hiddenValue1 in hiddenValues)
                                        {
                                            hidden =
                                                value.IndexOf(hiddenValue1,
                                                    StringComparison.OrdinalIgnoreCase) >= 0;
                                            if (hidden) break;
                                        }
                                    }
                                }
                                else
                                {
                                    if (hiddenValue.WholeWord)
                                    {
                                        foreach (var hiddenValue1 in hiddenValues)
                                        {
                                            hidden = string.CompareOrdinal(value, hiddenValue1) == 0;
                                            if (hidden) break;
                                        }
                                    }
                                    else
                                    {
                                        foreach (var hiddenValue1 in hiddenValues)
                                        {
                                            hidden = value.IndexOf(hiddenValue1, StringComparison.Ordinal) >= 0;
                                            if (hidden) break;
                                        }
                                    }
                                }
                            }

                            _hiddenCache[audioBook.Key] = hidden;
                        }
                    }
                }

                if (updateLoaded)
                {
                    _loadedCache = keys.ToDictionary(item => item.Value, item => false);

                    foreach (var loadedRecord in dbContainer.UserDataSet.GetUserDataAll())
                    {
                        var key = loadedRecord.RecordGroupKey + "\\" + loadedRecord.RecordKey;
                        if (keys.TryGetValue(key, out var recordKey))
                        {
                            _loadedCache[recordKey] = true;
                        }
                    }
                }
            }
        }

        private IReadOnlyCollection<string> GetGenres(IEnumerable<IAudioBook> records)
        {
            var allGenres = records
                .SelectMany(item => item.GetGenres())
                .GroupBy(item => item)
                .ToDictionary(item => item.Key, item => item.Count());

            return allGenres
                .OrderByDescending(item => item.Value)
                .Where(item=>!item.Key.IsNullOrEmpty())
                .Select(item=>item.Key.ChangeCase(Extensions.CaseTypes.FirstWord, true, false))
                .ToArray();
        }

        private void FillFilterValueCollection(string collectionName, IEnumerable<string> filterValues)
        {
            var filterCollection = GetFilterValuesFromCache(collectionName);
            filterValues = filterValues.ToArray();
            var application = Application.Current;
            application?.Dispatcher.BeginInvoke((Action) (() =>
            {
                filterCollection.Clear();
                foreach (var filterValue in filterValues)
                {
                    filterCollection.Add(filterValue);
                }
            }));
        }

        private bool Filter(string filterString, object value)
        {
            return value != null &&
                   value.ToString().IndexOf(filterString, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        private bool FilterTimeSpan(string filter, TimeSpan value)
        {
            bool result;

            if (Extensions.IsNullOrEmpty(filter)) result = true;
            else
            {
                if (filter.StartsWith(">"))
                {
                    var filterValue = TimeSpanParser.Parse(filter.Substring(1, filter.Length - 1));
                    result = value > filterValue;
                }
                else if (filter.StartsWith("<"))
                {
                    var filterValue = TimeSpanParser.Parse(filter.Substring(1, filter.Length - 1));
                    result = value < filterValue;
                }
                else if (filter.StartsWith("="))
                {
                    var filterValue = TimeSpanParser.Parse(filter.Substring(1, filter.Length - 1));
                    result = value == filterValue;
                }
                else
                {
                    var filterValue = TimeSpanParser.Parse(filter);
                    result = value == filterValue;
                }
            }

            return result;
        }

        private ObservableCollection<string> GetFilterValuesFromCache(string setName)
        {
            if (!_filterValuesCache.TryGetValue(setName, out var result))
            {
                result = new ObservableCollection<string>();
                if (!_filterValuesCache.TryAdd(setName, result))
                {
                    result = _filterValuesCache[setName];
                }
            }

            return result;
        }

        private void RegisterFilteredComboboxesSource()
        {
            FilteredComboBoxEditor.Register("Author", GetFilterValuesFromCache("Author"));
            FilteredComboBoxEditor.Register("Bitrate", GetFilterValuesFromCache("Bitrate"));
            FilteredComboBoxEditor.Register("Genre", GetFilterValuesFromCache("Genre"));
            FilteredComboBoxEditor.Register("Publisher", GetFilterValuesFromCache("Publisher"));
            FilteredComboBoxEditor.Register("Reader", GetFilterValuesFromCache("Reader"));
        }
    }
}