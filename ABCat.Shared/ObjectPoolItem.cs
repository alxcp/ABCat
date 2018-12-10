using System;

namespace ABCat.Shared
{
    public sealed class ObjectPoolItem<T> : IDisposable
    {
        private readonly ObjectPool<T> _owner;

        public ObjectPoolItem(ObjectPool<T> owner, bool createNew = false)
        {
            _owner = owner;
            Target = owner.GetObject(createNew);
        }

        public T Target { get; private set; }

        public void Dispose()
        {
            _owner.PutObject(Target);
        }

        public void Refresh()
        {
            Target = _owner.GetObject(true);
        }
    }
}