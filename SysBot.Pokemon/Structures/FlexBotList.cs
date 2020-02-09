using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Collections.Concurrent
{
    [DebuggerDisplay("Count={" + nameof(Count) + "}")]
    public class FlexBotList<T>
    {
        private readonly object _syncLock = new object();
        private readonly List<T> _list = new List<T>();

        public void Add(T item)
        {
            lock (_syncLock)
                _list.Add(item);
        }

        public void Remove(T item)
        {
            lock (_syncLock)
                _list.Remove(item);
        }

        public int Count
        {
            get
            {
                lock (_syncLock)
                    return _list.Count;
            }
        }

        public bool All(Func<T, bool> condition)
        {
            lock (_syncLock)
                return _list.All(condition);
        }

        public bool Tag(Predicate<T> condition, Action<T> action, out T result)
        {
            lock (_syncLock)
            {
                result = _list.Find(condition);
                if (result == null)
                    return false;

                action(result);
                return true;
            }
        }
    }
}