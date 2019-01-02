using System;
using System.Collections.Generic;
using System.Linq;
using ABCat.Shared;
using ABCat.Shared.Plugins.DataSets;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    public class ReplacementStringSet : DBSetBase, IReplacementStringSet
    {
        private volatile bool _isTablesCreated;

        public ReplacementStringSet(string databasePath, ExecuteWithLock executeWithLockDelegate)
            : base(databasePath, executeWithLockDelegate, false)
        {
            if (!_isTablesCreated)
            {
                ExecuteWithLock(() =>
                {
                    if (!_isTablesCreated)
                    {
                        CreateTable<ReplacementString>();
                        _isTablesCreated = true;
                    }
                });
            }
        }

        public void AddReplacementString(params IReplacementString[] replacementString)
        {
            ExecuteWithLock(() => { InsertAll(replacementString); });
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

            ExecuteWithLock(() =>
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
            });
        }

        public IEnumerable<IReplacementString> GetReplacementStringsAll()
        {
            return ExecuteWithLock(Table<ReplacementString>);
        }

        public IEnumerable<IReplacementString> GetReplacementStringsBy(string propertyName)
        {
            return ExecuteWithLock(() =>
            {
                return Table<ReplacementString>().Where(item => item.RecordPropertyName == propertyName);
            });
        }

        public IEnumerable<IReplacementString> Where(Func<IReplacementString, bool> predicate)
        {
            return ExecuteWithLock(() =>
            {
                return Table<ReplacementString>().Where(replacementString => predicate(replacementString));
            });
        }
    }
}