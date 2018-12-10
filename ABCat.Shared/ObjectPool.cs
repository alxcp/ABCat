using System;
using System.Collections.Concurrent;

namespace ABCat.Shared
{
    public class ObjectPool<T>
    {
        private readonly Func<T> _objectGenerator;
        private readonly ConcurrentBag<T> _objects;

        public ObjectPool(Func<T> objectGenerator)
        {
            if (objectGenerator == null) throw new ArgumentNullException("objectGenerator");
            _objects = new ConcurrentBag<T>();
            _objectGenerator = objectGenerator;
        }

        public T GetObject(bool createNew = false)
        {
            T item;
            if (!createNew && _objects.TryTake(out item)) return item;
            return _objectGenerator();
        }

        public ObjectPoolItem<T> GetPoolItem(bool createNew)
        {
            return new ObjectPoolItem<T>(this, createNew);
        }

        public void PutObject(T item)
        {
            _objects.Add(item);
        }
    }
}