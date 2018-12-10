using System;
using System.Collections.Generic;
using System.Linq;
using ABCat.Shared;
using ABCat.Shared.Plugins.DataSets;
using SQLite.Net;
using SQLite.Net.Platform.Win32;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    public class ReplacementStringSet : SQLiteConnection, IReplacementStringSet
    {
        private readonly object _lockContext;

        private volatile bool _isTablesCreated;

        public ReplacementStringSet(string databasePath, object lockContext)
            : base(new SQLitePlatformWin32(), databasePath)
        {
            _lockContext = lockContext;
            if (!_isTablesCreated)
            {
                lock (_lockContext)
                {
                    if (!_isTablesCreated)
                    {
                        CreateTable<ReplacementString>();
                        _isTablesCreated = true;
                    }
                }
            }
        }

        public void AddReplacementString(params IReplacementString[] replacementString)
        {
            lock (_lockContext)
            {
                InsertAll(replacementString);
            }
        }

        public IReplacementString CreateReplacementString()
        {
            return new ReplacementString();
        }

        public void Delete(string recordPropertyName, string replaceValue, string possibleValue)
        {
            if (Extensions.IsNullOrEmpty(recordPropertyName))
                throw new ArgumentNullException(nameof(recordPropertyName));
            if (Extensions.IsNullOrEmpty(replaceValue)) throw new ArgumentNullException(nameof(replaceValue));

            lock (_lockContext)
            {
                RunInTransaction(() =>
                {
                    List<ReplacementString> toRemove;
                    if (Extensions.IsNullOrEmpty(possibleValue))
                    {
                        toRemove =
                            Table<ReplacementString>()
                                .Where(
                                    item =>
                                        item.RecordPropertyName == recordPropertyName &&
                                        item.ReplaceValue == replaceValue)
                                .ToList();
                    }
                    else
                    {
                        toRemove =
                            Table<ReplacementString>()
                                .Where(
                                    item =>
                                        item.RecordPropertyName == recordPropertyName &&
                                        item.ReplaceValue == replaceValue && item.PossibleValue == possibleValue)
                                .ToList();
                    }

                    foreach (var replacementString in toRemove)
                    {
                        Delete(replacementString);
                    }
                });
            }
        }

        public IEnumerable<IReplacementString> GetReplacementStringsAll()
        {
            lock (_lockContext)
            {
                return Table<ReplacementString>();
            }
        }

        public IEnumerable<IReplacementString> GetReplacementStringsBy(string propertyName)
        {
            lock (_lockContext)
            {
                return Table<ReplacementString>().Where(item => item.RecordPropertyName == propertyName);
            }
        }

        public IEnumerable<IReplacementString> Where(Func<IReplacementString, bool> predicate)
        {
            lock (_lockContext)
            {
                return Table<ReplacementString>().Where(replacementString => predicate(replacementString));
            }
        }
    }
}