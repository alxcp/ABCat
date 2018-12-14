using System;
using System.Collections.Generic;
using System.Linq;
using ABCat.Shared.Plugins.DataSets;
using SQLite.Net;
using SQLite.Net.Platform.Win32;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    public class UserDataSet : DBSetBase, IUserDataSet, IHiddenRecordSet, IHiddenValueSet
    {
        private volatile bool _isTablesCreated;

        public UserDataSet(string databasePath, ExecuteWithLock executeWithLockDelegate)
            : base(databasePath, executeWithLockDelegate, false)
        {
            if (!_isTablesCreated)
            {
                ExecuteWithLock(() =>
                {
                    if (!_isTablesCreated)
                    {
                        CreateTable<UserData>();
                        CreateTable<HiddenRecord>();
                        CreateTable<HiddenValue>();
                        _isTablesCreated = true;
                    }
                });
            }
        }

        public void AddHiddenRecord(params IHiddenRecord[] hiddenRecords)
        {
            ExecuteWithLock(() => { InsertAll(hiddenRecords); });
        }

        public IHiddenRecord CreateHiddenRecord()
        {
            return new HiddenRecord();
        }

        public IEnumerable<IHiddenRecord> GetHiddenRecords(string groupName, HashSet<string> keys)
        {
            return ExecuteWithLock(() =>
            {
                return
                    Table<HiddenRecord>()
                        .Where(item => item.RecordGroupKey == groupName && keys.Contains(item.RecordKey));
            });
        }

        public IEnumerable<IHiddenRecord> GetHiddenRecordsAll()
        {
            return ExecuteWithLock(Table<HiddenRecord>);
        }

        public void AddHiddenValue(params IHiddenValue[] hiddenValue)
        {
            ExecuteWithLock(() => { InsertAll(hiddenValue); });
        }

        public IHiddenValue CreateHiddenValue()
        {
            return new HiddenValue();
        }

        public void Delete(string propertyName, string value)
        {
            var record =
                Table<HiddenValue>().FirstOrDefault(item => item.PropertyName == propertyName && item.Value == value);

            if (record != null)
            {
                Delete(record);
            }
        }

        public IEnumerable<IHiddenValue> GetHiddenValues(string propertyName)
        {
            return ExecuteWithLock(() =>
            {
                return Table<HiddenValue>().Where(item => item.PropertyName == propertyName);
            });
        }

        public IEnumerable<IHiddenValue> GetHiddenValuesAll()
        {
            return ExecuteWithLock(Table<HiddenValue>);
        }

        public bool IsHidden(string propertyName, string value)
        {
            return Table<HiddenValue>().Any(item => item.PropertyName == propertyName && item.Value == value);
        }

        public void AddUserData(params IUserData[] userData)
        {
            ExecuteWithLock(() => { InsertAll(userData); });
        }

        public IUserData CreateUserData()
        {
            return new UserData();
        }

        public IEnumerable<IUserData> GetUserDataAll()
        {
            return ExecuteWithLock(Table<UserData>);
        }

        public IEnumerable<IUserData> Where(Func<IUserData, bool> func)
        {
            return ExecuteWithLock(() => { return Table<UserData>().Where(item => func(item)); });
        }
    }
}