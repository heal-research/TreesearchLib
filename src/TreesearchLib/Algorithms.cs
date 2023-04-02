using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TreesearchLib
{
    public static class Algorithms
    {        
        /// <summary>
        /// This method performs a depth-first search starting from <paramref name="state"/>.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The initial state from which the search should start</param>
        /// <param name="depth">The current depth of the search</param>
        /// <param name="backtracks">The number of backtracks that have been performed so far</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The total number of backtracks</returns>
        public static long DepthSearch<T, Q>(ISearchControl<T, Q> control, T state, int depth,
                long backtracks, int filterWidth, int depthLimit, long backtrackLimit)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            
            var searchState = new LIFOCollection<(int depth, T state)>((depth, state));
            return DepthSearch(control, searchState, backtracks, filterWidth, depthLimit, backtrackLimit);
        }

        /// <summary>
        /// This method performs a depth-first search given the collection of states and depths.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="searchState">The search state collection which captures the visited nodes so far</param>
        /// <param name="backtracks">The number of backtracks that have been performed so far</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The total number of backtracks</returns>
        public static long DepthSearch<T, Q>(ISearchControl<T, Q> control,
                LIFOCollection<(int depth, T state)> searchState,
                long backtracks, int filterWidth, int depthLimit, long backtrackLimit)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            var lastDepth = -1;
            while (!control.ShouldStop() && backtracks < backtrackLimit && searchState.TryGetNext(out var c))
            {
                var (depth, currentState) = c;
                if (lastDepth < 0)
                {
                    lastDepth = depth;
                }
                else if (depth < lastDepth)
                {
                    backtracks++;
                }
                foreach (var next in currentState.GetBranches().Take(filterWidth).Reverse())
                {
                    if (control.VisitNode(next) == VisitResult.Discard)
                    {
                        continue;
                    }
                    if (depth + 1 < depthLimit) // do not branch further otherwise
                    {
                        searchState.Store((depth + 1, next));
                    }
                }
            }
            return backtracks;
        }

        /// <summary>
        /// This method performs a depth-first search starting from <paramref name="state"/>.
        /// </summary>
        /// <remarks>
        /// Because the state is mutable, the <paramref name="state"/> is mutated. You should
        /// consider to clone the state if a change is undesired.
        /// </remarks>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The initial state from which the search should start</param>
        /// <param name="depth">The current depth</param>
        /// <param name="backtracks">The number of backtracks that have been performed so far</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The depth of the state and the number of backtracks performed</returns>
        public static (int depth, long backtracks) DepthSearch<T, C, Q>(
                ISearchControl<T, Q> control, T state, int depth, long backtracks,
                int filterWidth, int depthLimit, long backtrackLimit)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            var searchState = new LIFOCollection<(int, C)>();
            foreach (var entry in state.GetChoices().Take(filterWidth).Reverse().Select(choice => (depth, choice)))
            {
                searchState.Store(entry);
            }
            (depth, backtracks) = DepthSearch(control, state, searchState, depth, backtracks, filterWidth, depthLimit, backtrackLimit);
            return (depth, backtracks);
        }

        /// <summary>
        /// This method performs a depth-first search starting from <paramref name="state"/>.
        /// </summary>
        /// <remarks>
        /// Because the state is mutable, the <paramref name="state"/> is mutated. You should
        /// consider to clone the state if a change is undesired.
        /// </remarks>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The initial state from which the search should start</param>
        /// <param name="searchState">The search state</param>
        /// <param name="depth">The current depth of the state</param>
        /// <param name="backtracks">The number of backtracks performed</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The depth of the state as well as the number of backtracks performed</returns>
        public static (int depth, long backtracks) DepthSearch<T, C, Q>(ISearchControl<T, Q> control, T state,
                LIFOCollection<(int, C)> searchState, int depth, long backtracks,
                int filterWidth, int depthLimit, long backtrackLimit)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            while (!control.ShouldStop() && backtracks < backtrackLimit && searchState.TryGetNext(out var next))
            {
                var (nextDepth, choice) = next;
                if (nextDepth < depth)
                {
                    backtracks++;
                    do
                    {
                        state.UndoLast();
                        depth--;
                    } while (nextDepth < depth);
                }
                state.Apply(choice);
                depth++;

                if (control.VisitNode(state) == VisitResult.Discard)
                {
                    continue;
                }

                if (depth >= depthLimit)
                {
                    continue;
                }

                foreach (var entry in state.GetChoices().Take(filterWidth).Reverse().Select(ch => (depth, ch)))
                {
                    searchState.Store(entry);
                }
            }

            return (depth, backtracks);
        }

        /// <summary>
        /// This method performs a breadth-first search starting from <paramref name="state"/>.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The initial state from which the search should start</param>
        /// <param name="depth">The current depth</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth up to which the breadth-first search expands.</param>
        /// <param name="nodesReached">Expands up to a depth with at least this many nodes.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The depth and remaining nodes (e.g., if aborted by depthLimit or nodesReached)</returns>
        public static (int depth, IStateCollection<T> states) BreadthSearch<T, Q>(ISearchControl<T, Q> control,
                T state, int depth, int filterWidth, int depthLimit, int nodesReached)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            if (depthLimit <= 0) throw new ArgumentException($"{depthLimit} needs to be breater or equal than 0", nameof(depthLimit));
            if (nodesReached <= 0) throw new ArgumentException($"{nodesReached} needs to be breater or equal than 0", nameof(nodesReached));
            var searchState = new BiLevelFIFOCollection<T>(state);
            depth = BreadthSearch(control, searchState, depth, filterWidth, depthLimit, nodesReached);
            return (depth, searchState.ToSingleLevel());
        }

        /// <summary>
        /// This method performs a breadth-first search starting from <paramref name="state"/>.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="searchState">The initial state from which the search should start</param>
        /// <param name="depth">The current depth</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth up to which the breadth-first search expands.</param>
        /// <param name="nodesReached">Expands up to a depth with at least this many nodes.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The depth reached</returns>
        public static int BreadthSearch<T, Q>(ISearchControl<T, Q> control, BiLevelFIFOCollection<T> searchState, int depth, int filterWidth, int depthLimit, int nodesReached)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            while (searchState.GetQueueNodes > 0 && depth < depthLimit && searchState.GetQueueNodes < nodesReached && !control.ShouldStop())
            {
                while (!control.ShouldStop() && searchState.TryFromGetQueue(out var currentState))
                {
                    foreach (var next in currentState.GetBranches().Take(filterWidth))
                    {
                        if (control.VisitNode(next) == VisitResult.Discard)
                        {
                            continue;
                        }

                        searchState.ToPutQueue(next);
                    }
                }
                if (searchState.GetQueueNodes == 0)
                {
                    depth++;
                    searchState.SwapQueues();
                }
            }
            return depth;
        }

        /// <summary>
        /// This method performs a breadth-first search starting from <paramref name="state"/>.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The initial state from which the search should start</param>
        /// <param name="depth">The current depth</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth up to which the breadth-first search expands.</param>
        /// <param name="nodesReached">Expands up to a depth with at least this many nodes.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The depth and remaining nodes (e.g., if aborted by depthLimit or nodesReached)</returns>
        public static (int depth, IStateCollection<T> states) BreadthSearch<T, C, Q>(ISearchControl<T, Q> control,
            T state, int depth, int filterWidth, int depthLimit, int nodesReached)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            if (depthLimit <= 0) throw new ArgumentException($"{depthLimit} needs to be breater or equal than 0", nameof(depthLimit));
            if (nodesReached <= 0) throw new ArgumentException($"{nodesReached} needs to be breater or equal than 0", nameof(nodesReached));
            var searchState = new BiLevelFIFOCollection<T>(state);
            depth = BreadthSearch<T, C, Q>(control, searchState, depth, filterWidth, depthLimit, nodesReached);
            return (depth, searchState.ToSingleLevel());
        }

        /// <summary>
        /// This method performs a breadth-first search starting from <paramref name="state"/>.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="searchState">The initial collection from which the search should start/continue</param>
        /// <param name="depth">The current depth of the search</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth up to which the breadth-first search expands.</param>
        /// <param name="nodesReached">Expands up to a depth with at least this many nodes.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The depth reached</returns>
        public static int BreadthSearch<T, C, Q>(ISearchControl<T, Q> control,
                BiLevelFIFOCollection<T> searchState, int depth, int filterWidth, int depthLimit, int nodesReached)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            while (searchState.GetQueueNodes > 0 && depth < depthLimit && searchState.GetQueueNodes < nodesReached && !control.ShouldStop())
            {
                while (!control.ShouldStop() && searchState.TryFromGetQueue(out var currentState))
                {
                    foreach (var next in currentState.GetChoices().Take(filterWidth))
                    {
                        var clone = (T)currentState.Clone();
                        clone.Apply(next);

                        if (control.VisitNode(clone) == VisitResult.Discard)
                        {
                            continue;
                        }

                        searchState.ToPutQueue(clone);
                    }
                }
                if (searchState.GetQueueNodes == 0)
                {
                    depth++;
                    searchState.SwapQueues();
                }
            }

            return depth;
        }
    }

    public static class AlgorithmExtensions
    {
        /// <summary>
        /// Performs an exhaustive depth-first search in a new Task. The search can be confined, by
        /// choosing only the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, Q>> DepthFirstAsync<T, Q>(this SearchControl<T, Q> control,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => DepthFirst(control, filterWidth, depthLimit, backtrackLimit));
        }

        /// <summary>
        /// Performs an exhaustive depth-first search. The search can be confined, by choosing only
        /// the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, Q> DepthFirst<T, Q>(this SearchControl<T, Q> control,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            Algorithms.DepthSearch(control, control.InitialState, depth: 0, backtracks: 0, filterWidth, depthLimit, backtrackLimit);
            return control;
        }

        /// <summary>
        /// Performs an exhaustive depth-first search in a new Task. The search can be confined, by
        /// choosing only the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, C, Q>> DepthFirstAsync<T, C, Q>(this SearchControl<T, C, Q> control,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => DepthFirst(control, filterWidth, depthLimit, backtrackLimit));
        }

        /// <summary>
        /// Performs an exhaustive depth-first search. The search can be confined, by choosing only
        /// the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, C, Q> DepthFirst<T, C, Q>(this SearchControl<T, C, Q> control,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            Algorithms.DepthSearch<T, C, Q>(control, state, depth: 0, backtracks: 0, filterWidth, depthLimit, backtrackLimit);
            return control;
        }

        /// <summary>
        /// Performs a depth-first search with the given options in a new Task.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static Task<TState> DepthFirstAsync<TState, TQuality>(this IState<TState, TQuality> state, int filterWidth = int.MaxValue,
                int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue, TimeSpan? runtime = null,
                QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => DepthFirst((TState)state, filterWidth, depthLimit, backtrackLimit, runtime, callback, nodeLimit, token));
        }
        /// <summary>
        /// Performs a depth-first search with the given options.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static TState DepthFirst<TState, TQuality>(this IState<TState, TQuality> state, int filterWidth = int.MaxValue,
                int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue, TimeSpan? runtime = null,
                QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (nodeLimit.HasValue) control = control.WithNodeLimit(nodeLimit.Value);
            return control.DepthFirst(filterWidth, depthLimit, backtrackLimit).BestQualityState;
        }

        /// <summary>
        /// Performs a depth-first search with the given options in a new Task.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TChoice">The choice type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static Task<TState> DepthFirstAsync<TState, TChoice, TQuality>
                (this IMutableState<TState, TChoice, TQuality> state, int filterWidth = int.MaxValue,
                int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue, TimeSpan? runtime = null,
                QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => DepthFirst<TState, TChoice, TQuality>((TState)state, filterWidth, depthLimit, backtrackLimit, runtime, callback, nodeLimit, token));
        }

        /// <summary>
        /// Performs a depth-first search with the given options.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TChoice">The choice type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static TState DepthFirst<TState, TChoice, TQuality>(
                this IMutableState<TState, TChoice, TQuality> state, int filterWidth = int.MaxValue,
                int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue, TimeSpan? runtime = null,
                QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (nodeLimit.HasValue) control = control.WithNodeLimit(nodeLimit.Value);
            return control.DepthFirst(filterWidth, depthLimit, backtrackLimit).BestQualityState;
        }

        /// <summary>
        /// Performs an exhaustive breadth-first search in a new Task. The search can be confined, by
        /// choosing only the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, Q>> BreadthFirstAsync<T, Q>(this SearchControl<T, Q> control,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BreadthFirst(control, filterWidth, depthLimit));
        }

        /// <summary>
        /// Performs an exhaustive breadth-first search. The search can be confined, by choosing only
        /// the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, Q> BreadthFirst<T, Q>(this SearchControl<T, Q> control,
                int filterWidth = int.MaxValue, int depthlimit = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            Algorithms.BreadthSearch(control, control.InitialState, depth: 0, filterWidth, depthlimit, int.MaxValue);
            return control;
        }

        /// <summary>
        /// Performs an exhaustive breadth-first search in a new Task. The search can be confined, by
        /// choosing only the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, C, Q>> BreadthFirstAsync<T, C, Q>(this SearchControl<T, C, Q> control,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BreadthFirst<T, C, Q>(control, filterWidth, depthLimit));
        }

        /// <summary>
        /// Performs an exhaustive breadth-first search. The search can be confined, by choosing only
        /// the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, C, Q> BreadthFirst<T, C, Q>(this SearchControl<T, C, Q> control,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            Algorithms.BreadthSearch<T, C, Q>(control, state, 0, filterWidth, depthLimit, int.MaxValue);
            return control;
        }

        /// <summary>
        /// Performs a breadth-first search with the given options in a new Task.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static Task<TState> BreadthFirstAsync<TState, TQuality>(this IState<TState, TQuality> state,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => BreadthFirst((TState)state, filterWidth, depthLimit, runtime, callback, nodeLimit, token));
        }

        /// <summary>
        /// Performs a breadth-first search with the given options.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static TState BreadthFirst<TState, TQuality>(this IState<TState, TQuality> state,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (nodeLimit.HasValue) control = control.WithNodeLimit(nodeLimit.Value);
            return control.BreadthFirst(filterWidth, depthLimit).BestQualityState;
        }

        /// <summary>
        /// Performs a breadth-first search with the given options in a new Task.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TChoice">The choice type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static Task<TState> BreadthFirstAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => BreadthFirst<TState, TChoice, TQuality>((TState)state, filterWidth, depthLimit, runtime, callback, nodeLimit, token));
        }

        /// <summary>
        /// Performs a breadth-first search with the given options.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TChoice">The choice type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static TState BreadthFirst<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (nodeLimit.HasValue) control = control.WithNodeLimit(nodeLimit.Value);
            return control.BreadthFirst(filterWidth, depthLimit).BestQualityState;
        }
    }
}