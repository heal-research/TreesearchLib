using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TreesearchLib
{
    public static class Algorithms
    {
        /// <summary>
        /// Performs an exhaustive depth-first search in a new Task. The search can be confined, by
        /// choosing only the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, Q>> DepthFirstAsync<T, Q>(this SearchControl<T, Q> control, int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => DepthFirst(control, filterWidth: filterWidth, depthLimit: depthLimit));
        }

        /// <summary>
        /// Performs an exhaustive depth-first search. The search can be confined, by choosing only
        /// the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, Q> DepthFirst<T, Q>(this SearchControl<T, Q> control, int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            DoDepthSearch(control, control.InitialState, filterWidth, depthLimit);
            return control;
        }

        /// <summary>
        /// Performs an exhaustive breadth-first search in a new Task. The search can be confined, by
        /// choosing only the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, Q>> BreadthFirstAsync<T, Q>(this SearchControl<T, Q> control, int filterWidth = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BreadthFirst(control, filterWidth));
        }

        /// <summary>
        /// Performs an exhaustive breadth-first search. The search can be confined, by choosing only
        /// the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, Q> BreadthFirst<T, Q>(this SearchControl<T, Q> control, int filterWidth = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            DoBreadthSearch(control, control.InitialState, filterWidth, int.MaxValue, int.MaxValue);
            return control;
        }

        /// <summary>
        /// Performs an exhaustive depth-first search in a new Task. The search can be confined, by
        /// choosing only the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, C, Q>> DepthFirstAsync<T, C, Q>(this SearchControl<T, C, Q> control, int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => DepthFirst(control, filterWidth, depthLimit));
        }

        /// <summary>
        /// Performs an exhaustive depth-first search. The search can be confined, by choosing only
        /// the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, C, Q> DepthFirst<T, C, Q>(this SearchControl<T, C, Q> control, int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            DoDepthSearch<T, C, Q>(control, state, filterWidth, depthLimit);
            return control;
        }

        /// <summary>
        /// Performs an exhaustive breadth-first search in a new Task. The search can be confined, by
        /// choosing only the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, C, Q>> BreadthFirstAsync<T, C, Q>(this SearchControl<T, C, Q> control, int filterWidth = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BreadthFirst<T, C, Q>(control, filterWidth));
        }

        /// <summary>
        /// Performs an exhaustive breadth-first search. The search can be confined, by choosing only
        /// the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, C, Q> BreadthFirst<T, C, Q>(this SearchControl<T, C, Q> control, int filterWidth = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            DoBreadthSearch<T, C, Q>(control, state, filterWidth, int.MaxValue, int.MaxValue);
            return control;
        }
        
        /// <summary>
        /// This method performs a depth-first search starting from <paramref name="state"/>.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The initial state from which the search should start</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns></returns>
        public static void DoDepthSearch<T, Q>(ISearchControl<T, Q> control, T state, int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            var searchState = new LIFOCollection<(int depth, T state)>((0, state));
            while (searchState.TryGetNext(out var c) && !control.ShouldStop())
            {
                var (depth, currentState) = c;
                foreach (var next in currentState.GetBranches().Reverse().Take(filterWidth))
                {
                    if (control.VisitNode(next) == VisitResult.Discard)
                    {
                        continue;
                    }
                    if (depth + 1 < depthLimit) // do not branch further
                    {
                        searchState.Store((depth + 1, next));
                    }
                }
            }
        }

        /// <summary>
        /// This method performs a breadth-first search starting from <paramref name="state"/>.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The initial state from which the search should start</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth up to which the breadth-first search expands.</param>
        /// <param name="nodesReached">Expands up to a depth with at least this many nodes.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The remaining nodes (e.g., if aborted by depthLimit or nodesReached)</returns>
        public static IStateCollection<T> DoBreadthSearch<T, Q>(ISearchControl<T, Q> control, T state, int filterWidth, int depthLimit, int nodesReached)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            if (depthLimit <= 0) throw new ArgumentException($"{depthLimit} needs to be breater or equal than 0", nameof(depthLimit));
            if (nodesReached <= 0) throw new ArgumentException($"{nodesReached} needs to be breater or equal than 0", nameof(nodesReached));
            var searchState = new BiLevelFIFOCollection<T>(state);
            var depth = 0;
            while (searchState.GetQueueNodes > 0 && depth < depthLimit && searchState.GetQueueNodes < nodesReached && !control.ShouldStop())
            {
                while (searchState.TryFromGetQueue(out var currentState) && !control.ShouldStop())
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
                depth++;
                searchState.SwapQueues();
            }
            return searchState.ToSingleLevel();
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
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The depth of the state in number of moves applied from the initial given state (assumed depth 0)</returns>
        public static int DoDepthSearch<T, C, Q>(ISearchControl<T, Q> control, T state, int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            var searchState = new LIFOCollection<(int, C)>();
            var stateDepth = 0;
            foreach (var entry in state.GetChoices().Take(filterWidth).Reverse().Select(choice => (stateDepth, choice)))
            {
                searchState.Store(entry);
            }

            while (searchState.TryGetNext(out var next) && !control.ShouldStop())
            {
                var (depth, choice) = next;
                while (depth < stateDepth)
                {
                    state.UndoLast();
                    stateDepth--;
                }
                state.Apply(choice);
                stateDepth++;

                if (control.VisitNode(state) == VisitResult.Discard)
                {
                    continue;
                }

                if (stateDepth >= depthLimit)
                {
                    continue;
                }

                foreach (var entry in state.GetChoices().Take(filterWidth).Reverse().Select(ch => (stateDepth, ch)))
                {
                    searchState.Store(entry);
                }
            }
            return stateDepth;
        }

        /// <summary>
        /// This method performs a breadth-first search starting from <paramref name="state"/>.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The initial state from which the search should start</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth up to which the breadth-first search expands.</param>
        /// <param name="nodesReached">Expands up to a depth with at least this many nodes.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The remaining nodes (e.g., if aborted by depthLimit or nodesReached)</returns>
        public static IStateCollection<T> DoBreadthSearch<T, C, Q>(ISearchControl<T, Q> control, T state, int filterWidth, int depthLimit, int nodesReached)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            if (depthLimit <= 0) throw new ArgumentException($"{depthLimit} needs to be breater or equal than 0", nameof(depthLimit));
            if (nodesReached <= 0) throw new ArgumentException($"{nodesReached} needs to be breater or equal than 0", nameof(nodesReached));
            var searchState = new BiLevelFIFOCollection<T>(state);
            var depth = 0;
            while (searchState.GetQueueNodes > 0 && depth < depthLimit && searchState.GetQueueNodes < nodesReached && !control.ShouldStop())
            {
                while (searchState.TryFromGetQueue(out var currentState) && !control.ShouldStop())
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
                depth++;
                searchState.SwapQueues();
            }
            return searchState.ToSingleLevel();
        }
    }

    public static class AlgorithmStateExtensions
    {
        /// <summary>
        /// Performs a depth-first search with the given options in a new Task.
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
        public static Task<TState> DepthFirstAsync<TState, TQuality>(this IState<TState, TQuality> state, int filterWidth = int.MaxValue,
                int depthLimit = int.MaxValue, TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => DepthFirst((TState)state, filterWidth, depthLimit, runtime, callback, nodeLimit, token));
        }
        /// <summary>
        /// Performs a depth-first search with the given options.
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
        public static TState DepthFirst<TState, TQuality>(this IState<TState, TQuality> state, int filterWidth = int.MaxValue,
                int depthLimit = int.MaxValue, TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (nodeLimit.HasValue) control = control.WithNodeLimit(nodeLimit.Value);
            return control.DepthFirst(filterWidth, depthLimit).BestQualityState;
        }

        /// <summary>
        /// Performs a depth-first search with the given options in a new Task.
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
        public static Task<TState> DepthFirstAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int filterWidth = int.MaxValue,
                int depthLimit = int.MaxValue, TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => DepthFirst<TState, TChoice, TQuality>((TState)state, filterWidth, depthLimit, runtime, callback, nodeLimit, token));
        }

        /// <summary>
        /// Performs a depth-first search with the given options.
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
        public static TState DepthFirst<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int filterWidth = int.MaxValue,
                int depthLimit = int.MaxValue, TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (nodeLimit.HasValue) control = control.WithNodeLimit(nodeLimit.Value);
            return control.DepthFirst(filterWidth, depthLimit).BestQualityState;
        }

        /// <summary>
        /// Performs a breadth-first search with the given options in a new Task.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static Task<TState> BreadthFirstAsync<TState, TQuality>(this IState<TState, TQuality> state, int filterWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => BreadthFirst((TState)state, filterWidth, runtime, callback, nodeLimit, token));
        }

        /// <summary>
        /// Performs a breadth-first search with the given options.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static TState BreadthFirst<TState, TQuality>(this IState<TState, TQuality> state, int filterWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (nodeLimit.HasValue) control = control.WithNodeLimit(nodeLimit.Value);
            return control.BreadthFirst(filterWidth).BestQualityState;
        }

        /// <summary>
        /// Performs a breadth-first search with the given options in a new Task.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TChoice">The choice type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static Task<TState> BreadthFirstAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int filterWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => BreadthFirst<TState, TChoice, TQuality>((TState)state, filterWidth, runtime, callback, nodeLimit, token));
        }

        /// <summary>
        /// Performs a breadth-first search with the given options.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TChoice">The choice type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static TState BreadthFirst<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int filterWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (nodeLimit.HasValue) control = control.WithNodeLimit(nodeLimit.Value);
            return control.BreadthFirst(filterWidth).BestQualityState;
        }
    }
}