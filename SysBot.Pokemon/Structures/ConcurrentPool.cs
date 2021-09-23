using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// List of data that can be added or removed, not indexed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("Count={" + nameof(Count) + "}")]
    public class ConcurrentPool<T> where T : class
    {
        private readonly object _syncLock = new();
        private readonly List<T> _list = new();

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

        /// <summary>
        /// Finds the first object which satisfies the <see cref="condition"/>, applies the <see cref="action"/>, and returns the <see cref="result"/>
        /// </summary>
        public bool Tag(Predicate<T> condition, Action<T> action, out T? result)
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

        /// <summary>
        /// Gets the real array of <see cref="T"/>; does not change the object's state.
        /// </summary>
        public T[] ToArray()
        {
            lock (_syncLock)
                return _list.ToArray();
        }

        public void Clear()
        {
            lock (_syncLock)
                _list.Clear();
        }
    }
}