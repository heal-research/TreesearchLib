using System;
using System.Collections.Generic;

namespace TreesearchLib
{
    public enum SearchType
    {
        Breadth, Depth
    }
    public interface ISearchState<T>
    {
        bool TryGetNext(out T value);
        void Store(T state);
        int Nodes();

        SearchType SearchType { get; }
    }

    public class DFSState<T> : ISearchState<T>
    {
        private Stack<T> states = new Stack<T>();

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

        public int Nodes() => states.Count;

        public SearchType SearchType => SearchType.Depth;

        public void Store(T state) => states.Push(state);
    }

    public class BFSState<T> : ISearchState<T>
    {
        private Queue<T> states = new Queue<T>();

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

        public int Nodes() => states.Count;

        public SearchType SearchType => SearchType.Depth;

        public void Store(T state) => states.Enqueue(state);
    }


    public class SearchLimits
    {
        public SearchLimits()
        {
            Started = DateTime.Now;
            Deadline = DateTime.MaxValue;
            DepthLimit = int.MaxValue;
            BreadthLimit = int.MaxValue;
            BeamWidth = int.MaxValue;
            UpperBound = new Quality(int.MaxValue);
            nodesVisited = 0;
            totalNodesVisited = 0;
        }
        public SearchLimits(TimeSpan maxRuntime) : this()
        {
            Deadline = Started + maxRuntime;

        }
        public SearchLimits(int depthLimit, int breadthLimit) : this()
        {
            DepthLimit = depthLimit;
            BreadthLimit = breadthLimit;
        }
        public DateTime Started { get; }

        public DateTime Deadline { get; }
        public int DepthLimit { get; set; }
        public int BreadthLimit { get; set; }
        public int BeamWidth { get; set; }
        public Quality UpperBound { get; set; }
        int nodesVisited;
        int totalNodesVisited;
        public void ResetVisitedNodes(int initial)
        {
            totalNodesVisited += nodesVisited;
            nodesVisited = initial;
        }

        public void VisitNode()
        {
            nodesVisited += 1;
        }

        public bool ShouldStop(SearchType ty)
        {
            var limit = 0;
            switch (ty)
            {
                case SearchType.Depth:
                    limit = DepthLimit;
                    break;
                case SearchType.Breadth:
                    limit = BreadthLimit;
                    break;
            }

            if (nodesVisited > limit)
            {
                return true;
            }

            if (DateTime.Now > Deadline)
            {
                return true;
            }
            return false;
        }

        public void FoundSolution(Quality quality)
        {
            if (quality.IsBetter(UpperBound))
            {
                UpperBound = quality;
                Console.WriteLine($"Found new best solution with {quality} after {DateTime.Now - Started}");
            }
        }
    }
}