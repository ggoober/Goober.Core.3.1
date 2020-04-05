using System.Collections.Concurrent;

namespace Goober.Core.Primitives
{
    public class ConcurentPool<T> where T: class
    {
        private ConcurrentBag<T> _objects;

        public ConcurentPool()
        {
            _objects = new ConcurrentBag<T>();
        }

        public T Take()
        {
            if (_objects.TryTake(out T item)) return item;

            return null;
        }

        public void Put(T item)
        {
            _objects.Add(item);
        }
    }
}
