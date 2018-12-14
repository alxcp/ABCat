using System;
using SQLite.Net;
using SQLite.Net.Platform.Win32;

namespace ABCat.Plugins.DataSources.AudioBooks
{
    public abstract class DBSetBase : SQLiteConnection
    {
        protected ExecuteWithLock ExecuteWithLockDelegate { get; }

        protected DBSetBase(string path, ExecuteWithLock executeWithLockDelegate, bool vacuum) : base(new SQLitePlatformWin32(), path)
        {
            ExecuteWithLockDelegate = executeWithLockDelegate;
        }

        protected void ExecuteWithLock(Action action)
        {
            ExecuteWithLockDelegate(action);
        }

        protected T ExecuteWithLock<T>(Func<T> action)
        {
            T result = default(T);
            ExecuteWithLockDelegate(() => { result = action(); });
            return result;
        }
    }
}