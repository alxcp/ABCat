using System;
using System.Collections.Generic;
using System.Linq;
using ABCat.Shared.Plugins.DataSets;
using SQLite.Net;
using SQLite.Net.Platform.Win32;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    public class UserDataSet : SQLiteConnection, IUserDataSet, IHiddenRecordSet, IHiddenValueSet
    {
        private readonly object _lockContext;
        private volatile bool _isTablesCreated;

        public UserDataSet(string databasePath, object lockContext)
            : base(new SQLitePlatformWin32(), databasePath)
        {
            _lockContext = lockContext;
            if (!_isTablesCreated)
            {
                lock (_lockContext)
                {
                    if (!_isTablesCreated)
                    {
                        CreateTable<UserData>();
                        CreateTable<HiddenRecord>();
                        CreateTable<HiddenValue>();
                        _isTablesCreated = true;
                    }
                }
            }
        }

        public void AddHiddenRecord(params IHiddenRecord[] hiddenRecords)
        {
            lock (_lockContext)
            {
                InsertAll(hiddenRecords);
            }
        }

        public IHiddenRecord CreateHiddenRecord()
        {
            return new HiddenRecord();
        }

        public IEnumerable<IHiddenRecord> GetHiddenRecords(string groupName, HashSet<string> keys)
        {
            lock (_lockContext)
            {
                return
                    Table<HiddenRecord>()
                        .Where(item => item.RecordGroupKey == groupName && keys.Contains(item.RecordKey));
            }
        }

        public IEnumerable<IHiddenRecord> GetHiddenRecordsAll()
        {
            lock (_lockContext)
            {
                return Table<HiddenRecord>();
            }
        }

        public void AddHiddenValue(params IHiddenValue[] hiddenValue)
        {
            lock (_lockContext)
            {
                InsertAll(hiddenValue);
            }
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
            lock (_lockContext)
            {
                return Table<HiddenValue>().Where(item => item.PropertyName == propertyName);
            }
        }

        public IEnumerable<IHiddenValue> GetHiddenValuesAll()
        {
            lock (_lockContext)
            {
                return Table<HiddenValue>();
            }
        }

        public bool IsHidden(string propertyName, string value)
        {
            return Table<HiddenValue>().Any(item => item.PropertyName == propertyName && item.Value == value);
        }

        public void AddUserData(params IUserData[] userData)
        {
            lock (_lockContext)
            {
                InsertAll(userData);
            }
        }

        public IUserData CreateUserData()
        {
            return new UserData();
        }

        public IEnumerable<IUserData> GetUserDataAll()
        {
            lock (_lockContext)
            {
                return Table<UserData>();
            }
        }

        public IEnumerable<IUserData> Where(Func<IUserData, bool> func)
        {
            lock (_lockContext)
            {
                return Table<UserData>().Where(item => func(item));
            }
        }
    }
}