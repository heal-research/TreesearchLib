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
        /// This method performs a sequential breadth-first search starting from <paramref name="state"/>
        /// until at least <paramref name="maxDegreeOfParallelism" /> nodes have been reached
        /// and then performs depth-first search in parallel.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The initial state from which the search should start</param>
        /// <param name="depth">The current depth of the state</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns></returns>
        public static void ParallelDepthSearch<T, Q>(ISearchControl<T, Q> control, T state, int depth,
                int filterWidth, int depthLimit, long backtrackLimit, int maxDegreeOfParallelism = -1)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            if (depthLimit <= 0) throw new ArgumentException($"{depthLimit} needs to be breater or equal than 0", nameof(depthLimit));
            if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be -1 or greater or equal than 0", nameof(maxDegreeOfParallelism));
            
            var (d, states) = Algorithms.BreadthSearch(control, state, depth: depth, filterWidth, depthLimit, maxDegreeOfParallelism < 0 ? Environment.ProcessorCount : maxDegreeOfParallelism);
            depth = d;
            var remainingNodes = control.NodeLimit - control.VisitedNodes;
            if (depth >= depthLimit || states.Nodes == 0 || remainingNodes <= 0)
            {
                return;
            }
            var backtracksPerThread = long.MaxValue;
            if (backtrackLimit < long.MaxValue)
            {
                backtracksPerThread = (long)Math.Ceiling(backtrackLimit / (double)(maxDegreeOfParallelism < 0 ? Environment.ProcessorCount : maxDegreeOfParallelism));
            }

            var backtracks = 0L;
            var locker = new object();
            Parallel.ForEach(states.AsEnumerable(),
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                s =>
                {
                    var localDepth = depth;
                    var backtracksMem = 0L;
                    var searchState = new LIFOCollection<(int, T)>((localDepth, s));
                    while (true)
                    {
                        var localControl = SearchControl<T, Q>.Start(s)
                            .WithRuntimeLimit(TimeSpan.FromSeconds(1))
                            .WithCancellationToken(control.Cancellation);
                        lock (locker)
                        {
                            if (control.ShouldStop() || backtracks >= backtrackLimit)
                            {
                                localControl.Finish();
                                break;
                            }
                            backtracksMem = backtracks;

                            localControl = localControl.WithNodeLimit(remainingNodes);
                            if (control.BestQuality.HasValue)
                            {
                                localControl = localControl.WithUpperBound<T, Q>(control.BestQuality.Value);
                            }
                        }
                        var localbacktracks = Algorithms.DepthSearch<T, Q>(localControl, searchState, backtracksMem, filterWidth, depthLimit, backtracksPerThread);
                        localControl.Finish();

                        lock (locker)
                        {
                            control.Merge(localControl);
                            remainingNodes = control.NodeLimit - control.VisitedNodes;
                            backtracks += localbacktracks - backtracksMem;
                        }
                        if (searchState.Nodes == 0)
                        {
                            break;
                        }
                    }
                }
            );
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
        /// <param name="depth">The current depth of the state</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        public static void ParallelDepthSearch<T, C, Q>(ISearchControl<T, Q> control, T state, int depth,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue,
                int maxDegreeOfParallelism = -1)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            if (depthLimit <= 0) throw new ArgumentException($"{depthLimit} needs to be breater or equal than 0", nameof(depthLimit));
            if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be -1 or greater or equal than 0", nameof(maxDegreeOfParallelism));

            var (d, states) = Algorithms.BreadthSearch<T, C, Q>(control, (T)state.Clone(), depth, filterWidth, depthLimit, maxDegreeOfParallelism < 0 ? Environment.ProcessorCount : maxDegreeOfParallelism);
            depth = d;
            var remainingnodes = control.NodeLimit - control.VisitedNodes;
            if (depth >= depthLimit || states.Nodes == 0 || remainingnodes <= 0)
            {
                return;
            }
            var backtracksPerThread = long.MaxValue;
            if (backtrackLimit < long.MaxValue)
            {
                backtracksPerThread = (long)Math.Ceiling(backtrackLimit / (double)(maxDegreeOfParallelism < 0 ? Environment.ProcessorCount : maxDegreeOfParallelism));
            }

            var backtracks = 0L;
            var locker = new object();
            Parallel.ForEach(states.AsEnumerable(),
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                s =>
                {
                    var backtracksMem = 0L;
                    var localDepth = depth;
                    var searchState = new LIFOCollection<(int, C)>();
                    foreach (var choice in s.GetChoices().Take(filterWidth).Reverse())
                    {
                        searchState.Store((localDepth, choice));
                    }
                    
                    while (true)
                    {
                        var localControl = SearchControl<T, C, Q>.Start(s)
                            .WithRuntimeLimit(TimeSpan.FromSeconds(1))
                            .WithCancellationToken(control.Cancellation);
                        lock (locker)
                        {
                            if (control.ShouldStop() || backtracks >= backtrackLimit)
                            {
                                localControl.Finish();
                                break;
                            }
                            backtracksMem = backtracks;

                            localControl = localControl.WithNodeLimit(remainingnodes);
                            if (control.BestQuality.HasValue)
                            {
                                localControl = localControl.WithUpperBound<T, C, Q>(control.BestQuality.Value);
                            }
                        }
                        
                        var (dd, localbacktracks) = Algorithms.DepthSearch<T, C, Q>(localControl, s, searchState, localDepth, backtracksMem, filterWidth, depthLimit, backtracksPerThread);
                        localDepth = dd;
                        localControl.Finish();

                        lock (locker)
                        {
                            control.Merge(localControl);
                            remainingnodes = control.NodeLimit - control.VisitedNodes;
                            backtracks += localbacktracks - backtracksMem;
                        }
                        if (searchState.Nodes == 0)
                        {
                            break;
                        }
                    }
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
        /// <param name="depth">The current depth of the state</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth up to which the breadth-first search expands.</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The remaining nodes (e.g., if aborted by depthLimit or nodesReached)</returns>
        public static IStateCollection<T> ParallelBreadthSearch<T, Q>(ISearchControl<T, Q> control, T state,
            int depth, int filterWidth, int depthLimit, int maxDegreeOfParallelism)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            if (depthLimit <= 0) throw new ArgumentException($"{depthLimit} needs to be breater or equal than 0", nameof(depthLimit));
            if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be -1 or greater or equal than 0", nameof(maxDegreeOfParallelism));

            var (d, states) = Algorithms.BreadthSearch(control, state, depth, filterWidth, depthLimit, maxDegreeOfParallelism < 0 ? Environment.ProcessorCount : maxDegreeOfParallelism);
            depth = d;
            var remainingnodes = control.NodeLimit - control.VisitedNodes;
            if (depth >= depthLimit || states.Nodes == 0 || remainingnodes <= 0)
            {
                return states;
            }

            var queue = new ConcurrentQueue<List<T>>();
            var locker = new object();
            Parallel.ForEach(states.AsEnumerable(),
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                s =>
                {
                    var localDepth = depth;
                    var searchState = new BiLevelFIFOCollection<T>(s);
                    while (!control.ShouldStop())
                    {
                        var localControl = SearchControl<T, Q>.Start(s)
                            .WithRuntimeLimit(TimeSpan.FromSeconds(1))
                            .WithCancellationToken(control.Cancellation);
                        lock (locker)
                        {
                            localControl = localControl.WithNodeLimit(remainingnodes);
                            if (control.BestQuality.HasValue)
                            {
                                localControl = localControl.WithUpperBound<T, Q>(control.BestQuality.Value);
                            }
                        }

                        localDepth = Algorithms.BreadthSearch<T, Q>(localControl, searchState, localDepth, filterWidth, depthLimit, int.MaxValue);
                        localControl.Finish();

                        lock (locker)
                        {
                            control.Merge(localControl);
                            remainingnodes = control.NodeLimit - control.VisitedNodes;
                        }

                        if (searchState.GetQueueNodes + searchState.PutQueueNodes == 0
                            || localDepth >= depthLimit)
                        {
                            break;
                        }
                    }
                    queue.Enqueue(searchState.ToSingleLevel().AsEnumerable().ToList());
                }
            );
            return new FIFOCollection<T>(queue.SelectMany(x => x));
        }

        /// <summary>
        /// This method performs a breadth-first search starting from <paramref name="state"/>.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The initial state from which the search should start</param>
        /// <param name="depth">The current depth of the state</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth up to which the breadth-first search expands.</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The remaining nodes (e.g., if aborted by depthLimit or nodesReached)</returns>
        public static IStateCollection<T> ParallelBreadthSearch<T, C, Q>(ISearchControl<T, Q> control, T state,
            int depth, int filterWidth, int depthLimit, int maxDegreeOfParallelism)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            if (depthLimit <= 0) throw new ArgumentException($"{depthLimit} needs to be breater or equal than 0", nameof(depthLimit));
            if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be -1 or greater or equal than 0", nameof(maxDegreeOfParallelism));

            var (d, states) = Algorithms.BreadthSearch<T, C, Q>(control, (T)state.Clone(), depth, filterWidth, depthLimit, maxDegreeOfParallelism < 0 ? Environment.ProcessorCount : maxDegreeOfParallelism);
            depth = d;
            var remainingnodes = control.NodeLimit - control.VisitedNodes;
            if (depth >= depthLimit || states.Nodes == 0 || remainingnodes <= 0)
            {
                return states;
            }

            var queue = new ConcurrentQueue<List<T>>();
            var locker = new object();
            Parallel.ForEach(states.AsEnumerable(),
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, s =>
                {
                    var localDepth = depth;
                    var searchState = new BiLevelFIFOCollection<T>(s);
                    while (!control.ShouldStop())
                    {
                        var localControl = SearchControl<T, C, Q>.Start(s)
                            .WithRuntimeLimit<T, C, Q>(TimeSpan.FromSeconds(1))
                            .WithCancellationToken(control.Cancellation);
                        lock (locker)
                        {
                            localControl = localControl.WithNodeLimit(remainingnodes);
                            if (control.BestQuality.HasValue)
                            {
                                localControl = localControl.WithUpperBound<T, C, Q>(control.BestQuality.Value);
                            }
                        }

                        localDepth = Algorithms.BreadthSearch<T, C, Q>(localControl, searchState, localDepth, filterWidth, depthLimit, int.MaxValue);
                        localControl.Finish();

                        lock (locker)
                        {
                            control.Merge(localControl);
                            remainingnodes = control.NodeLimit - control.VisitedNodes;
                        }

                        if (searchState.GetQueueNodes + searchState.PutQueueNodes == 0
                            || localDepth >= depthLimit)
                        {
                            break;
                        }
                    }
                    queue.Enqueue(searchState.ToSingleLevel().AsEnumerable().ToList());
                }
            );
            return new FIFOCollection<T>(queue.SelectMany(x => x));
        }
    }

    public static class ConcurrentAlgorithmExtensions
    {
        /// <summary>
        /// Performs an exhaustive depth-first search in a new Task. The search can be confined, by
        /// choosing only the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, Q>> ParallelDepthFirstAsync<T, Q>(this SearchControl<T, Q> control,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue,
            int maxDegreeOfParallelism = -1)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelDepthFirst(control, filterWidth, depthLimit, backtrackLimit, maxDegreeOfParallelism));
        }

        /// <summary>
        /// Performs an exhaustive depth-first search. The search can be confined, by choosing only
        /// the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, Q> ParallelDepthFirst<T, Q>(this SearchControl<T, Q> control,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue,
            int maxDegreeOfParallelism = -1)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            ConcurrentAlgorithms.ParallelDepthSearch(control, control.InitialState, depth: 0, filterWidth, depthLimit, backtrackLimit, maxDegreeOfParallelism);
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
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, C, Q>> ParallelDepthFirstAsync<T, C, Q>(this SearchControl<T, C, Q> control,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue,
                int maxDegreeOfParallelism = -1)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelDepthFirst(control, filterWidth, depthLimit, backtrackLimit, maxDegreeOfParallelism));
        }

        /// <summary>
        /// Performs an exhaustive depth-first search. The search can be confined, by choosing only
        /// the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, C, Q> ParallelDepthFirst<T, C, Q>(this SearchControl<T, C, Q> control,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue,
                int maxDegreeOfParallelism = -1)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            ConcurrentAlgorithms.ParallelDepthSearch<T, C, Q>(control, state, depth: 0, filterWidth, depthLimit, backtrackLimit, maxDegreeOfParallelism);
            return control;
        }

        /// <summary>
        /// Performs an exhaustive breadth-first search in a new Task. The search can be confined, by
        /// choosing only the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, Q>> ParallelBreadthFirstAsync<T, Q>(this SearchControl<T, Q> control,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, int maxDegreeOfParallelism = -1)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelBreadthFirst(control, filterWidth, depthLimit, maxDegreeOfParallelism));
        }

        /// <summary>
        /// Performs an exhaustive breadth-first search. The search can be confined, by choosing only
        /// the first <paramref name="filterWidth"/> branches.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, Q> ParallelBreadthFirst<T, Q>(this SearchControl<T, Q> control,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, int maxDegreeOfParallelism = -1)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            ConcurrentAlgorithms.ParallelBreadthSearch(control, control.InitialState, depth: 0, filterWidth, depthLimit, maxDegreeOfParallelism);
            return control;
        }

        /// <summary>
        /// Performs an exhaustive breadth-first search in a new Task. The search can be confined, by
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
        public static Task<SearchControl<T, C, Q>> ParallelBreadthFirstAsync<T, C, Q>(this SearchControl<T, C, Q> control,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, int maxDegreeOfParallelism = -1)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelBreadthFirst<T, C, Q>(control, filterWidth, depthLimit, maxDegreeOfParallelism));
        }

        /// <summary>
        /// Performs an exhaustive breadth-first search. The search can be confined, by choosing only
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
        public static SearchControl<T, C, Q> ParallelBreadthFirst<T, C, Q>(this SearchControl<T, C, Q> control,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, int maxDegreeOfParallelism = -1)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            ConcurrentAlgorithms.ParallelBreadthSearch<T, C, Q>(control, state, depth: 0, filterWidth, depthLimit, maxDegreeOfParallelism);
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
        public static Task<TState> ParallelDepthFirstAsync<TState, TQuality>(this IState<TState, TQuality> state, int filterWidth = int.MaxValue,
                int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue, int maxDegreeOfParallelism = -1,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelDepthFirst((TState)state, filterWidth, depthLimit, backtrackLimit, maxDegreeOfParallelism, runtime, callback, nodeLimit, token));
        }
        /// <summary>
        /// Performs a depth-first search with the given options.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
        /// <param name="maxDegreeOfParallelism">Limits the number of parallel tasks</param>
        /// <param name="runtime">The maximum runtime</param>
        /// <param name="callback">A callback when an improving solution has been found</param>
        /// <param name="nodeLimit">A limit on the number of nodes to visit</param>
        /// <param name="token">The cancellation token</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The resulting best state that has been found (or none)</returns>
        public static TState ParallelDepthFirst<TState, TQuality>(this IState<TState, TQuality> state, int filterWidth = int.MaxValue,
                int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue, int maxDegreeOfParallelism = -1,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (nodeLimit.HasValue) control = control.WithNodeLimit(nodeLimit.Value);
            return control.ParallelDepthFirst(filterWidth, depthLimit, backtrackLimit, maxDegreeOfParallelism).BestQualityState;
        }

        /// <summary>
        /// Performs a depth-first search with the given options in a new Task.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
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
                int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue, int maxDegreeOfParallelism = -1,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelDepthFirst<TState, TChoice, TQuality>((TState)state, filterWidth, depthLimit, backtrackLimit, maxDegreeOfParallelism, runtime, callback, nodeLimit, token));
        }

        /// <summary>
        /// Performs a depth-first search with the given options.
        /// </summary>
        /// <param name="state">The state to start from</param>
        /// <param name="filterWidth">Limits the number of branches per node</param>
        /// <param name="depthLimit">Limits the depth of the search</param>
        /// <param name="backtrackLimit">Limits the number of backtracks</param>
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
                int depthLimit = int.MaxValue, long backtrackLimit = long.MaxValue, int maxDegreeOfParallelism = -1,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (nodeLimit.HasValue) control = control.WithNodeLimit(nodeLimit.Value);
            return control.ParallelDepthFirst(filterWidth, depthLimit, backtrackLimit, maxDegreeOfParallelism).BestQualityState;
        }

        /// <summary>
        /// Performs a breadth-first search with the given options in a new Task.
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
        public static Task<TState> ParallelBreadthFirstAsync<TState, TQuality>(this IState<TState, TQuality> state,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, int maxDegreeOfParallelism = -1,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelBreadthFirst((TState)state, filterWidth, depthLimit, maxDegreeOfParallelism, runtime, callback, nodeLimit, token));
        }

        /// <summary>
        /// Performs a breadth-first search with the given options.
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
        public static TState ParallelBreadthFirst<TState, TQuality>(this IState<TState, TQuality> state,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, int maxDegreeOfParallelism = -1,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (nodeLimit.HasValue) control = control.WithNodeLimit(nodeLimit.Value);
            return control.ParallelBreadthFirst(filterWidth, depthLimit, maxDegreeOfParallelism).BestQualityState;
        }

        /// <summary>
        /// Performs a breadth-first search with the given options in a new Task.
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
        public static Task<TState> ParallelBreadthFirstAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, int maxDegreeOfParallelism = -1,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelBreadthFirst<TState, TChoice, TQuality>((TState)state, filterWidth, depthLimit, maxDegreeOfParallelism, runtime, callback, nodeLimit, token));
        }

        /// <summary>
        /// Performs a breadth-first search with the given options.
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
        public static TState ParallelBreadthFirst<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, int maxDegreeOfParallelism = -1,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null, long? nodeLimit = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (nodeLimit.HasValue) control = control.WithNodeLimit(nodeLimit.Value);
            return control.ParallelBreadthFirst(filterWidth, depthLimit, maxDegreeOfParallelism).BestQualityState;
        }
    }
}