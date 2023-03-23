using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TreesearchLib
{
    public interface IStateCollection<T>
    {
        /// <summary>
        /// The number of nodes in the collection
        /// </summary>
        /// <value></value>
        int Nodes { get; }

        /// <summary>
        /// Obtains the next node, or none if the collection is empty
        /// </summary>
        /// <param name="value">The node that was obtained</param>
        /// <returns>Whether a node was obtained</returns>
        bool TryGetNext(out T value);
        /// <summary>
        /// Stores a node in the collection
        /// </summary>
        /// <param name="state">The node to store</param>
        void Store(T state);
        /// <summary>
        /// Returns all stored states as an enumerable in no particular order
        /// </summary>
        /// <returns>The stored states</returns>
        IEnumerable<T> AsEnumerable();
    }

    /// <summary>
    /// This collection uses a stack, and thus the last stored node will be the first to be returned
    /// </summary>
    /// <typeparam name="T">The type of the node</typeparam>
    public class LIFOCollection<T> : IStateCollection<T>
    {
        public int Nodes => states.Count;

        private Stack<T> states = new Stack<T>();

        public LIFOCollection()
        {
        }

        public LIFOCollection(T initial) : this()
        {
            Store(initial);
        }

        public bool TryGetNext(out T next)
        {
            if (states.Count == 0)
            {
                next = default(T);
                return false;
            }
            next = states.Pop();
            return true;
        }

        public void Store(T state) => states.Push(state);

        public IEnumerable<T> AsEnumerable() => states;
    }

    /// <summary>
    /// This collection uses a queue, and thus the first stored node will be the first to be returned
    /// </summary>
    /// <typeparam name="T">The type of the node</typeparam>
    public class FIFOCollection<T> : IStateCollection<T>
    {
        public int Nodes => states.Count;

        private Queue<T> states = new Queue<T>();

        public FIFOCollection()
        {
        }

        public FIFOCollection(T initial) : this()
        {
            Store(initial);
        }

        internal FIFOCollection(Queue<T> other)
        {
            states = other;
        }

        internal FIFOCollection(IEnumerable<T> other)
        {
            states = new Queue<T>(other);
        }

        public bool TryGetNext(out T next)
        {
            if (states.Count == 0)
            {
                next = default(T);
                return false;
            }
            next = states.Dequeue();
            return true;
        }

        public void Store(T state) => states.Enqueue(state);

        public IEnumerable<T> AsEnumerable() => states;
    }

    /// <summary>
    /// This collection maintains two queues, the first queue (aka the get-queue) is to retrieve items,
    /// the second queue (aka the put-queue) to store items. Using <see cref="SwapQueues"/> the queues
    /// may be swapped and their roles switch.
    /// 
    /// Sometimes in breadth-first search, a level should be completed, before the next level is started.
    /// This collection supports that case in that the next level is maintained in a separate collection.
    /// The <see cref="Nodes"> point to the number of states in the get-queue.
    /// </summary>
    /// <remarks>
    /// Because of the peculiar behaviour, BiLevelFIFOCollection does not implement
    /// <see cref="IStateCollection{T}"/>. It allows to "export" as a regular
    /// <see cref="FIFOCollection{T}"/>, by calling <see cref="ToSingleLevel"/>.
    /// 
    /// Also, when calling <see cref="SwapQueues"/>, any remaining nodes in the get-queue are prepended to the
    /// nodes in the put-queue before swapping.
    /// </remarks>
    /// <typeparam name="T">The type of node to store</typeparam>
    public class BiLevelFIFOCollection<T>
    {
        public int GetQueueNodes => getQueue.Count;
        public int PutQueueNodes => putQueue.Count;
        public long RetrievedNodes { get; private set; }

        private Queue<T> getQueue = new Queue<T>();
        private Queue<T> putQueue = new Queue<T>();

        public BiLevelFIFOCollection()
        {
            RetrievedNodes = 0;
        }

        public BiLevelFIFOCollection(T initial) : this()
        {
            getQueue.Enqueue(initial); // initially, the items are put into the get-queue
        }

        public BiLevelFIFOCollection(IEnumerable<T> initial) : this()
        {
            foreach (var i in initial)
            {
                getQueue.Enqueue(i); // initially, the items are put into the get-queue
            }
        }

        public bool TryFromGetQueue(out T next)
        {
            if (getQueue.Count == 0)
            {
                next = default(T);
                return false;
            }
            RetrievedNodes++;
            next = getQueue.Dequeue();
            return true;
        }

        public void ToPutQueue(T state) => putQueue.Enqueue(state);

        public void SwapQueues()
        {
            if (getQueue.Count > 0)
            {
                // to maintain the order (get-queue first, then put-queue)
                while (putQueue.Count > 0) getQueue.Enqueue(putQueue.Dequeue());
            } else
            {
                (getQueue, putQueue) = (putQueue, getQueue);
            }
        }

        /// <summary>
        /// This method dumps both queues into a single-level FIFO collection.
        /// The current bi-level instance does not contain any states thereafter
        /// </summary>
        /// <remarks>
        /// It maintains order in that the get-queue comes before the potential
        /// non-empty put-queue.
        /// </remarks>
        /// <returns>The regular FIFO collection with just a single queue</returns>
        public FIFOCollection<T> ToSingleLevel()
        {
            while (putQueue.Count > 0) getQueue.Enqueue(putQueue.Dequeue());
            var result = new FIFOCollection<T>(getQueue);
            getQueue = new Queue<T>();
            return result;
        }
    }

    public class PriorityBiLevelFIFOCollection<T>
    {
        public int CurrentLayerNodes => currentLayerQueue.Count;
        public int NextLayerNodes => nextLayerQueue.Count;
        public long RetrievedNodes { get; private set; }

        private Queue<T> currentLayerQueue = new Queue<T>();
        private LinkedList<(float priority, T state)> nextLayerQueue = new LinkedList<(float, T)>();

        public PriorityBiLevelFIFOCollection()
        {
            RetrievedNodes = 0;
        }

        public PriorityBiLevelFIFOCollection(T initial) : this()
        {
            currentLayerQueue.Enqueue(initial); // initially, the items are put into the get-queue
        }

        public PriorityBiLevelFIFOCollection(IEnumerable<T> initial) : this()
        {
            foreach (var i in initial)
            {
                currentLayerQueue.Enqueue(i); // initially, the items are put into the get-queue
            }
        }

        public bool TryFromCurrentLayerQueue(out T next)
        {
            if (currentLayerQueue.Count == 0)
            {
                next = default(T);
                return false;
            }
            RetrievedNodes++;
            next = currentLayerQueue.Dequeue();
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="priority"></param>
        public void ToNextLayerQueue(T state, float priority) => nextLayerQueue.AddLast((priority, state));

        /// <summary>
        /// Discards any remaining states in the current layer queue and replaces it with the
        /// <paramref name="bestN"/> best states from the next layer queue.
        /// </summary>
        /// <param name="bestN">The number of states to choose for the next layer</param>
        public void AdvanceLayer(int bestN)
        {
            currentLayerQueue.Clear();
            foreach (var (priority, state) in nextLayerQueue.OrderBy(x => x.priority).Take(bestN))
            {
                currentLayerQueue.Enqueue(state);
            }
            nextLayerQueue.Clear();
        }
    }
}