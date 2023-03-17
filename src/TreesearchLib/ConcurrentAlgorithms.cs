using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TreesearchLib
{
    public static class ConcurrentAlgorithms
    {
        /// <summary>
        /// Performs an exhaustive depth-first search in a new Task. The search can be confined, by
        /// choosing only the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, Q>> ParallelDepthFirstAsync<T, Q>(this SearchControl<T, Q> control,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue,
            int maxDegreeOfParallelism = 4)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelDepthFirst(control, filterWidth: filterWidth, depthLimit: depthLimit, maxDegreeOfParallelism: maxDegreeOfParallelism));
        }

        /// <summary>
        /// Performs an exhaustive depth-first search. The search can be confined, by choosing only
        /// the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, Q> ParallelDepthFirst<T, Q>(this SearchControl<T, Q> control,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue,
            int maxDegreeOfParallelism = 4)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            DoParallelDepthSearch(control, control.InitialState, filterWidth, depthLimit);
            return control;
        }

        /// <summary>
        /// Performs an exhaustive breadth-first search in a new Task. The search can be confined, by
        /// choosing only the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, Q>> ParallelBreadthFirstAsync<T, Q>(this SearchControl<T, Q> control,
            int filterWidth = int.MaxValue, int maxDegreeOfParallelism = 4)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelBreadthFirst(control, filterWidth, maxDegreeOfParallelism));
        }

        /// <summary>
        /// Performs an exhaustive breadth-first search. The search can be confined, by choosing only
        /// the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, Q> ParallelBreadthFirst<T, Q>(this SearchControl<T, Q> control,
            int filterWidth = int.MaxValue, int maxDegreeOfParallelism = 4)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            DoParallelBreadthSearch(control, control.InitialState, filterWidth, int.MaxValue, int.MaxValue, maxDegreeOfParallelism);
            return control;
        }

        /// <summary>
        /// Performs an exhaustive depth-first search in a new Task. The search can be confined, by
        /// choosing only the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, C, Q>> ParallelDepthFirstAsync<T, C, Q>(this SearchControl<T, C, Q> control,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, int maxDegreeOfParallelism = 4)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelDepthFirst(control, filterWidth, depthLimit, maxDegreeOfParallelism));
        }

        /// <summary>
        /// Performs an exhaustive depth-first search. The search can be confined, by choosing only
        /// the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, C, Q> ParallelDepthFirst<T, C, Q>(this SearchControl<T, C, Q> control,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, int maxDegreeOfParallelism = 4)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            DoParallelDepthSearch<T, C, Q>(control, state, filterWidth, depthLimit, maxDegreeOfParallelism);
            return control;
        }

        /// <summary>
        /// Performs an exhaustive breadth-first search in a new Task. The search can be confined, by
        /// choosing only the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, C, Q>> ParallelBreadthFirstAsync<T, C, Q>(this SearchControl<T, C, Q> control,
            int filterWidth = int.MaxValue, int maxDegreeOfParallelism = 4)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelBreadthFirst<T, C, Q>(control, filterWidth, maxDegreeOfParallelism));
        }

        /// <summary>
        /// Performs an exhaustive breadth-first search. The search can be confined, by choosing only
        /// the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, C, Q> ParallelBreadthFirst<T, C, Q>(this SearchControl<T, C, Q> control,
            int filterWidth = int.MaxValue, int maxDegreeOfParallelism = 4)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            DoParallelBreadthSearch<T, C, Q>(control, state, filterWidth, int.MaxValue, int.MaxValue, maxDegreeOfParallelism);
            return control;
        }
        
        /// <summary>
        /// This method performs a sequential breadth-first search starting from <paramref name="state"/>
        /// until at least <paramref name="maxDegreeOfParallelism" /> nodes have been reached
        /// and then performs depth-first search in parallel.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The initial state from which the search should start</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns></returns>
        public static void DoParallelDepthSearch<T, Q>(ISearchControl<T, Q> control, T state,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, int maxDegreeOfParallelism = 4)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            if (maxDegreeOfParallelism <= 0) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be greater or equal than 0", nameof(maxDegreeOfParallelism));
            
            var states = Algorithms.DoBreadthSearch(control, state, filterWidth, depthLimit, maxDegreeOfParallelism);
            var guard = new WrappedThreadSafeSearchControl<T, Q>(control); // wrap the control to make it thread-safe
            Parallel.ForEach(states.AsEnumerable(),
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                s =>
                {
                    var localControl = SearchControl<T, Q>.Start(s);
                    Algorithms.DoDepthSearch(localControl, s, filterWidth, depthLimit);
                }
            );
        }

        /// <summary>
        /// This method performs a sequential breadth-first search starting from <paramref name="state"/>
        /// until at least <paramref name="maxDegreeOfParallelism" /> nodes have been reached
        /// and then performs breadth-first search in parallel.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The initial state from which the search should start</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth up to which the breadth-first search expands.</param>
        /// <param name="nodesReached">Expands up to a depth with at least this many nodes.</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The remaining nodes (e.g., if aborted by depthLimit or nodesReached)</returns>
        public static IStateCollection<T> DoParallelBreadthSearch<T, Q>(ISearchControl<T, Q> control, T state,
            int filterWidth, int depthLimit, int nodesReached, int maxDegreeOfParallelism)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            if (depthLimit <= 0) throw new ArgumentException($"{depthLimit} needs to be breater or equal than 0", nameof(depthLimit));
            if (nodesReached <= 0) throw new ArgumentException($"{nodesReached} needs to be breater or equal than 0", nameof(nodesReached));
            
            // TODO: get depth of sequential breadth-first search
            var states = Algorithms.DoBreadthSearch(control, state, filterWidth, depthLimit, maxDegreeOfParallelism);
            if (states.Nodes == 0 || states.Nodes >= nodesReached) return states;

            var queue = new ConcurrentQueue<List<T>>();
            var retrievedNodes = 0L;
            var guard = new WrappedThreadSafeSearchControl<T, Q>(control); // wrap the control to make it thread-safe
            Parallel.ForEach(states.AsEnumerable(),
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                s => {
                    // TODO: deduct depth above from depthLimit
                    var result = Algorithms.DoBreadthSearch(guard, s, filterWidth, depthLimit, nodesReached);
                    queue.Enqueue(result.AsEnumerable().ToList());
                    Interlocked.Add(ref retrievedNodes, result.RetrievedNodes);
                }
            );
            return new FIFOCollection<T>(queue.SelectMany(x => x), retrievedNodes);
        }
        
        /// <summary>
        /// This method performs a sequential breadth-first search starting from <paramref name="state"/>
        /// until at least <paramref name="maxDegreeOfParallelism" /> nodes have been reached
        /// and then performs depth-first search in parallel.
        /// </summary>
        /// <remarks>
        /// Because the state is mutable, the <paramref name="state"/> is mutated. You should
        /// consider to clone the state if a change is undesired.
        /// </remarks>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The initial state from which the search should start</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        public static void DoParallelDepthSearch<T, C, Q>(ISearchControl<T, Q> control, T state,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, int maxDegreeOfParallelism = 4)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            var states = Algorithms.DoBreadthSearch<T, C, Q>(control, (T)state.Clone(), filterWidth, depthLimit, maxDegreeOfParallelism);
            var guard = new WrappedThreadSafeSearchControl<T, C, Q>(control); // wrap the control to make it thread-safe
            Parallel.ForEach(states.AsEnumerable(),
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                s =>
                {                    
                    var localControl = SearchControl<T, C, Q>.Start(s);
                    Algorithms.DoDepthSearch<T, C, Q>(localControl, s, filterWidth, depthLimit);
                }
            );
        }

        /// <summary>
        /// This method performs a breadth-first search starting from <paramref name="state"/>.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The initial state from which the search should start</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth up to which the breadth-first search expands.</param>
        /// <param name="nodesReached">Expands up to a depth with at least this many nodes.</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The remaining nodes (e.g., if aborted by depthLimit or nodesReached)</returns>
        public static IStateCollection<T> DoParallelBreadthSearch<T, C, Q>(ISearchControl<T, Q> control, T state,
            int filterWidth, int depthLimit, int nodesReached, int maxDegreeOfParallelism)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            // TODO: Parallelize
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

    public static class ConcurrentAlgorithmStateExtensions
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
        public static Task<TState> ParallelDepthFirstAsync<TState, TQuality>(this IState<TState, TQuality> state, int filterWidth = int.MaxValue,
                int depthLimit = int.MaxValue, int maxDegreeOfParallelism = 4,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelDepthFirst((TState)state, filterWidth, depthLimit, maxDegreeOfParallelism, runtime, callback, nodeLimit, token));
        }
        /// <summary>
        /// Performs a depth-first search with the given options.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static TState ParallelDepthFirst<TState, TQuality>(this IState<TState, TQuality> state, int filterWidth = int.MaxValue,
                int depthLimit = int.MaxValue, int maxDegreeOfParallelism = 4,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (nodeLimit.HasValue) control = control.WithNodeLimit(nodeLimit.Value);
            return control.ParallelDepthFirst(filterWidth, depthLimit, maxDegreeOfParallelism).BestQualityState;
        }

        /// <summary>
        /// Performs a depth-first search with the given options in a new Task.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TChoice">The choice type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static Task<TState> ParallelDepthFirstAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int filterWidth = int.MaxValue,
                int depthLimit = int.MaxValue, int maxDegreeOfParallelism = 4,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelDepthFirst<TState, TChoice, TQuality>((TState)state, filterWidth, depthLimit, maxDegreeOfParallelism, runtime, callback, nodeLimit, token));
        }

        /// <summary>
        /// Performs a depth-first search with the given options.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TChoice">The choice type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static TState ParallelDepthFirst<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int filterWidth = int.MaxValue,
                int depthLimit = int.MaxValue, int maxDegreeOfParallelism = 4,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (nodeLimit.HasValue) control = control.WithNodeLimit(nodeLimit.Value);
            return control.ParallelDepthFirst(filterWidth, depthLimit, maxDegreeOfParallelism).BestQualityState;
        }

        /// <summary>
        /// Performs a breadth-first search with the given options in a new Task.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static Task<TState> ParallelBreadthFirstAsync<TState, TQuality>(this IState<TState, TQuality> state,
                int filterWidth = int.MaxValue, int maxDegreeOfParallelism = 4,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelBreadthFirst((TState)state, filterWidth, maxDegreeOfParallelism, runtime, callback, nodeLimit, token));
        }

        /// <summary>
        /// Performs a breadth-first search with the given options.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static TState ParallelBreadthFirst<TState, TQuality>(this IState<TState, TQuality> state,
                int filterWidth = int.MaxValue, int maxDegreeOfParallelism = 4,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (nodeLimit.HasValue) control = control.WithNodeLimit(nodeLimit.Value);
            return control.ParallelBreadthFirst(filterWidth, maxDegreeOfParallelism).BestQualityState;
        }

        /// <summary>
        /// Performs a breadth-first search with the given options in a new Task.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TChoice">The choice type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static Task<TState> ParallelBreadthFirstAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state,
                int filterWidth = int.MaxValue, int maxDegreeOfParallelism = 4,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelBreadthFirst<TState, TChoice, TQuality>((TState)state, filterWidth, maxDegreeOfParallelism, runtime, callback, nodeLimit, token));
        }

        /// <summary>
        /// Performs a breadth-first search with the given options.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TChoice">The choice type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static TState ParallelBreadthFirst<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state,
                int filterWidth = int.MaxValue, int maxDegreeOfParallelism = 4,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (nodeLimit.HasValue) control = control.WithNodeLimit(nodeLimit.Value);
            return control.ParallelBreadthFirst(filterWidth, maxDegreeOfParallelism).BestQualityState;
        }
    }
}