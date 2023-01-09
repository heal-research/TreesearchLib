using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TreesearchLib
{
    public interface IStateCollection<TState>
    {
        int Nodes { get; }
        long RetrievedNodes { get; }

        bool TryGetNext(out TState value);
        void Store(TState state);
    }

    public class LIFOCollection<TState> : IStateCollection<TState>
    {
        public int Nodes => states.Count;
        public long RetrievedNodes { get; private set; }

        private Stack<TState> states = new Stack<TState>();

        public LIFOCollection()
        {
            RetrievedNodes = 0;
        }

        public LIFOCollection(TState initial) : this()
        {
            Store(initial);
        }

        public bool TryGetNext(out TState next)
        {
            if (states.Count == 0)
            {
                next = default(TState);
                return false;
            }
            RetrievedNodes++;
            next = states.Pop();
            return true;
        }

        public void Store(TState state) => states.Push(state);
    }

    public class FIFOCollection<TState> : IStateCollection<TState>
    {
        public int Nodes => states.Count;
        public long RetrievedNodes { get; private set; }

        private Queue<TState> states = new Queue<TState>();

        public FIFOCollection()
        {
            RetrievedNodes = 0;
        }

        public FIFOCollection(TState initial) : this()
        {
            Store(initial);
        }

        internal FIFOCollection(Queue<TState> other, long retrievedNodes)
        {
            RetrievedNodes = retrievedNodes;
            states = other;
        }

        public bool TryGetNext(out TState next)
        {
            if (states.Count == 0)
            {
                next = default(TState);
                return false;
            }
            RetrievedNodes++;
            next = states.Dequeue();
            return true;
        }

        public void Store(TState state) => states.Enqueue(state);
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
/// <see cref="IStateCollection{TState}"/>. It allows to "export" as a regular
/// <see cref="FIFOCollection{TState}"/>, by calling <see cref="ToSingleLevel"/>.
/// </remarks>
/// <typeparam name="TState"></typeparam>
    public class BiLevelFIFOCollection<TState>
    {
        public int GetQueueNodes => getQueue.Count;
        public int PutQueueNodes => putQueue.Count;
        public long RetrievedNodes { get; private set; }

        private Queue<TState> getQueue = new Queue<TState>();
        private Queue<TState> putQueue = new Queue<TState>();

        public BiLevelFIFOCollection()
        {
            RetrievedNodes = 0;
        }

        public BiLevelFIFOCollection(TState initial) : this()
        {
            getQueue.Enqueue(initial); // initially, the items are put into the get-queue
        }

        public BiLevelFIFOCollection(IEnumerable<TState> initial) : this()
        {
            foreach (var i in initial)
            {
                getQueue.Enqueue(i); // initially, the items are put into the get-queue
            }
        }

        public bool TryFromGetQueue(out TState next)
        {
            if (getQueue.Count == 0)
            {
                next = default(TState);
                return false;
            }
            RetrievedNodes++;
            next = getQueue.Dequeue();
            return true;
        }

        public void ToPutQueue(TState state) => putQueue.Enqueue(state);

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
        public FIFOCollection<TState> ToSingleLevel()
        {
            while (putQueue.Count > 0) getQueue.Enqueue(putQueue.Dequeue());
            var result = new FIFOCollection<TState>(getQueue, RetrievedNodes);
            getQueue = new Queue<TState>();
            return result;
        }
    }
}