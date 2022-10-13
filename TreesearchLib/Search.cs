using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

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

    public delegate void QualityCallback<TQuality>(SearchControl<TQuality> control, TQuality quality) where TQuality : struct, IQuality<TQuality>;

    public class SearchControl<TQuality> where TQuality : struct, IQuality<TQuality>
    {
        private SearchControl()
        {
            stopwatch = Stopwatch.StartNew();
            DepthLimit = int.MaxValue;
            BreadthLimit = int.MaxValue;
            BeamWidth = int.MaxValue;
            UpperBound = null;
            Cancellation = CancellationToken.None;
            Runtime = TimeSpan.MaxValue;
            nodesVisited = 0;
            TotalNodesVisited = 0;
        }

        private int nodesVisited;
        private Stopwatch stopwatch;
        
        public QualityCallback<TQuality> ImprovementCallback { get; set; }

        public TimeSpan Elapsed => stopwatch.Elapsed;
        public TimeSpan Runtime { get; set; }
        public int DepthLimit { get; set; }
        public int BreadthLimit { get; set; }
        public int BeamWidth { get; set; }
        public TQuality? UpperBound { get; set; }
        public int TotalNodesVisited { get; private set; }
        public CancellationToken Cancellation { get; set; }

        public bool IsFinished => !stopwatch.IsRunning;

        public void VisitNode()
        {
            nodesVisited++;
        }

        public SearchControl<TQuality> WithNodeCount(int initialVisitedNodes = 0)
        {
            TotalNodesVisited += nodesVisited;
            nodesVisited = initialVisitedNodes;
            return this;
        }

        public SearchControl<TQuality> Finish()
        {
            stopwatch.Stop();
            TotalNodesVisited += nodesVisited;
            return this;
        }

        public bool ShouldStop(SearchType searchType)
        {
            if (IsFinished || Cancellation.IsCancellationRequested || stopwatch.Elapsed > Runtime)
            {
                return true;
            }

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

            return false;
        }

        public void FoundSolution(TQuality quality)
        {
            if (quality.IsBetter(UpperBound))
            {
                UpperBound = quality;
                ImprovementCallback?.Invoke(this, quality);
            }
        }

        public static SearchControl<TQuality> Start()
        {
            return new SearchControl<TQuality>();
        }
    }

    public static class SearchControlExtensions
    {
        public static SearchControl<TQuality> WithImprovementCallback<TQuality>(this SearchControl<TQuality> control, QualityCallback<TQuality> callback)
            where TQuality : struct, IQuality<TQuality>
        {
            control.ImprovementCallback = callback;
            return control;
        }

        public static SearchControl<TQuality> WithCancellationToken<TQuality>(this SearchControl<TQuality> control, CancellationToken token)
            where TQuality : struct, IQuality<TQuality>
        {
            control.Cancellation = token;
            return control;
        }

        public static SearchControl<TQuality> WithUpperBound<TQuality>(this SearchControl<TQuality> control, TQuality upperBound)
            where TQuality : struct, IQuality<TQuality>
        {
            control.UpperBound = upperBound;
            return control;
        }

        public static SearchControl<TQuality> WithRuntimeLimit<TQuality>(this SearchControl<TQuality> control, TimeSpan runtime)
            where TQuality : struct, IQuality<TQuality>
        {
            control.Runtime = runtime;
            return control;
        }

        public static SearchControl<TQuality> WithBreadthLimit<TQuality>(this SearchControl<TQuality> control, int breadthLimit)
            where TQuality : struct, IQuality<TQuality>
        {
            control.BreadthLimit = breadthLimit;
            return control;
        }

        public static SearchControl<TQuality> WithDepthLimit<TQuality>(this SearchControl<TQuality> control, int depthLimit)
            where TQuality : struct, IQuality<TQuality>
        {
            control.DepthLimit = depthLimit;
            return control;
        }

        public static SearchControl<TQuality> WithBeamWidth<TQuality>(this SearchControl<TQuality> control, int beamWidth)
            where TQuality : struct, IQuality<TQuality>
        {
            control.BeamWidth = beamWidth;
            return control;
        }

        private static SearchControl<TQuality> DoSearchDepthFirst<TSearchable, TChoice, TQuality>(SearchControl<TQuality> control, TSearchable state, ref TSearchable bestState)
            where TQuality : struct, IQuality<TQuality>
            where TSearchable : class, ISearchable<TChoice, TQuality>
        {
            if (state is ISearchableWithUndo<TChoice, TQuality> stateWithUndo)
            {
                var bestStateWithUndo = (ISearchableWithUndo<TChoice, TQuality>)bestState;
                Searcher.SearchWithUndo<ISearchableWithUndo<TChoice, TQuality>, TChoice, TQuality>(stateWithUndo, ref bestStateWithUndo, control);
                bestState = (TSearchable)bestStateWithUndo;
            } else
            {
                var searchState = new DFSState<TSearchable>(state);
                Searcher.Search<TSearchable, TChoice, TQuality>(searchState, ref bestState, control);
            }
            return control;
        }

        public static SearchControl<TQuality> SearchDepthFirstAndContinue<TSearchable, TChoice, TQuality>(this SearchControl<TQuality> control, TSearchable state, ref TSearchable bestState)
            where TQuality : struct, IQuality<TQuality>
            where TSearchable : class, ISearchable<TChoice, TQuality>
        {
            return DoSearchDepthFirst<TSearchable, TChoice, TQuality>(control, state, ref bestState);
        }

        public static SearchControl<TQuality> SearchDepthFirst<TSearchable, TChoice, TQuality>(this SearchControl<TQuality> control, TSearchable state, ref TSearchable bestState)
            where TQuality : struct, IQuality<TQuality>
            where TSearchable : class, ISearchable<TChoice, TQuality>
        {
            return SearchDepthFirstAndContinue<TSearchable, TChoice, TQuality>(control, state, ref bestState).Finish();
        }

        public static SearchControl<TQuality> SearchDepthFirstAndContinue<TSearchable, TChoice, TQuality>(this SearchControl<TQuality> control, TSearchable state, ref TSearchable bestState, bool utilizeUndo = true)
            where TQuality : struct, IQuality<TQuality>
            where TSearchable : class, ISearchableWithUndo<TChoice, TQuality>
        {
            if (!utilizeUndo)
            {
                var searchState = new DFSState<TSearchable>(state);
                Searcher.Search<TSearchable, TChoice, TQuality>(searchState, ref bestState, control);
            } else
            {
                Searcher.SearchWithUndo<TSearchable, TChoice, TQuality>(state, ref bestState, control);
            }
            return control;
        }

        public static SearchControl<TQuality> SearchDepthFirst<TSearchable, TChoice, TQuality>(this SearchControl<TQuality> control, TSearchable state, ref TSearchable bestState, bool utilizeUndo = true)
            where TQuality : struct, IQuality<TQuality>
            where TSearchable : class, ISearchableWithUndo<TChoice, TQuality>
        {
            return SearchDepthFirstAndContinue<TSearchable, TChoice, TQuality>(control, state, ref bestState, utilizeUndo).Finish();
        }

        public static SearchControl<Minimize> SearchDepthFirstAndContinue<TSearchable, TChoice>(this SearchControl<Minimize> control, TSearchable state, ref TSearchable bestState)
            where TSearchable : class, ISearchable<TChoice, Minimize>
        {
            return SearchDepthFirstAndContinue<TSearchable, TChoice, Minimize>(control, state, ref bestState);
        }

        public static SearchControl<Minimize> SearchDepthFirstAndContinue<TSearchable, TChoice>(this SearchControl<Minimize> control, TSearchable state, ref TSearchable bestState, bool utilizeUndo = true)
            where TSearchable : class, ISearchableWithUndo<TChoice, Minimize>
        {
            return SearchDepthFirstAndContinue<TSearchable, TChoice, Minimize>(control, state, ref bestState, utilizeUndo);
        }

        public static SearchControl<Minimize> SearchDepthFirst<TSearchable, TChoice>(this SearchControl<Minimize> control, TSearchable state, ref TSearchable bestState)
            where TSearchable : class, ISearchable<TChoice, Minimize>
        {
            return SearchDepthFirst<TSearchable, TChoice, Minimize>(control, state, ref bestState);
        }

        public static SearchControl<Minimize> SearchDepthFirst<TSearchable, TChoice>(this SearchControl<Minimize> control, TSearchable state, ref TSearchable bestState, bool utilizeUndo = true)
            where TSearchable : class, ISearchableWithUndo<TChoice, Minimize>
        {
            return SearchDepthFirst<TSearchable, TChoice, Minimize>(control, state, ref bestState, utilizeUndo);
        }

        public static SearchControl<Maximize> SearchDepthFirstAndContinue<TSearchable, TChoice>(this SearchControl<Maximize> control, TSearchable state, ref TSearchable bestState)
            where TSearchable : class, ISearchable<TChoice, Maximize>
        {
            return SearchDepthFirstAndContinue<TSearchable, TChoice, Maximize>(control, state, ref bestState);
        }

        public static SearchControl<Maximize> SearchDepthFirstAndContinue<TSearchable, TChoice>(this SearchControl<Maximize> control, TSearchable state, ref TSearchable bestState, bool utilizeUndo = true)
            where TSearchable : class, ISearchableWithUndo<TChoice, Maximize>
        {
            return SearchDepthFirstAndContinue<TSearchable, TChoice, Maximize>(control, state, ref bestState, utilizeUndo);
        }

        public static SearchControl<Maximize> SearchDepthFirst<TSearchable, TChoice>(this SearchControl<Maximize> control, TSearchable state, ref TSearchable bestState)
            where TSearchable : class, ISearchable<TChoice, Maximize>
        {
            return SearchDepthFirst<TSearchable, TChoice, Maximize>(control, state, ref bestState);
        }

        public static SearchControl<Maximize> SearchDepthFirst<TSearchable, TChoice>(this SearchControl<Maximize> control, TSearchable state, ref TSearchable bestState, bool utilizeUndo = true)
            where TSearchable : class, ISearchableWithUndo<TChoice, Maximize>
        {
            return SearchDepthFirst<TSearchable, TChoice, Maximize>(control, state, ref bestState, utilizeUndo);
        }

        public static SearchControl<TQuality> SearchBreadthFirstAndContinue<TSearchable, TChoice, TQuality>(this SearchControl<TQuality> control, TSearchable state, ref TSearchable bestState)
            where TSearchable : class, ISearchable<TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var searchState = new BFSState<TSearchable>(state);
            Searcher.Search<TSearchable, TChoice, TQuality>(searchState, ref bestState, control);
            return control;
        }

        public static SearchControl<TQuality> SearchBreadthFirst<TSearchable, TChoice, TQuality>(this SearchControl<TQuality> control, TSearchable state, ref TSearchable bestState)
            where TSearchable : class, ISearchable<TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return SearchBreadthFirstAndContinue<TSearchable, TChoice, TQuality>(control, state, ref bestState).Finish();
        }

        public static SearchControl<Minimize> SearchBreadthFirstAndContinue<TSearchable, TChoice>(this SearchControl<Minimize> control, TSearchable state, ref TSearchable bestState)
            where TSearchable : class, ISearchable<TChoice, Minimize>
        {
            return SearchBreadthFirstAndContinue<TSearchable, TChoice, Minimize>(control, state, ref bestState);
        }

        public static SearchControl<Minimize> SearchBreadthFirst<TSearchable, TChoice>(this SearchControl<Minimize> control, TSearchable state, ref TSearchable bestState)
            where TSearchable : class, ISearchable<TChoice, Minimize>
        {
            return SearchBreadthFirst<TSearchable, TChoice, Minimize>(control, state, ref bestState);
        }

        public static SearchControl<Maximize> SearchBreadthFirstAndContinue<TSearchable, TChoice>(this SearchControl<Maximize> control, TSearchable state, ref TSearchable bestState)
            where TSearchable : class, ISearchable<TChoice, Maximize>
        {
            return SearchBreadthFirstAndContinue<TSearchable, TChoice, Maximize>(control, state, ref bestState);
        }

        public static SearchControl<Maximize> SearchBreadthFirst<TSearchable, TChoice>(this SearchControl<Maximize> control, TSearchable state, ref TSearchable bestState)
            where TSearchable : class, ISearchable<TChoice, Maximize>
        {
            return SearchBreadthFirst<TSearchable, TChoice, Maximize>(control, state, ref bestState);
        }

        public static SearchControl<TQuality> SearchBreadthFirstThenDepthAndContinue<TSearchable, TChoice, TQuality>(this SearchControl<TQuality> control, TSearchable state, ref TSearchable bestState)
            where TSearchable : class, ISearchable<TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var searchState = new BFSState<TSearchable>(state);
            Searcher.Search<TSearchable, TChoice, TQuality>(searchState, ref bestState, control);
            while (searchState.TryGetNext(out var next))
            {
                control.WithNodeCount(0);
                DoSearchDepthFirst<TSearchable, TChoice, TQuality>(control, next, ref bestState);
            }
            return control;
        }

        public static SearchControl<TQuality> SearchBreadthFirstThenDepth<TSearchable, TChoice, TQuality>(this SearchControl<TQuality> control, TSearchable state, ref TSearchable bestState)
            where TSearchable : class, ISearchable<TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return SearchBreadthFirstThenDepthAndContinue<TSearchable, TChoice, TQuality>(control, state, ref bestState).Finish();
        }

        public static SearchControl<Minimize> SearchBreadthFirstThenDepthAndContinue<TSearchable, TChoice>(this SearchControl<Minimize> control, TSearchable state, ref TSearchable bestState)
            where TSearchable : class, ISearchable<TChoice, Minimize>
        {
            return SearchBreadthFirstThenDepthAndContinue<TSearchable, TChoice, Minimize>(control, state, ref bestState);
        }

        public static SearchControl<Minimize> SearchBreadthFirstThenDepth<TSearchable, TChoice>(this SearchControl<Minimize> control, TSearchable state, ref TSearchable bestState)
            where TSearchable : class, ISearchable<TChoice, Minimize>
        {
            return SearchBreadthFirstThenDepth<TSearchable, TChoice, Minimize>(control, state, ref bestState);
        }

        public static SearchControl<Maximize> SearchBreadthFirstThenDepthAndContinue<TSearchable, TChoice>(this SearchControl<Maximize> control, TSearchable state, ref TSearchable bestState)
            where TSearchable : class, ISearchable<TChoice, Maximize>
        {
            return SearchBreadthFirstThenDepthAndContinue<TSearchable, TChoice, Maximize>(control, state, ref bestState);
        }

        public static SearchControl<Maximize> SearchBreadthFirstThenDepth<TSearchable, TChoice>(this SearchControl<Maximize> control, TSearchable state, ref TSearchable bestState)
            where TSearchable : class, ISearchable<TChoice, Maximize>
        {
            return SearchBreadthFirstThenDepth<TSearchable, TChoice, Maximize>(control, state, ref bestState);
        }
    }
}