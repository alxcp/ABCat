using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ABCat.Shared.Plugins.DataSets;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    public class AudioBookSet : DBSetBase, IAudioBookSet, IAudioBookGroupSet
    {
        private static volatile bool _isTablesCreated;

        private readonly ConcurrentQueue<AudioBookGroup> _addedAudioBookGroups = new ConcurrentQueue<AudioBookGroup>();
        private readonly ConcurrentQueue<AudioBook> _addedAudioBooks = new ConcurrentQueue<AudioBook>();

        private readonly ConcurrentQueue<AudioBookGroup>
            _changedAudioBookGroups = new ConcurrentQueue<AudioBookGroup>();

        private readonly ConcurrentQueue<AudioBook> _changedAudioBooks = new ConcurrentQueue<AudioBook>();

        public AudioBookSet(string path, ExecuteWithLock executeWithLockDelegate, bool vacuum)
            : base(path, executeWithLockDelegate, vacuum)
        {
            if (!_isTablesCreated)
            {
                ExecuteWithLock(() =>
                {
                    if (!_isTablesCreated)
                    {
                        CreateTable<AudioBook>();
                        CreateTable<AudioBookGroup>();
                        _isTablesCreated = true;

                        if (vacuum) Execute("VACUUM");
                    }
                });
            }
        }

        public void AddChangedRecordGroups(params IAudioBookGroup[] audioBookGroups)
        {
            foreach (var audioBookGroup in audioBookGroups.Cast<AudioBookGroup>())
            {
                _changedAudioBookGroups.Enqueue(audioBookGroup);
            }
        }

        public void AddRecordGroup(IAudioBookGroup group)
        {
            _addedAudioBookGroups.Enqueue((AudioBookGroup) group);
        }

        public IAudioBookGroup CreateRecordGroup()
        {
            return new AudioBookGroup();
        }

        public IAudioBookGroup GetRecordGroupByKey(string key)
        {
            return ExecuteWithLock(() =>
            {
                return Table<AudioBookGroup>().FirstOrDefault(group => group.Key == key);
            });
        }

        public IEnumerable<IAudioBookGroup> GetRecordGroupsAll()
        {
            return ExecuteWithLock(Table<AudioBookGroup>);
        }

        public IEnumerable<IAudioBookGroup> GetRecordGroupsByIds(HashSet<int> groupIds)
        {
            return ExecuteWithLock(() =>
            {
                return Table<AudioBookGroup>().Where(group => groupIds.Contains(group.Id));
            });
        }

        public IEnumerable<IAudioBookGroup> GetRecordGroupsUpdatedBefore(DateTime lastUpdate)
        {
            return ExecuteWithLock(() =>
            {
                return Table<AudioBookGroup>().Where(group => group.LastUpdate <= lastUpdate);
            });
        }

        public void SaveAudioBookGroups()
        {
            ExecuteWithLock(() =>
            {
                var bookGroups4Add = new List<AudioBookGroup>();

                while (_addedAudioBookGroups.Any())
                {
                    if (_addedAudioBookGroups.TryDequeue(out var @group))
                    {
                        bookGroups4Add.Add(group);
                    }
                }

                var bookGroups4Replace = new List<AudioBookGroup>();

                while (_changedAudioBookGroups.Any())
                {
                    if (_changedAudioBookGroups.TryDequeue(out var @group))
                    {
                        bookGroups4Replace.Add(group);
                    }
                }

                RunInTransaction(() =>
                {
                    if (bookGroups4Replace.Any())
                    {
                        Execute(@"
DELETE FROM AudioBookGroup
WHERE Key IN({0})".F(string.Join(",", bookGroups4Replace.Select(item => item.Key))));
                    }

                    InsertAll(bookGroups4Add.Union(bookGroups4Replace));
                });
            });
        }

        public void AddChangedRecords(params IAudioBook[] audioBooks)
        {
            foreach (var audioBook in audioBooks.Cast<AudioBook>())
            {
                _changedAudioBooks.Enqueue(audioBook);
            }
        }

        public void AddRecord(IAudioBook record)
        {
            _addedAudioBooks.Enqueue((AudioBook) record);
        }

        public IAudioBook CreateRecord()
        {
            return new AudioBook();
        }

        public IEnumerable<IAudioBook> GetRecordsAll()
        {
            return ExecuteWithLock<IEnumerable<IAudioBook>>(Table<AudioBook>);
        }

        public IEnumerable<IAudioBook> GetRecordsByGroup(string linkedObjectString)
        {
            return ExecuteWithLock(() => Query<AudioBook>(@"
SELECT * FROM AudioBook
WHERE GroupKey = ?;", linkedObjectString));
        }

        public IEnumerable<IAudioBook> GetRecordsByKeys(HashSet<string> recordsKeys)
        {
            return ExecuteWithLock(() =>
            {
                var result = new List<IAudioBook>();
                var count = 0;

                while (count * 500 < recordsKeys.Count)
                {
                    var part = new HashSet<string>(recordsKeys.Skip(count * 500).Take(500));
                    result.AddRange(Table<AudioBook>().Where(audioBook => part.Contains(audioBook.Key)));
                    count++;
                }

                return result;
            });
        }

        public IEnumerable<IAudioBook> GetRecordsUpdatedBefore(DateTime lastUpdate)
        {
            return ExecuteWithLock(() =>
            {
                return Table<AudioBook>().ToArray().Where(audioBook => audioBook.LastUpdate <= lastUpdate);
            });
        }

        public void SaveAudioBooks()
        {
            ExecuteWithLock(() =>
            {
                var books4Add = new List<AudioBook>();

                while (_addedAudioBooks.Count > 0)
                {
                    if (_addedAudioBooks.TryDequeue(out var book))
                    {
                        books4Add.Add(book);
                    }
                }

                var books4Replace = new List<AudioBook>();

                while (_changedAudioBooks.Count > 0)
                {
                    if (_changedAudioBooks.TryDequeue(out var book))
                    {
                        books4Replace.Add(book);
                    }
                }

                RunInTransaction(() =>
                {
                    if (books4Replace.Any())
                    {
                        Execute(@"
DELETE FROM AudioBook
WHERE Key IN({0})".F(string.Join(",", books4Replace.Select(item => item.Key))));
                    }

                    InsertAll(books4Add.Union(books4Replace));
                });
            });
        }

        public void Import(string dbFilePath)
        {
            using (var audioBookSet = new AudioBookSet(dbFilePath, ExecuteWithLockDelegate, true))
            {
                var groups = audioBookSet.GetRecordGroupsAll();
                var records = audioBookSet.GetRecordsAll();

                var existedGroups = new HashSet<string>(GetRecordGroupsAll().Select(item => item.Key));
                var existedRecords = new HashSet<string>(GetRecordsAll().Select(item => item.Key));

                var newGroups = groups.Where(group => !existedGroups.Contains(group.Key));
                var newRecords = records.Where(record => !existedRecords.Contains(record.Key));

                foreach (var audioBookGroup in newGroups)
                {
                    AddRecordGroup(audioBookGroup);
                }

                foreach (var newRecord in newRecords)
                {
                    AddRecord(newRecord);
                }
            }
        }

        public IAudioBook GetRecordByKey(string key)
        {
            return ExecuteWithLock(() => FindWithQuery<AudioBook>(@"
SELECT * FROM AudioBook
WHERE Key = ?
LIMIT 1;", key));
        }
    }
}