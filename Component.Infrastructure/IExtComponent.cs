using System;

namespace Component.Infrastructure
{
    public interface IExtComponent : IDisposable
    {
        void FixComponentConfig();
    }
}