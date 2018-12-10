using System;
using System.Collections.Generic;

namespace ABCat.Shared.Plugins.DataSets
{
    public interface IUserDataSet : IObjectSet<IUserData>
    {
        void AddUserData(params IUserData[] userData);
        IUserData CreateUserData();
        IEnumerable<IUserData> GetUserDataAll();
        IEnumerable<IUserData> Where(Func<IUserData, bool> func);
    }
}