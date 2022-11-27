using System.Collections.Generic;
using System.Diagnostics;

// ms-lpl, removed from their website but archived on the internet, with alterations to be inheritable

namespace System.Collections.Concurrent
{
    /// <summary>Provides a thread-safe priority queue data structure.</summary>
    /// <typeparam name="TKey">Specifies the type of keys used to prioritize values.</typeparam>
    /// <typeparam name="TValue">Specifies the type of elements in the queue.</typeparam>
    [DebuggerDisplay("Count={" + nameof(Count) + "}")]
    public class ConcurrentPriorityQueue<TKey, TValue> : IProducerConsumerCollection<KeyValuePair<TKey, TValue>> where TKey : IComparable<TKey> where TValue : IEquatable<TValue>
    {
        protected readonly object _syncLock = new();
        protected readonly MinQueue Queue = new();

        /// <summary>Initializes a new instance of the ConcurrentPriorityQueue class.</summary>
        public ConcurrentPriorityQueue() { }

        /// <summary>Initializes a new instance of the ConcurrentPriorityQueue class that contains elements copied from the specified collection.</summary>
        /// <param name="collection">The collection whose elements are copied to the new ConcurrentPriorityQueue.</param>
        public ConcurrentPriorityQueue(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            foreach (var item in collection)
                Queue.Insert(item);
        }

        /// <summary>Adds the key/value pair to the priority queue.</summary>
        /// <param name="priority">The priority of the item to be added.</param>
        /// <param name="value">The item to be added.</param>
        public void Enqueue(TKey priority, TValue value)
        {
            Enqueue(new KeyValuePair<TKey, TValue>(priority, value));
        }

        /// <summary>Adds the key/value pair to the priority queue.</summary>
        /// <param name="item">The key/value pair to be added to the queue.</param>
        public void Enqueue(KeyValuePair<TKey, TValue> item)
        {
            lock (_syncLock)
                Queue.Insert(item);
        }

        /// <summary>Attempts to remove and return the next prioritized item in the queue.</summary>
        /// <param name="result">
        /// When this method returns, if the operation was successful, result contains the object removed. If
        /// no object was available to be removed, the value is unspecified.
        /// </param>
        /// <returns>
        /// true if an element was removed and returned from the queue successfully; otherwise, false.
        /// </returns>
        public bool TryDequeue(out KeyValuePair<TKey, TValue> result)
        {
            result = default;
            lock (_syncLock)
            {
                if (Queue.Count == 0)
                    return false;
                result = Queue.Remove();
                return true;
            }
        }

        /// <summary>Attempts to return the next prioritized item in the queue.</summary>
        /// <param name="result">
        /// When this method returns, if the operation was successful, result contains the object.
        /// The queue was not modified by the operation.
        /// </param>
        /// <returns>
        /// true if an element was returned from the queue successfully; otherwise, false.
        /// </returns>
        public bool TryPeek(out KeyValuePair<TKey, TValue> result)
        {
            result = default;
            lock (_syncLock)
            {
                if (Queue.Count == 0)
                    return false;
                result = Queue.Peek();
                return true;
            }
        }

        /// <summary>Empties the queue.</summary>
        public void Clear() { lock (_syncLock) Queue.Clear(); }

        /// <summary>Gets whether the queue is empty.</summary>
        public bool IsEmpty => Count == 0;

        /// <summary>Gets the number of elements contained in the queue.</summary>
        public int Count
        {
            get { lock (_syncLock) return Queue.Count; }
        }

        /// <summary>Copies the elements of the collection to an array, starting at a particular array index.</summary>
        /// <param name="array">
        /// The one-dimensional array that is the destination of the elements copied from the queue.
        /// </param>
        /// <param name="index">
        /// The zero-based index in array at which copying begins.
        /// </param>
        /// <remarks>The elements will not be copied to the array in any guaranteed order.</remarks>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index)
        {
            lock (_syncLock) Queue.Items.CopyTo(array, index);
        }

