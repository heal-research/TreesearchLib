using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TreesearchLib
{
    /// <summary>
    /// This class contains implementations of many parallel heuristic algorithms.
    /// Each method is implemented for IState<T, Q> and IMutableState<T, C, Q> separately.
    /// There are extension methods to ISearchControl<T, Q> and IState<T, Q> resp. IMutableState<T, C, Q> that call these methods
    /// in the class <see cref="ConcurrentHeuristicExtensions"/>.
    /// </summary>
    public static class ConcurrentHeuristics {
        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <remarks>This is the concurrent version</remarks>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The state to start the search from</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes (lower is better)</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void ParallelBeamSearch<T, Q>(ISearchControl<T, Q> control, T state,
                int beamWidth, Func<T, float> rank, int filterWidth, int maxDegreeOfParallelism)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (rank == null) throw new ArgumentNullException(nameof(rank));
            if (filterWidth == 1 && beamWidth > 1) throw new ArgumentException($"{nameof(beamWidth)} cannot exceed 1 when {nameof(filterWidth)} equals 1.");
            if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be -1 or greater or equal than 0", nameof(maxDegreeOfParallelism));
            
            var currentLayer = new List<T>();
            currentLayer.Add(state);
            while (!control.ShouldStop() && currentLayer.Count > 0)
            {
                var nextlayer = new List<(float rank, T state)>();
                
                var reaminingTime = control.Runtime - control.Elapsed;
                if (reaminingTime < TimeSpan.Zero)
                {
                    break;
                }
                var remainingNodes = control.NodeLimit - control.VisitedNodes;
                var locker = new object();
                Parallel.ForEach(currentLayer, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                (currentState) =>
                {
                    var localNextLayer = new Queue<(float rank, T state)>();
                    var localControl = SearchControl<T, Q>.Start(currentState)
                        .WithCancellationToken(control.Cancellation)
                        .WithRuntimeLimit(reaminingTime);
                    lock (locker)
                    {
                        localControl = localControl.WithNodeLimit(remainingNodes);
                        if (control.BestQuality.HasValue)
                        {
                            // to discard certain nodes
                            localControl = localControl.WithUpperBound<T, Q>(control.BestQuality.Value);
                        }
                    }
                    foreach (var next in currentState.GetBranches().Take(filterWidth))
                    {
                        if (localControl.VisitNode(next) == VisitResult.Discard)
                        {
                            continue;
                        }

                        localNextLayer.Enqueue((rank(next), next));
                    }
                    localControl.Finish();
                    lock (locker)
                    {
                        control.Merge(localControl);
                        nextlayer.AddRange(localNextLayer);
                        remainingNodes = control.NodeLimit - control.VisitedNodes;
                    }
                });

                currentLayer.Clear();                
                currentLayer.AddRange(nextlayer
                    .OrderBy(x => x.rank)
                    .Take(beamWidth)
                    .Select(x => x.state));
            }
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <remarks>This is the concurrent version</remarks>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The state to start the search from</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes (lower is better)</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void ParallelBeamSearch<T, C, Q>(ISearchControl<T, Q> control, T state,
                int beamWidth, Func<T, float> rank, int filterWidth, int maxDegreeOfParallelism)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (rank == null) throw new ArgumentNullException(nameof(rank));
            if (filterWidth == 1 && beamWidth > 1) throw new ArgumentException($"{nameof(beamWidth)} cannot exceed 1 when {nameof(filterWidth)} equals 1.");
            if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be -1 or greater or equal than 0", nameof(maxDegreeOfParallelism));
            
            var currentLayer = new List<T>(beamWidth);
            currentLayer.Add(state);
            while (!control.ShouldStop() && currentLayer.Count > 0)
            {
                var nextlayer = new List<(float rank, T state)>();
                
                var locker = new object();
                var remainingNodes = control.NodeLimit - control.VisitedNodes;
                var remainingRuntime = control.Runtime - control.Elapsed;
                Parallel.ForEach(currentLayer, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                (currentState) =>
                {
                    var localNextLayer = new Queue<(float rank, T state)>();
                    var localControl = SearchControl<T, C, Q>.Start(currentState)
                            .WithCancellationToken(control.Cancellation)
                            .WithRuntimeLimit(remainingRuntime);
                    lock (locker)
                    {
                        localControl = localControl.WithNodeLimit(remainingNodes);
                        if (control.BestQuality.HasValue)
                        {
                            localControl = localControl.WithUpperBound<T, C, Q>(control.BestQuality.Value);
                        }
                    }
                    foreach (var choice in currentState.GetChoices().Take(filterWidth))
                    {
                        var next = (T)currentState.Clone();
                        next.Apply(choice);

                        if (localControl.VisitNode(next) == VisitResult.Discard)
                        {
                            continue;
                        }

                        localNextLayer.Enqueue((rank(next), next));
                    }
                    localControl.Finish();
                    lock (locker)
                    {
                        control.Merge(localControl);
                        nextlayer.AddRange(localNextLayer);
                        remainingNodes = control.NodeLimit - control.VisitedNodes;
                    }
                });

                currentLayer.Clear();                
                currentLayer.AddRange(nextlayer
                    .OrderBy(x => x.rank)
                    .Take(beamWidth)
                    .Select(x => x.state));
            }
        }

        /// <summary>
        /// Rake search performs a breadth-first search until a level is reached with <paramref name="rakeWidth"/>
        /// nodes and then from each node a depth-first search by just taking the first branch (i.e., a greedy heuristic).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The state to start the search from</param>
        /// <param name="rakeWidth">The number of nodes to reach, before proceeding with a simple greedy heuristic</param>
        /// <param name="lookahead">The lookahead method to use</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, Q> ParallelRakeSearch<T, Q>(
                this SearchControl<T, Q> control, T state, int rakeWidth,
                Lookahead<T, Q> lookahead, int maxDegreeOfParallelism = -1)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (rakeWidth <= 0) throw new ArgumentException($"{rakeWidth} needs to be greater or equal than 1", nameof(rakeWidth));
            if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be -1 or greater or equal than 0", nameof(maxDegreeOfParallelism));
            if (lookahead == null) throw new ArgumentNullException(nameof(lookahead));

            var (_, rake) = Algorithms.BreadthSearch(control, state, depth: 0, int.MaxValue, int.MaxValue, rakeWidth);
            var remainingTime = control.Runtime - control.Elapsed;
            var remainingNodes = control.NodeLimit - control.VisitedNodes;
            if (control.ShouldStop() || remainingTime < TimeSpan.Zero || remainingNodes <= 0)
            {
                return control;
            }
            var locker = new object();
            Parallel.ForEach(rake.AsEnumerable().Take(rakeWidth), new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
            next =>
            {
                var localControl = SearchControl<T, Q>.Start(next)
                    .WithCancellationToken(control.Cancellation)
                    .WithRuntimeLimit(remainingTime);
                lock (locker)
                {
                    localControl = localControl.WithNodeLimit(remainingNodes);
                    if (control.BestQuality.HasValue)
                    {
                        localControl = localControl.WithUpperBound<T, Q>(control.BestQuality.Value);
                    }
                }
                lookahead(localControl, next);
                localControl.Finish();
                lock (locker)
                {
                    control.Merge(localControl);
                    remainingNodes = control.NodeLimit - control.VisitedNodes;
                }
            });
            return control;
        }

        /// <summary>
        /// Rake search performs a breadth-first search until a level is reached with <paramref name="rakeWidth"/>
        /// nodes and then from each node a depth-first search by just taking the first branch (i.e., a greedy heuristic).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The state to start the search from</param>
        /// <param name="rakeWidth">The number of nodes to reach, before proceeding with a simple greedy heuristic</param>
        /// <param name="lookahead">The lookahead method to use</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, C, Q> ParallelRakeSearch<T, C, Q>(
                this SearchControl<T, C, Q> control, T state, int rakeWidth,
                Lookahead<T, C, Q> lookahead, int maxDegreeOfParallelism = -1)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (rakeWidth <= 0) throw new ArgumentException($"{rakeWidth} needs to be greater or equal than 1", nameof(rakeWidth));
            if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be -1 or greater or equal than 0", nameof(maxDegreeOfParallelism));
            if (lookahead == null) throw new ArgumentNullException(nameof(lookahead));

            var (_, rake) = Algorithms.BreadthSearch<T, C, Q>(control, state, depth: 0, int.MaxValue, int.MaxValue, rakeWidth);
            var remainingTime = control.Runtime - control.Elapsed;
            var remainingNodes = control.NodeLimit - control.VisitedNodes;
            if (control.ShouldStop() || remainingTime < TimeSpan.Zero || remainingNodes <= 0) // the last two are just safety checks, control.ShouldStop() should terminate in these cases too
            {
                return control;
            }
            var locker = new object();
            Parallel.ForEach(rake.AsEnumerable().Take(rakeWidth), new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
            next =>
            {
                var localControl = SearchControl<T, C, Q>.Start(next)
                    .WithCancellationToken(control.Cancellation)
                    .WithRuntimeLimit(remainingTime); // each thread gets the same time
                lock (locker)
                {
                    localControl = localControl.WithNodeLimit(remainingNodes);
                    if (control.BestQuality.HasValue)
                    {
                        localControl = localControl.WithUpperBound<T, C, Q>(control.BestQuality.Value);
                    }
                }
                lookahead(localControl, next);
                localControl.Finish();
                lock (locker)
                {
                    control.Merge(localControl);
                    remainingNodes = control.NodeLimit - control.VisitedNodes;
                }
            });
            return control;
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, an efficient beam search may be used for the lookahead.
        /// The lookahead depth is not configurable, instead a full solution must be achieved.
        /// </summary>
        /// <remarks>
        /// Voß, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="state">The state from which the pilot method should start operating</param>
        /// <param name="depth">The depth of the current state</param>
        /// <param name="lookahead">The lookahead method to use</param>
        /// <param name="filterWidth">How many descendents will be considered for lookahead</param>
        /// <param name="depthLimit">The depth limit for the pilot search</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns></returns>
        public static void ParallelPilotMethod<T, Q>(ISearchControl<T, Q> control,
                T state, int depth, Lookahead<T, Q> lookahead,
                int filterWidth, int depthLimit, int maxDegreeOfParallelism)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (depthLimit < 0) throw new ArgumentException($"{nameof(depthLimit)} needs to be greater or equal to 0", nameof(depthLimit));
            if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1) throw new ArgumentException($"{maxDegreeOfParallelism} is not a valid value for {nameof(maxDegreeOfParallelism)}", nameof(maxDegreeOfParallelism));
            if (lookahead == null) throw new ArgumentNullException(nameof(lookahead));

            var remainingTime = control.Runtime - control.Elapsed;
            var remainingNodes = control.NodeLimit - control.VisitedNodes;
            var locker = new object();
            while (depth < depthLimit)
            {
                var branches = state.GetBranches().ToList();
                if (branches.Count == 0) return;
                T bestBranch = branches[0]; // default first branch
                Q? bestBranchQuality = null;
                Parallel.ForEach(branches,
                    new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                    next =>
                    {
                        Q? quality = default;
                        if (next.IsTerminal)
                        {
                            // no lookahead required
                            quality = next.Quality;
                        } else
                        {
                            var localControl = SearchControl<T, Q>.Start(next)
                                .WithCancellationToken(control.Cancellation)
                                .WithRuntimeLimit(remainingTime); // each thread gets the same time
                            lock (locker)
                            {
                                localControl = localControl.WithNodeLimit(remainingNodes);
                            }
                            lookahead(localControl, next);
                            localControl.Finish();
                            if (localControl.BestQuality.HasValue) quality = localControl.BestQuality;
                            lock (locker)
                            {
                                control.Merge(localControl);
                                remainingNodes = control.NodeLimit - control.VisitedNodes;
                            }
                        }

                        if (!quality.HasValue) return; // no solution achieved
                        lock (locker)
                        {
                            if (!bestBranchQuality.HasValue || quality.Value.IsBetter(bestBranchQuality.Value))
                            {
                                bestBranch = next;
                                bestBranchQuality = quality;
                            }
                        }
                    }
                );
                state = bestBranch;
                depth++;
                if (state.IsTerminal) return;
            }
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, an efficient beam search may be used for the lookahead.
        /// The lookahead depth is not configurable, instead a full solution must be achieved.
        /// </summary>
        /// <remarks>
        /// Voß, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="state">The state from which the pilot method should start operating</param>
        /// <param name="depth">The depth of the current state</param>
        /// <param name="lookahead">The lookahead method to use</param>
        /// <param name="filterWidth">How many descendents will be considered for lookahead</param>
        /// <param name="depthLimit">The depth limit for the pilot search</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns></returns>
        public static void ParallelPilotMethod<T, C, Q>(ISearchControl<T, Q> control,
                T state, int depth, Lookahead<T, C, Q> lookahead,
                int filterWidth, int depthLimit, int maxDegreeOfParallelism)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (depthLimit < 0) throw new ArgumentException($"{nameof(depthLimit)} needs to be greater or equal to 0", nameof(depthLimit));
            if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1) throw new ArgumentException($"{nameof(maxDegreeOfParallelism)} needs to be -1 or greater or equal to 1", nameof(maxDegreeOfParallelism));
            if (lookahead == null) throw new ArgumentNullException(nameof(lookahead));

            var remainingTime = control.Runtime - control.Elapsed;
            var remainingNodes = control.NodeLimit - control.VisitedNodes;
            var locker = new object();
            while (depth < depthLimit)
            {
                var branches = state.GetChoices().Select(c => { var clone = (T)state.Clone(); clone.Apply(c); return clone; }).ToList();
                if (branches.Count == 0) return;
                T bestBranch = branches[0];
                Q? bestBranchQuality = null;
                Parallel.ForEach(branches,
                    new ParallelOptions() { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                    next =>
                    {
                        Q? quality = default;
                        if (next.IsTerminal)
                        {
                            // no lookahead required
                            quality = next.Quality;
                        } else
                        {
                            var localControl = SearchControl<T, C, Q>.Start(next)
                                .WithCancellationToken(control.Cancellation)
                                .WithRuntimeLimit(remainingTime); // each thread gets the same time
                            lock (locker)
                            {
                                localControl = localControl.WithNodeLimit(remainingNodes);
                            }
                            lookahead(localControl, next);
                            localControl.Finish();
                            if (localControl.BestQuality.HasValue) quality = localControl.BestQuality;
                            lock (locker)
                            {
                                control.Merge(localControl);
                                remainingNodes = control.NodeLimit - control.VisitedNodes;
                            }
                        }

                        if (!quality.HasValue) return; // no solution achieved
                        lock (locker)
                        {
                            if (!bestBranchQuality.HasValue || quality.Value.IsBetter(bestBranchQuality.Value))
                            {
                                bestBranch = next;
                                bestBranchQuality = quality;
                            }
                        }
                    }
                );
                state = bestBranch;
                depth++;
                if (state.IsTerminal) return;
            }
        }
    }

    public static class ConcurrentHeuristicExtensions
    {
        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <remarks>This is the concurrent version</remarks>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes (lower is better)</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static Task<SearchControl<T, Q>> ParallelBeamSearchAsync<T, Q>(
            this SearchControl<T, Q> control, int beamWidth, Func<T, float> rank,
            int filterWidth = int.MaxValue, int maxDegreeOfParallelism = -1)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelBeamSearch(control, beamWidth, rank,
                filterWidth, maxDegreeOfParallelism));
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <remarks>This is the concurrent version</remarks>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes (lower is better)</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static SearchControl<T, Q> ParallelBeamSearch<T, Q>(
            this SearchControl<T, Q> control, int beamWidth, Func<T, float> rank,
            int filterWidth = int.MaxValue, int maxDegreeOfParallelism = -1)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            ConcurrentHeuristics.ParallelBeamSearch(control, control.InitialState, beamWidth, rank,
                filterWidth, maxDegreeOfParallelism);
            return control;
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <remarks>This is the concurrent version</remarks>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes (lower is better)</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static Task<SearchControl<T, C, Q>> ParallelBeamSearchAsync<T, C, Q>(
            this SearchControl<T, C, Q> control, int beamWidth, Func<T, float> rank,
            int filterWidth = int.MaxValue, int maxDegreeOfParallelism = -1)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelBeamSearch(control, beamWidth, rank,
                filterWidth, maxDegreeOfParallelism));
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <remarks>This is the concurrent version</remarks>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes (lower is better)</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static SearchControl<T, C, Q> ParallelBeamSearch<T, C, Q>(
            this SearchControl<T, C, Q> control, int beamWidth, Func<T, float> rank,
            int filterWidth = int.MaxValue, int maxDegreeOfParallelism = -1)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            ConcurrentHeuristics.ParallelBeamSearch<T, C, Q>(control, control.InitialState, beamWidth,
                rank, filterWidth, maxDegreeOfParallelism);
            return control;
        }

        /// <summary>
        /// Rake search performs a breadth-first search until a level is reached with <paramref name="rakeWidth"/>
        /// nodes and then from each node a depth-first search by just taking the first branch (i.e., a greedy heuristic).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="rakeWidth">The number of nodes to reach, before proceeding with a simple greedy heuristic</param>
        /// <param name="lookahead">The lookahead method to use, <see cref="LA.DFSLookahead"/> with filterWidth = 1, will be used if null</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, Q>> ParallelRakeSearchAsync<T, Q>(
            this SearchControl<T, Q> control, int rakeWidth,
            Lookahead<T, Q> lookahead = null, int maxDegreeOfParallelism = -1)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelRakeSearch(control, rakeWidth, lookahead, maxDegreeOfParallelism));
        }

        /// <summary>
        /// Rake search performs a breadth-first search until a level is reached with <paramref name="rakeWidth"/>
        /// nodes and then from each node a depth-first search by just taking the first branch (i.e., a greedy heuristic).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="rakeWidth">The number of nodes to reach, before proceeding with a simple greedy heuristic</param>
        /// <param name="lookahead">The lookahead method to use, <see cref="LA.DFSLookahead"/> with filterWidth = 1, will be used if null</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, Q> ParallelRakeSearch<T, Q>(
            this SearchControl<T, Q> control, int rakeWidth, 
            Lookahead<T, Q> lookahead = null, int maxDegreeOfParallelism = -1)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (rakeWidth <= 0) throw new ArgumentException($"{rakeWidth} needs to be greater or equal than 1", nameof(rakeWidth));
            if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be -1 or greater or equal than 0", nameof(maxDegreeOfParallelism));
            if (lookahead == null) lookahead = LA.DFSLookahead<T, Q>(filterWidth: 1);

            ConcurrentHeuristics.ParallelRakeSearch(control, control.InitialState, rakeWidth, lookahead, maxDegreeOfParallelism);
            return control;
        }

        /// <summary>
        /// Rake search performs a breadth-first search until a level is reached with <paramref name="rakeWidth"/>
        /// nodes and then from each node a depth-first search by just taking the first branch (i.e., a greedy heuristic).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="rakeWidth">The number of nodes to reach, before proceeding with a simple greedy heuristic</param>
        /// <param name="lookahead">The lookahead method to use, <see cref="LA.DFSLookahead"/> with filterWidth = 1, will be used if null</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, C, Q>> ParallelRakeSearchAsync<T, C, Q>(
            this SearchControl<T, C, Q> control, int rakeWidth,
            Lookahead<T, C, Q> lookahead = null, int maxDegreeOfParallelism = -1)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelRakeSearch(control, rakeWidth, lookahead, maxDegreeOfParallelism));
        }

        /// <summary>
        /// Rake search performs a breadth-first search until a level is reached with <paramref name="rakeWidth"/>
        /// nodes and then from each node a depth-first search by just taking the first branch (i.e., a greedy heuristic).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="rakeWidth">The number of nodes to reach, before proceeding with a simple greedy heuristic</param>
        /// <param name="lookahead">The lookahead method to use, <see cref="LA.DFSLookahead"/> with filterWidth = 1, will be used if null</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, C, Q> ParallelRakeSearch<T, C, Q>(
            this SearchControl<T, C, Q> control, int rakeWidth,
            Lookahead<T, C, Q> lookahead = null, int maxDegreeOfParallelism = -1)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (rakeWidth <= 0) throw new ArgumentException($"{rakeWidth} needs to be greater or equal than 1", nameof(rakeWidth));
            if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be -1 or greater or equal than 0", nameof(maxDegreeOfParallelism));
            if (lookahead == null) lookahead = LA.DFSLookahead<T, C, Q>(filterWidth: 1);

            ConcurrentHeuristics.ParallelRakeSearch(control, (T)control.InitialState.Clone(), rakeWidth, lookahead, maxDegreeOfParallelism);
            return control;
        }

        /// <summary>
        /// Rake search performs a breadth-first search until a level is reached with <paramref name="rakeWidth"/>
        /// nodes and then from each node a beam search is performed.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="rakeWidth">The number of nodes to reach, before proceeding with a simple greedy heuristic</param>
        /// <param name="beamWidth">Used in the beam search to determine the number of beams</param>
        /// <param name="rank">The ranking function used by the beam search (lower is better)</param>
        /// <param name="filterWidth">To limit the number of branches per node</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        [Obsolete("Use ParallelRakeSearchAsync with BeamSearchLookahead")]
        public static Task<SearchControl<T, Q>> ParallelRakeAndBeamSearchAsync<T, Q>(
            this SearchControl<T, Q> control, int rakeWidth, int beamWidth,
            Func<T, float> rank, int filterWidth = int.MaxValue,
            int maxDegreeOfParallelism = -1)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelRakeAndBeamSearch(control, rakeWidth, beamWidth, rank, filterWidth, maxDegreeOfParallelism));
        }

        /// <summary>
        /// Rake search performs a breadth-first search until a level is reached with <paramref name="rakeWidth"/>
        /// nodes and then from each node a beam search is performed.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="rakeWidth">The number of nodes to reach, before proceeding with a simple greedy heuristic</param>
        /// <param name="beamWidth">Used in the beam search to determine the number of beams</param>
        /// <param name="rank">The ranking function used by the beam search (lower is better)</param>
        /// <param name="filterWidth">To limit the number of branches per node</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        [Obsolete("Use ParallelRakeSearch with BeamSearchLookahead")]
        public static SearchControl<T, Q> ParallelRakeAndBeamSearch<T, Q>(
            this SearchControl<T, Q> control, int rakeWidth, int beamWidth,
            Func<T, float> rank, int filterWidth = int.MaxValue,
            int maxDegreeOfParallelism = -1)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (rakeWidth <= 0) throw new ArgumentException($"{rakeWidth} needs to be greater or equal than 1", nameof(rakeWidth));
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (filterWidth <= 0) throw new ArgumentException("A filter width of 0 or less is not possible");
            if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be -1 or greater or equal than 0", nameof(maxDegreeOfParallelism));
            
            var (_, rake) = Algorithms.BreadthSearch(control, control.InitialState, depth: 0, int.MaxValue, int.MaxValue, rakeWidth);
            var remainingTime = control.Runtime - control.Elapsed;
            var remainingNodes = control.NodeLimit - control.VisitedNodes;
            if (control.ShouldStop() || remainingTime < TimeSpan.Zero || remainingNodes <= 0) // the last two are just safety checks, control.ShouldStop() should terminate in these cases too
            {
                return control;
            }
            var locker = new object();
            Parallel.ForEach(rake.AsEnumerable().Take(rakeWidth), new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
            next =>
            {
                var localControl = SearchControl<T, Q>.Start(next)
                    .WithCancellationToken(control.Cancellation)
                    .WithRuntimeLimit(remainingTime); // each thread gets the same time
                lock (locker)
                {
                    localControl = localControl.WithNodeLimit(remainingNodes);
                    if (control.BestQuality.HasValue)
                    {
                        localControl = localControl.WithUpperBound<T, Q>(control.BestQuality.Value);
                    }
                }
                Heuristics.BeamSearch<T, Q>(localControl, next, beamWidth, rank, filterWidth, depthLimit: int.MaxValue);
                lock (locker)
                {
                    control.Merge(localControl);
                    remainingNodes = control.NodeLimit - control.VisitedNodes;
                }
            });
            return control;
        }

        /// <summary>
        /// Rake search performs a breadth-first search until a level is reached with <paramref name="rakeWidth"/>
        /// nodes and then from each node a beam search is performed.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="rakeWidth">The number of nodes to reach, before proceeding with a simple greedy heuristic</param>
        /// <param name="beamWidth">Used in the beam search to determine the number of beams</param>
        /// <param name="rank">The ranking function used by the beam search (lower is better)</param>
        /// <param name="filterWidth">To limit the number of branches per node</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        [Obsolete("Use ParallelRakeSearchAsync with BeamSearchLookahead")]
        public static Task<SearchControl<T, C, Q>> ParallelRakeAndBeamSearchAsync<T, C, Q>(
            this SearchControl<T, C, Q> control, int rakeWidth, int beamWidth,
            Func<T, float> rank, int filterWidth = int.MaxValue, int maxDegreeOfParallelism = -1)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelRakeAndBeamSearch(control, rakeWidth, beamWidth, rank, filterWidth, maxDegreeOfParallelism));
        }

        /// <summary>
        /// Rake search performs a breadth-first search until a level is reached with <paramref name="rakeWidth"/>
        /// nodes and then from each node a beam search is performed.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="rakeWidth">The number of nodes to reach, before proceeding with a simple greedy heuristic</param>
        /// <param name="beamWidth">Used in the beam search to determine the number of beams</param>
        /// <param name="rank">The ranking function used by the beam search (lower is better)</param>
        /// <param name="filterWidth">To limit the number of branches per node</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        [Obsolete("Use ParallelRakeSearch with BeamSearchLookahead")]
        public static SearchControl<T, C, Q> ParallelRakeAndBeamSearch<T, C, Q>(
            this SearchControl<T, C, Q> control, int rakeWidth, int beamWidth,
            Func<T, float> rank, int filterWidth = int.MaxValue, int maxDegreeOfParallelism = -1)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (rakeWidth <= 0) throw new ArgumentException($"{rakeWidth} needs to be greater or equal than 1", nameof(rakeWidth));
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (filterWidth <= 0) throw new ArgumentException("A filter width of 0 or less is not possible");
            if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be -1 or greater or equal than 0", nameof(maxDegreeOfParallelism));
            
            var (_, rake) = Algorithms.BreadthSearch<T, C, Q>(control, (T)control.InitialState.Clone(), depth: 0, int.MaxValue, int.MaxValue, rakeWidth);
            var remainingTime = control.Runtime - control.Elapsed;
            var remainingNodes = control.NodeLimit - control.VisitedNodes;
            if (control.ShouldStop() || remainingTime < TimeSpan.Zero || remainingNodes <= 0) // the last two are just safety checks, control.ShouldStop() should terminate in these cases too
            {
                return control;
            }
            var locker = new object();
            Parallel.ForEach(rake.AsEnumerable().Take(rakeWidth), new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
            next =>
            {
                var localControl = SearchControl<T, C, Q>.Start(next)
                    .WithCancellationToken(control.Cancellation)
                    .WithRuntimeLimit(remainingTime); // each thread gets the same time
                lock (locker)
                {
                    localControl = localControl.WithNodeLimit(remainingNodes);
                    if (control.BestQuality.HasValue)
                    {
                        localControl = localControl.WithUpperBound<T, C, Q>(control.BestQuality.Value);
                    }
                }
                Heuristics.BeamSearch<T, C, Q>(localControl, next, depth: 0, beamWidth, rank, filterWidth, depthLimit: int.MaxValue);
                lock (locker)
                {
                    control.Merge(localControl);
                    remainingNodes = control.NodeLimit - control.VisitedNodes;
                }
            });
            return control;
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, an efficient beam search may be used for the lookahead.
        /// The lookahead depth is not configurable, instead a full solution must be achieved.
        /// </summary>
        /// <remarks>
        /// Voß, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="lookahead">The lookahead method to use, <see cref="LA.DFSLookahead"/> with filterWidth = 1, will be used if null</param>
        /// <param name="filterWidth">How many descendents will be considered for lookahead</param>
        /// <param name="depthLimit">The maximum depth of the pilot search</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use for the lookahead.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static Task<SearchControl<T, Q>> ParallelPilotMethodAsync<T, Q>(
            this SearchControl<T, Q> control, Lookahead<T, Q> lookahead = null,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, int maxDegreeOfParallelism = -1)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelPilotMethod(control, lookahead, filterWidth, depthLimit,
                maxDegreeOfParallelism));
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, an efficient beam search may be used for the lookahead.
        /// The lookahead depth is not configurable, instead a full solution must be achieved.
        /// </summary>
        /// <remarks>
        /// Voß, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="lookahead">The lookahead method to use, <see cref="LA.DFSLookahead"/> with filterWidth = 1, will be used if null</param>
        /// <param name="filterWidth">How many descendents will be considered for lookahead</param>
        /// <param name="depthLimit">The maximum depth of the pilot search</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use for the search.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static SearchControl<T, Q> ParallelPilotMethod<T, Q>(
            this SearchControl<T, Q> control, Lookahead<T, Q> lookahead = null,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, int maxDegreeOfParallelism = -1)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException("A filter width of 0 or less is not possible");
            if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be -1 or greater or equal than 0", nameof(maxDegreeOfParallelism));
            if (lookahead == null) lookahead = LA.DFSLookahead<T, Q>(filterWidth: 1);

            var state = control.InitialState;
            ConcurrentHeuristics.ParallelPilotMethod<T, Q>(control, state, depth: 0, lookahead,
                filterWidth, depthLimit, maxDegreeOfParallelism);
            return control;
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, an efficient beam search may be used for the lookahead.
        /// The lookahead depth is not configurable, instead a full solution must be achieved.
        /// </summary>
        /// <remarks>
        /// Voß, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="lookahead">The lookahead method to use, <see cref="LA.DFSLookahead"/> with filterWidth = 1, will be used if null</param>
        /// <param name="filterWidth">How many descendents will be considered for lookahead</param>
        /// <param name="depthLimit">The maximum depth of the pilot search</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static Task<SearchControl<T, C, Q>> ParallelPilotMethodAsync<T, C, Q>(
            this SearchControl<T, C, Q> control, Lookahead<T, C, Q> lookahead = null,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, int maxDegreeOfParallelism = -1)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelPilotMethod(control, lookahead, filterWidth, depthLimit,
                maxDegreeOfParallelism));
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, an efficient beam search may be used for the lookahead.
        /// The lookahead depth is not configurable, instead a full solution must be achieved.
        /// </summary>
        /// <remarks>
        /// Voß, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="beamWidth">The parameter that governs how many parallel lines through the search tree should be considered during lookahead. For values > 1, rank must be defined as BeamSearch will be used.</param>
        /// <param name="rank">A function that ranks states (lower is better), if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <param name="filterWidth">How many descendents will be considered for lookahead</param>
        /// <param name="depthLimit">The maximum depth of the pilot search</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static SearchControl<T, C, Q> ParallelPilotMethod<T, C, Q>(
            this SearchControl<T, C, Q> control, Lookahead<T, C, Q> lookahead = null,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue, int maxDegreeOfParallelism = -1)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException("A filter width of 0 or less is not possible");
            if (maxDegreeOfParallelism == 0 || maxDegreeOfParallelism < -1) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be -1 or greater or equal than 0", nameof(maxDegreeOfParallelism));
            if (lookahead == null) lookahead = LA.DFSLookahead<T, C, Q>(filterWidth: 1);
            
            var state = (T)control.InitialState.Clone();
            ConcurrentHeuristics.ParallelPilotMethod<T, C, Q>(control, state, depth: 0, lookahead,
                filterWidth, depthLimit, maxDegreeOfParallelism);
            return control;
        }

        public static Task<TState> ParallelBeamSearchAsync<TState, TQuality>(
            this IState<TState, TQuality> state, int beamWidth, Func<TState, float> rank,
            int filterWidth = int.MaxValue, TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken),
            int maxDegreeOfParallelism = -1)
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelBeamSearch((TState)state, beamWidth, rank,
                filterWidth, runtime, nodelimit, callback, token, maxDegreeOfParallelism));
        }
        public static TState ParallelBeamSearch<TState, TQuality>(
            this IState<TState, TQuality> state, int beamWidth, Func<TState, float> rank,
            int filterWidth = int.MaxValue, TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken),
            int maxDegreeOfParallelism = -1)
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.ParallelBeamSearch(beamWidth, rank, filterWidth,
                maxDegreeOfParallelism).BestQualityState;
        }

        public static Task<TState> ParallelBeamSearchAsync<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int beamWidth,
            Func<TState, float> rank, int filterWidth = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken),
            int maxDegreeOfParallelism = -1)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelBeamSearch<TState, TChoice, TQuality>(
                (TState)state, beamWidth, rank, filterWidth, runtime, nodelimit,
                callback, token, maxDegreeOfParallelism)
            );
        }

        public static TState ParallelBeamSearch<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int beamWidth,
            Func<TState, float> rank, int filterWidth = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken),
            int maxDegreeOfParallelism = -1)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).
                WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.ParallelBeamSearch(beamWidth, rank, filterWidth,
                maxDegreeOfParallelism).BestQualityState;
        }

        public static Task<TState> ParallelRakeSearchAsync<TState, TQuality>(
            this IState<TState, TQuality> state, int rakeWidth,
            Lookahead<TState, TQuality> lookahead = null,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken),
            int maxDegreeOfParallelism = -1)
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelRakeSearch((TState)state, rakeWidth, lookahead,
                runtime, nodelimit, callback, token, maxDegreeOfParallelism));
        }

        public static TState ParallelRakeSearch<TState, TQuality>(
            this IState<TState, TQuality> state, int rakeWidth,
            Lookahead<TState, TQuality> lookahead = null,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken),
            int maxDegreeOfParallelism = -1)
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.ParallelRakeSearch(rakeWidth, lookahead,
                maxDegreeOfParallelism: maxDegreeOfParallelism).BestQualityState;
        }

        public static Task<TState> ParallelRakeSearchAsync<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int rakeWidth,
            Lookahead<TState, TChoice, TQuality> lookahead = null,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken),
            int maxDegreeOfParallelism = -1)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelRakeSearch<TState, TChoice, TQuality>(
                (TState)state, rakeWidth, lookahead, runtime, nodelimit, callback, token,
                maxDegreeOfParallelism)
            );
        }

        public static TState ParallelRakeSearch<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int rakeWidth,
            Lookahead<TState, TChoice, TQuality> lookahead = null,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken),
            int maxDegreeOfParallelism = -1)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.ParallelRakeSearch(rakeWidth, lookahead,
                maxDegreeOfParallelism: maxDegreeOfParallelism).BestQualityState;
        }

        [Obsolete("Use ParallelRakeSearchAsync with BeamSearchLookahead instead")]
        public static Task<TState> ParallelRakeAndBeamSearchAsync<TState, TQuality>(
            this IState<TState, TQuality> state, int rakeWidth, int beamWidth,
            Func<TState, float> rank, int filterWidth = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken),
            int maxDegreeOfParallelism = -1)
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelRakeAndBeamSearch((TState)state, rakeWidth,
                beamWidth, rank, filterWidth, runtime, nodelimit, callback,
                token, maxDegreeOfParallelism)
            );
        }

        [Obsolete("Use ParallelRakeSearch with BeamSearchLookahead instead")]
        public static TState ParallelRakeAndBeamSearch<TState, TQuality>(
            this IState<TState, TQuality> state, int rakeWidth, int beamWidth,
            Func<TState, float> rank, int filterWidth = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken),
            int maxDegreeOfParallelism = -1)
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.ParallelRakeAndBeamSearch(rakeWidth, beamWidth, rank,
                filterWidth, maxDegreeOfParallelism: maxDegreeOfParallelism).BestQualityState;
        }

        [Obsolete("Use ParallelRakeSearchAsync with BeamSearchLookahead instead")]
        public static Task<TState> ParallelRakeAndBeamSearchAsync<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int rakeWidth, int beamWidth,
            Func<TState, float> rank, int filterWidth = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken),
            int maxDegreeOfParallelism = -1)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelRakeAndBeamSearch<TState, TChoice, TQuality>(
                (TState)state, rakeWidth, beamWidth, rank, filterWidth, runtime,
                nodelimit, callback, token, maxDegreeOfParallelism)
            );
        }

        [Obsolete("Use ParallelRakeSearch with BeamSearchLookahead instead")]
        public static TState ParallelRakeAndBeamSearch<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int rakeWidth, int beamWidth,
            Func<TState, float> rank, int filterWidth = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken),
            int maxDegreeOfParallelism = -1)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.ParallelRakeAndBeamSearch(rakeWidth, beamWidth, rank,
                filterWidth, maxDegreeOfParallelism: maxDegreeOfParallelism).BestQualityState;
        }

        public static Task<TState> ParallelPilotMethodAsync<TState, TQuality>(
            this IState<TState, TQuality> state, Lookahead<TState, TQuality> lookahead = null,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken),
            int maxDegreeOfParallelism = -1)
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelPilotMethod((TState)state, lookahead, filterWidth, depthLimit,
                runtime, nodelimit, callback, token, maxDegreeOfParallelism)
            );
        }

        public static TState ParallelPilotMethod<TState, TQuality>(
            this IState<TState, TQuality> state, Lookahead<TState, TQuality> lookahead = null,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken),
            int maxDegreeOfParallelism = -1)
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (lookahead == null) lookahead = LA.DFSLookahead<TState, TQuality>(filterWidth: 1);
            return control.ParallelPilotMethod(lookahead, filterWidth, depthLimit,
                maxDegreeOfParallelism).BestQualityState;
        }

        public static Task<TState> ParallelPilotMethodAsync<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state,
            Lookahead<TState, TChoice, TQuality> lookahead = null,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken),
            int maxDegreeOfParallelism = -1)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelPilotMethod<TState, TChoice, TQuality>(
                (TState)state, lookahead, filterWidth, depthLimit, runtime, nodelimit,
                callback, token, maxDegreeOfParallelism)
            );
        }

        public static TState ParallelPilotMethod<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state,
            Lookahead<TState, TChoice, TQuality> lookahead = null,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken),
            int maxDegreeOfParallelism = -1)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (lookahead == null) lookahead = LA.DFSLookahead<TState, TChoice, TQuality>(filterWidth: 1);
            return control.ParallelPilotMethod(lookahead, filterWidth, depthLimit,
                maxDegreeOfParallelism).BestQualityState;
        }
    }
}