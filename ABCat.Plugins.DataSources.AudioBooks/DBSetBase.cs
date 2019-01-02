using System;
using SQLite.Net;
using SQLite.Net.Platform.Win32;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    public abstract class DBSetBase : SQLiteConnection
    {
        protected DBSetBase(string path, ExecuteWithLock executeWithLockDelegate, bool vacuum) : base(
            new SQLitePlatformWin32(), path)
        {
            ExecuteWithLockDelegate = executeWithLockDelegate;
        }

        protected ExecuteWithLock ExecuteWithLockDelegate { get; }

        protected void ExecuteWithLock(Action action)
        {
            ExecuteWithLockDelegate(action);
        }

        protected T ExecuteWithLock<T>(Func<T> action)
        {
            var result = default(T);
            ExecuteWithLockDelegate(() => { result = action(); });
            return result;
        }
    }
}