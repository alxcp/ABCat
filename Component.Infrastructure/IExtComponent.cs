using System;

namespace Component.Infrastructure
{
    public interface IExtComponent : IDisposable
    {
        bool CheckForConfig(bool correct, out Config incorrectConfig);
    }
}