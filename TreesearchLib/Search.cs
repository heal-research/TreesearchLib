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

        public DFSState()
        {

        }

        public DFSState(T initial)
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

        public int Nodes() => states.Count;

        public SearchType SearchType => SearchType.Depth;

        public void Store(T state) => states.Push(state);
    }

    public class BFSState<T> : ISearchState<T>
    {
        private Queue<T> states = new Queue<T>();

        public BFSState()
        {

        }

        public BFSState(T initial)
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
            next = states.Dequeue();
            return true;
        }

        public int Nodes() => states.Count;

        public SearchType SearchType => SearchType.Depth;

        public void Store(T state) => states.Enqueue(state);
    }


    public class SearchLimits<TQuality> where TQuality : struct, IQuality<TQuality> // TODO: cancelationtoken
    {
        private SearchLimits(TQuality upperBound)
        {
            Started = DateTime.Now;
            Deadline = DateTime.MaxValue;
            DepthLimit = int.MaxValue;
            BreadthLimit = int.MaxValue;
            BeamWidth = int.MaxValue;
            UpperBound = upperBound;
            nodesVisited = 0;
            totalNodesVisited = 0;
        }
        public SearchLimits(TQuality upperBound, TimeSpan maxRuntime) : this(upperBound)
        {
            Deadline = Started + maxRuntime;

        }
        public SearchLimits(TQuality upperBound, int depthLimit, int breadthLimit) : this(upperBound)
        {
            DepthLimit = depthLimit;
            BreadthLimit = breadthLimit;
        }
        public DateTime Started { get; }

        public DateTime Deadline { get; }
        public int DepthLimit { get; set; }
        public int BreadthLimit { get; set; }
        public int BeamWidth { get; set; }
        public TQuality UpperBound { get; set; }
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

        public bool ShouldStop(SearchType searchType)
        {
            var limit = 0;
            switch (searchType)
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

        public void FoundSolution(TQuality quality)
        {
            if (quality.IsBetter(UpperBound))
            {
                UpperBound = quality;
                Console.WriteLine($"Found new best solution with {quality} after {DateTime.Now - Started}");
            }
        }

        public static SearchLimits<TQuality> WithUpperBound(TQuality upperBound)
        {
            return new SearchLimits<TQuality>(upperBound);
        }

        public SearchLimits<TQuality> SearchDepthFirst<T, TChoice>(T state, ref T bestState)
        where T : class, ISearchable<TChoice, TQuality>, ICloneable
        {
            var searchState = new DFSState<T>(state);
            Searcher.Search<T, TChoice, TQuality>(searchState, ref bestState, this);
            return this;
        }

        public SearchLimits<TQuality> SearchBreadthFirst<T, TChoice>(T state, ref T bestState)
        where T : class, ISearchable<TChoice, TQuality>, ICloneable
        {
            var searchState = new BFSState<T>(state);
            Searcher.Search<T, TChoice, TQuality>(searchState, ref bestState, this);
            return this;
        }

        public SearchLimits<TQuality> SearchWithUndo<T, TChoice>(T state, ref T bestState)
        where T : class, ISearchableWithUndo<TChoice, TQuality>, ICloneable
        {
            Searcher.SearchWithUndo<T, TChoice, TQuality>(state, ref bestState, this);
            return this;
        }

        //public SearchLimits<TQuality> SearchBreadthFirstThenDepth<T, TChoice>(T state, ref T bestState, int depthLimit, int breadthLimit)
        //where T : class, ISearchable<TChoice, TQuality>, ICloneable
        //{

        //    var searchState = new BFSState<T>(state);
        //    breadthLimit = breadthLimit;
        //    Searcher.Search<T, TChoice, TQuality>(searchState, ref bestState, this);

        //    while (searchState.TryGetNext(out var next))
        //    {
        //        var dfsState = new DFSState<T>(state);
        //        Searcher.Search<T, TChoice, TQuality>(dfsState, ref bestState, this);
        //    }
        //    return this;
        //}
    }
}