        /// <summary>Copies the elements stored in the queue to a new array.</summary>
        /// <returns>A new array containing a snapshot of elements copied from the queue.</returns>
        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            lock (_syncLock)
            {
                var clonedqueue = new MinQueue(Queue);
                var result = new KeyValuePair<TKey, TValue>[Queue.Count];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = clonedqueue.Remove();
                }
                return result;
            }
        }

        /// <summary>
        /// Searches through the <see cref="Queue"/> to find the first that matches the provided function.
        /// </summary>
        /// <param name="match">Function to find a match with</param>
        /// <returns>Default empty if none matching</returns>
        public KeyValuePair<TKey, TValue> Find(Func<TValue, bool> match)
        {
            lock (_syncLock)
            {
                return Queue.Items.Find(z => match(z.Value));
            }
        }

        /// <summary>Attempts to add an item in the queue.</summary>
        /// <param name="item">The key/value pair to be added.</param>
        /// <returns>
        /// true if the pair was added; otherwise, false.
        /// </returns>
        bool IProducerConsumerCollection<KeyValuePair<TKey, TValue>>.TryAdd(KeyValuePair<TKey, TValue> item)
        {
            Enqueue(item);
            return true;
        }

        /// <summary>Attempts to remove and return the next prioritized item in the queue.</summary>
        /// <param name="item">
        /// When this method returns, if the operation was successful, result contains the object removed. If
        /// no object was available to be removed, the value is unspecified.
        /// </param>
        /// <returns>
        /// true if an element was removed and returned from the queue successfully; otherwise, false.
        /// </returns>
        bool IProducerConsumerCollection<KeyValuePair<TKey, TValue>>.TryTake(out KeyValuePair<TKey, TValue> item)
        {
            return TryDequeue(out item);
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An enumerator for the contents of the queue.</returns>
        /// <remarks>
        /// The enumeration represents a moment-in-time snapshot of the contents of the queue. It does not
        /// reflect any updates to the collection after GetEnumerator was called. The enumerator is safe to
        /// use concurrently with reads from and writes to the queue.
        /// </remarks>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            var arr = ToArray();
            return ((IEnumerable<KeyValuePair<TKey, TValue>>)arr).GetEnumerator();
        }

        /// <summary>Returns an enumerator that iterates through a collection.</summary>
        /// <returns>An IEnumerator that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        /// <summary>Copies the elements of the collection to an array, starting at a particular array index.</summary>
        /// <param name="array">
        /// The one-dimensional array that is the destination of the elements copied from the queue.
        /// </param>
        /// <param name="index">
        /// The zero-based index in array at which copying begins.
        /// </param>
        void ICollection.CopyTo(Array array, int index)
        {
            lock (_syncLock)
                ((ICollection)Queue.Items).CopyTo(array, index);
        }

        /// <summary>
        /// Gets a value indicating whether access to the ICollection is synchronized with the SyncRoot.
        /// </summary>
        bool ICollection.IsSynchronized => true;

        /// <summary>
        /// Gets an object that can be used to synchronize access to the collection.
        /// </summary>
        object ICollection.SyncRoot => _syncLock;

        /// <summary>Implements a queue that prioritizes smaller values.</summary>
        protected sealed class MinQueue
        {
            /// <summary>Gets the number of objects stored in the Queue.</summary>
            public int Count => Items.Count;

            public List<KeyValuePair<TKey, TValue>> Items { get; }

            /// <summary>Initializes an empty queue.</summary>
            public MinQueue() => Items = new List<KeyValuePair<TKey, TValue>>();

            /// <summary>Initializes a queue as a copy of another queue instance.</summary>
            /// <param name="queue">The queue to copy.</param>
            /// <remarks>Key/Value values are not deep cloned.</remarks>
            public MinQueue(MinQueue queue) => Items = new List<KeyValuePair<TKey, TValue>>(queue.Items);

            /// <summary>Empties the Queue.</summary>
            public void Clear() => Items.Clear();

            /// <summary>Adds an item to the Queue.</summary>
            public void Insert(TKey key, TValue value) => Insert(new KeyValuePair<TKey, TValue>(key, value));

            /// <summary>Adds an item to the Queue.</summary>
            public void Insert(KeyValuePair<TKey, TValue> entry)
            {
                var nearest = Items.FindLastIndex(z => entry.Key.CompareTo(z.Key) >= 0);
                Items.Insert(nearest + 1, entry);
            }

            /// <summary>Returns the entry at the top of the Queue.</summary>
            public KeyValuePair<TKey, TValue> Peek() => Items[0];

            /// <summary>Removes the entry at the top of the Queue.</summary>
            public KeyValuePair<TKey, TValue> Remove()
            {
                var toReturn = Items[0];
                Items.RemoveAt(0);
                return toReturn;
            }
        }

        public int Remove(TValue detail)
        {
            lock (_syncLock)
            {
                var items = Queue.Items;
                return items.RemoveAll(z => z.Value!.Equals(detail));
            }
        }

        public int IndexOf(TValue detail)
        {
            lock (_syncLock)
            {
                var items = Queue.Items;
                return items.FindIndex(z => z.Value!.Equals(detail));
            }
        }
    }
}
