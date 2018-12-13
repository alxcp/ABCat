using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ABCat.Shared.Plugins.DataSets;
using SQLite.Net;
using SQLite.Net.Platform.Win32;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    public class BinaryDataSet : SQLiteConnection, IBinaryDataSet
    {
        private readonly ConcurrentQueue<BinaryData> _addedBinaryData = new ConcurrentQueue<BinaryData>();
        private readonly ConcurrentQueue<BinaryData> _changedBinaryData = new ConcurrentQueue<BinaryData>();
        private readonly object _lockContext;

        private volatile bool _isTablesCreated;
        private readonly Timer _saveTimer;
        private readonly TimeSpan _savePeriod = TimeSpan.FromSeconds(30);

        public BinaryDataSet(string databasePath, object lockContext)
            : base(new SQLitePlatformWin32(), databasePath)
        {
            _lockContext = lockContext;

            if (!_isTablesCreated)
            {
                lock (_lockContext)
                {
                    if (!_isTablesCreated)
                    {
                        CreateTable<BinaryData>();
                        _isTablesCreated = true;
                    }
                }
            }

            _saveTimer = new Timer(SaveBinaryDataByTimer, null, _savePeriod, _savePeriod);
        }

        public void AddChangedBinaryData(params IBinaryData[] audioBooks)
        {
            foreach (var binaryData in audioBooks.Cast<BinaryData>())
            {
                _changedBinaryData.Enqueue(binaryData);
            }
        }

        public IBinaryData CreateBinaryData()
        {
            return new BinaryData();
        }

        public IBinaryData GetByKey(string key)
        {
            lock (_lockContext)
            {
                return FindWithQuery<BinaryData>(@"
SELECT * FROM BinaryData
WHERE Key = ?
LIMIT 1;", key);
            }
        }

        private void SaveBinaryDataByTimer(object o = null)
        {
            if (_addedBinaryData.Any() || _changedBinaryData.Any())
                SaveBinaryData();
        }

        public void SaveBinaryData()
        {
            lock (_lockContext)
            {
                var data4Add = new List<BinaryData>();

                while (_addedBinaryData.Any())
                {
                    if (_addedBinaryData.TryDequeue(out var data))
                    {
                        data4Add.Add(data);
                    }
                }

                var data4Replace = new List<BinaryData>();

                while (_changedBinaryData.Any())
                {
                    if (_changedBinaryData.TryDequeue(out var data))
                    {
                        data4Replace.Add(data);
                    }
                }

                RunInTransaction(() =>
                {
                    if (data4Replace.AnySafe())
                    {
                        Execute(@"
DELETE FROM BinaryData
WHERE Key IN({0})".F(string.Join(",", data4Replace.Select(item => "'{0}'".F(item.Key)))));
                    }

                    InsertAll(data4Add.Union(data4Replace));
                });
            }
        }

        public void Import(string dbFilePath)
        {
            var existed = GetKeysAll();

            using (var binaryDataSet = new BinaryDataSet(dbFilePath, _lockContext))
            {
                var importKeys = binaryDataSet.GetKeysAll();

                var newKeys = importKeys.Where(item => !existed.Contains(item)).ToArray();
                var currentKeys = new HashSet<string>();

                for (var z = 0; z < newKeys.Length; z++)
                {
                    currentKeys.Add(newKeys[z]);

                    if (currentKeys.Count == 20 || z == newKeys.Length - 1)
                    {
                        AddChangedBinaryData(binaryDataSet.GetByKeys(currentKeys).ToArray());
                        SaveBinaryData();
                    }
                }
            }
        }

        public void AddBinaryData(IBinaryData binaryData)
        {
            _addedBinaryData.Enqueue((BinaryData) binaryData);
        }

        public IEnumerable<IBinaryData> GetByKeys(HashSet<string> keys)
        {
            lock (_lockContext)
            {
                return Table<BinaryData>().Where(row => keys.Contains(row.Key));
            }
        }

        public HashSet<string> GetKeysAll()
        {
            return
                new HashSet<string>(
                    Query(Table<BinaryData>().Table, "SELECT Key FROM BinaryData")
                        .Cast<BinaryData>()
                        .Select(item => item.Key));
        }
    }
}