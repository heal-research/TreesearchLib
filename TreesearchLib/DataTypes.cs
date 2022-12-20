using System;
using System.Collections.Generic;

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

        public void Clear() => states.Clear();
    }
}