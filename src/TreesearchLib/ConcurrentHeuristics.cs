using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TreesearchLib
{
    public static class ConcurrentHeuristics {
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
        public static Task<SearchControl<T, Q>> ParallelBeamSearchAsync<T, Q>(this SearchControl<T, Q> control, int beamWidth, Func<T, float> rank, int filterWidth = int.MaxValue, int maxDegreeOfParallelism = 4)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelBeamSearch(control, beamWidth, rank, filterWidth, maxDegreeOfParallelism));
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
        public static SearchControl<T, Q> ParallelBeamSearch<T, Q>(this SearchControl<T, Q> control, int beamWidth, Func<T, float> rank, int filterWidth = int.MaxValue, int maxDegreeOfParallelism = 4)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            DoParallelBeamSearch(control, control.InitialState, beamWidth, rank, filterWidth, maxDegreeOfParallelism);
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
        public static Task<SearchControl<T, C, Q>> ParallelBeamSearchAsync<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth, Func<T, float> rank, int filterWidth = int.MaxValue, int maxDegreeOfParallelism = 4)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelBeamSearch(control, beamWidth, rank, filterWidth, maxDegreeOfParallelism));
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
        public static SearchControl<T, C, Q> ParallelBeamSearch<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth, Func<T, float> rank, int filterWidth = int.MaxValue, int maxDegreeOfParallelism = 4)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            DoParallelBeamSearch<T, C, Q>(control, control.InitialState, beamWidth, rank, filterWidth, maxDegreeOfParallelism);
            return control;
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
        public static void DoParallelBeamSearch<T, Q>(ISearchControl<T, Q> control, T state, int beamWidth, Func<T, float> rank, int filterWidth, int maxDegreeOfParallelism)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (rank == null) throw new ArgumentNullException(nameof(rank));
            if (filterWidth == 1 && beamWidth > 1) throw new ArgumentException($"{nameof(beamWidth)} cannot exceed 1 when {nameof(filterWidth)} equals 1.");
            if (maxDegreeOfParallelism <= 0) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be greater or equal than 1", nameof(maxDegreeOfParallelism));

            var currentLayer = new List<T>();
            currentLayer.Add(state);
            while (!control.ShouldStop() && currentLayer.Count > 0)
            {
                var nextlayer = new List<(float rank, T state)>();
                
                var locker = new object();
                Parallel.ForEach(currentLayer, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                (currentState) =>
                {
                    var localNextLayer = new Queue<(float rank, T state)>();
                    var localControl = SearchControl<T, Q>.Start(currentState);
                    if (control.BestQuality.HasValue)
                    {
                        localControl = localControl.WithUpperBound<T, Q>(control.BestQuality.Value);
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
        public static void DoParallelBeamSearch<T, C, Q>(ISearchControl<T, Q> control, T state, int beamWidth, Func<T, float> rank, int filterWidth, int maxDegreeOfParallelism)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (rank == null) throw new ArgumentNullException(nameof(rank));
            if (filterWidth == 1 && beamWidth > 1) throw new ArgumentException($"{nameof(beamWidth)} cannot exceed 1 when {nameof(filterWidth)} equals 1.");
            if (maxDegreeOfParallelism <= 0) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be greater or equal than 1", nameof(maxDegreeOfParallelism));
            
            var currentLayer = new List<T>(beamWidth);
            currentLayer.Add(state);
            while (!control.ShouldStop() && currentLayer.Count > 0)
            {
                var nextlayer = new List<(float rank, T state)>();
                
                var locker = new object();
                Parallel.ForEach(currentLayer, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                (currentState) =>
                {
                    var localNextLayer = new Queue<(float rank, T state)>();
                    var localControl = SearchControl<T, C, Q>.Start(currentState);
                    if (control.BestQuality.HasValue)
                    {
                        localControl = localControl.WithUpperBound<T, C, Q>(control.BestQuality.Value);
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
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, an efficient beam search may be used for the lookahead.
        /// The lookahead depth is not configurable, instead a full solution must be achieved.
        /// </summary>
        /// <remarks>
        /// Voßs, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="beamWidth">The parameter that governs how many parallel lines through the search tree should be considered during lookahead. For values > 1, rank must be defined as BeamSearch will be used.</param>
        /// <param name="rank">A function that ranks states (lower is better), if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <param name="filterWidth">How many descendents will be considered per node (in case beamWidth > 1)</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use for the lookahead.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static Task<SearchControl<T, Q>> ParallelPilotMethodAsync<T, Q>(this SearchControl<T, Q> control, int beamWidth = 1, Func<T, float> rank = null, int filterWidth = 1, int maxDegreeOfParallelism = 4)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelPilotMethod(control, beamWidth, rank, filterWidth, maxDegreeOfParallelism));
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, an efficient beam search may be used for the lookahead.
        /// The lookahead depth is not configurable, instead a full solution must be achieved.
        /// </summary>
        /// <remarks>
        /// Voßs, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="beamWidth">The parameter that governs how many parallel lines through the search tree should be considered during lookahead. For values > 1, rank must be defined as BeamSearch will be used.</param>
        /// <param name="rank">A function that ranks states (lower is better), if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <param name="filterWidth">How many descendents will be considered per node (in case beamWidth > 1)</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use for the search.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static SearchControl<T, Q> ParallelPilotMethod<T, Q>(this SearchControl<T, Q> control, int beamWidth = 1, Func<T, float> rank = null, int filterWidth = 1, int maxDegreeOfParallelism = 4)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            var state = control.InitialState;
            DoParallelPilotMethod<T, Q>(control, state, beamWidth, rank, filterWidth, maxDegreeOfParallelism);
            return control;
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, an efficient beam search may be used for the lookahead.
        /// The lookahead depth is not configurable, instead a full solution must be achieved.
        /// </summary>
        /// <remarks>
        /// Voßs, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="state">The state from which the pilot method should start operating</param>
        /// <param name="beamWidth">The parameter that governs how many parallel lines through the search tree should be considered during lookahead. For values > 1, rank must be defined as BeamSearch will be used.</param>
        /// <param name="rank">A function that ranks states (lower is better), if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <param name="filterWidth">How many descendents will be considered per node (in case beamWidth > 1)</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns></returns>
        public static void DoParallelPilotMethod<T, Q>(ISearchControl<T, Q> control, T state, int beamWidth, Func<T, float> rank, int filterWidth, int maxDegreeOfParallelism)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (rank != null && beamWidth <= 0) throw new ArgumentException($"{beamWidth} needs to be greater or equal than 1 when beam search is used ({nameof(rank)} is non-null)", nameof(beamWidth));
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (filterWidth == 1 && beamWidth > 1) throw new ArgumentException($"{nameof(beamWidth)} parameter has no effect if {nameof(filterWidth)} is equal to 1", nameof(beamWidth));
            
            while (true)
            {
                T bestBranch = default(T);
                Q? bestBranchQuality = null;
                var locker = new object();
                var branches = state.GetBranches().ToList();
                if (branches.Count == 0) return;
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
                            if (rank == null)
                            {
                                // the depth search state is a stack
                                var searchState = new LIFOCollection<(int depth, T state)>((0, next));
                                while (!control.ShouldStop() && searchState.Nodes > 0)
                                {
                                    var localControl = SearchControl<T, Q>.Start(next).WithRuntimeLimit(TimeSpan.FromSeconds(1));
                                    if (control.BestQuality.HasValue)
                                    {
                                        localControl = localControl.WithUpperBound(control.BestQuality.Value);
                                    }
                                    Algorithms.DoDepthSearch(localControl, searchState, filterWidth: filterWidth, depthLimit: int.MaxValue);
                                    localControl.Finish();
                                    if (localControl.BestQuality.HasValue) quality = localControl.BestQuality;
                                    lock (locker)
                                    {
                                        control.Merge(localControl);
                                    }                                    
                                }
                            } else
                            {
                                // the beam search state is a special collection
                                var searchState = new PriorityBiLevelFIFOCollection<T>(next);
                                while (!control.ShouldStop() && searchState.CurrentLayerNodes > 0)
                                {
                                    var localControl = SearchControl<T, Q>.Start(next).WithRuntimeLimit(TimeSpan.FromSeconds(1));
                                    if (control.BestQuality.HasValue)
                                    {
                                        localControl = localControl.WithUpperBound(control.BestQuality.Value);
                                    }
                                    Heuristics.DoBeamSearch(localControl, searchState, beamWidth: beamWidth, rank: rank, filterWidth: filterWidth);
                                    localControl.Finish();
                                    if (localControl.BestQuality.HasValue) quality = localControl.BestQuality;
                                    lock (locker)
                                    {
                                        control.Merge(localControl);
                                    }
                                }
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
                if (!bestBranchQuality.HasValue) return;
                state = bestBranch;
                if (state.IsTerminal) return;
            }
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, an efficient beam search may be used for the lookahead.
        /// The lookahead depth is not configurable, instead a full solution must be achieved.
        /// </summary>
        /// <remarks>
        /// Voßs, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="beamWidth">The parameter that governs how many parallel lines through the search tree should be considered during lookahead. For values > 1, rank must be defined as BeamSearch will be used.</param>
        /// <param name="rank">A function that ranks states (lower is better), if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <param name="filterWidth">How many descendents will be considered per node (in case beamWidth > 1)</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static Task<SearchControl<T, C, Q>> ParallelPilotMethodAsync<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth = 1, Func<T, float> rank = null, int filterWidth = 1, int maxDegreeOfParallelism = 4)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelPilotMethod(control, beamWidth, rank, filterWidth, maxDegreeOfParallelism));
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, an efficient beam search may be used for the lookahead.
        /// The lookahead depth is not configurable, instead a full solution must be achieved.
        /// </summary>
        /// <remarks>
        /// Voßs, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="beamWidth">The parameter that governs how many parallel lines through the search tree should be considered during lookahead. For values > 1, rank must be defined as BeamSearch will be used.</param>
        /// <param name="rank">A function that ranks states (lower is better), if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <param name="filterWidth">How many descendents will be considered per node (in case beamWidth > 1)</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static SearchControl<T, C, Q> ParallelPilotMethod<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth = 1, Func<T, float> rank = null, int filterWidth = 1, int maxDegreeOfParallelism = 4)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            DoParallelPilotMethod<T, C, Q>(control, state, beamWidth, rank, filterWidth, maxDegreeOfParallelism);
            return control;
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, an efficient beam search may be used for the lookahead.
        /// The lookahead depth is not configurable, instead a full solution must be achieved.
        /// </summary>
        /// <remarks>
        /// Voßs, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="state">The state from which the pilot method should start operating</param>
        /// <param name="beamWidth">The parameter that governs how many parallel lines through the search tree should be considered during lookahead. For values > 1, rank must be defined as BeamSearch will be used.</param>
        /// <param name="rank">A function that ranks states (lower is better), if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <param name="filterWidth">How many descendents will be considered per node (in case beamWidth > 1)</param>
        /// <param name="maxDegreeOfParallelism">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns></returns>
        public static void DoParallelPilotMethod<T, C, Q>(ISearchControl<T, Q> control, T state, int beamWidth, Func<T, float> rank, int filterWidth, int maxDegreeOfParallelism)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (rank != null && beamWidth <= 0) throw new ArgumentException($"{beamWidth} needs to be greater or equal than 1 when beam search is used ({nameof(rank)} is non-null)", nameof(beamWidth));
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (filterWidth == 1 && beamWidth > 1) throw new ArgumentException($"{nameof(beamWidth)} parameter has no effect if {nameof(filterWidth)} is equal to 1", nameof(beamWidth));
            if (maxDegreeOfParallelism <= 0) throw new ArgumentException($"{maxDegreeOfParallelism} needs to be greater or equal than 1", nameof(maxDegreeOfParallelism));

            while (true)
            {
                T bestBranch = default(T);
                Q? bestBranchQuality = null;
                var locker = new object();
                var branches = state.GetChoices().Select(c => { var clone = (T)state.Clone(); clone.Apply(c); return clone; }).ToList();
                if (branches.Count == 0) return;
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
                            if (rank == null)
                            {
                                // the depth search state is a stack
                                var localDepth = 0;
                                var searchState = new LIFOCollection<(int depth, C choice)>();
                                foreach (var choice in next.GetChoices()) {
                                    searchState.Store((localDepth, choice));
                                }
                                while (!control.ShouldStop() && searchState.Nodes > 0)
                                {
                                    var localControl = SearchControl<T, C, Q>.Start(next).WithRuntimeLimit(TimeSpan.FromSeconds(1));
                                    if (control.BestQuality.HasValue)
                                    {
                                        localControl = localControl.WithUpperBound(control.BestQuality.Value);
                                    }
                                    localDepth = Algorithms.DoDepthSearch<T, C, Q>(localControl, next, searchState, localDepth, filterWidth: filterWidth, depthLimit: int.MaxValue);
                                    localControl.Finish();
                                    if (localControl.BestQuality.HasValue) quality = localControl.BestQuality;
                                    lock (locker)
                                    {
                                        control.Merge(localControl);
                                    }                                    
                                }
                            } else
                            {
                                // the beam search state is a special collection
                                var searchState = new PriorityBiLevelFIFOCollection<T>(next);
                                while (!control.ShouldStop() && searchState.CurrentLayerNodes > 0)
                                {
                                    var localControl = SearchControl<T, C, Q>.Start(next).WithRuntimeLimit(TimeSpan.FromSeconds(1));
                                    if (control.BestQuality.HasValue)
                                    {
                                        localControl = localControl.WithUpperBound(control.BestQuality.Value);
                                    }
                                    Heuristics.DoBeamSearch<T, C, Q>(localControl, searchState, beamWidth: beamWidth, rank: rank, filterWidth: filterWidth);
                                    localControl.Finish();
                                    if (localControl.BestQuality.HasValue) quality = localControl.BestQuality;
                                    lock (locker)
                                    {
                                        control.Merge(localControl);
                                    }
                                }
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
                if (!bestBranchQuality.HasValue) return;
                state = bestBranch;
                if (state.IsTerminal) return;
            }
        }
    }

    public static class ParallelHeuristicStateExtensions
    {
        public static Task<TState> ParallelBeamSearchAsync<TState, TQuality>(this IState<TState, TQuality> state, Func<TState, float> rank,
                 int beamWidth = 100, int filterWidth = int.MaxValue, TimeSpan? runtime = null,
                long? nodelimit = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken), int maxDegreeOfParallelism = 4)
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelBeamSearch((TState)state, rank, beamWidth, filterWidth, runtime, nodelimit, callback, token, maxDegreeOfParallelism));
        }
        public static TState ParallelBeamSearch<TState, TQuality>(this IState<TState, TQuality> state, Func<TState, float> rank,
                int beamWidth = 100, int filterWidth = int.MaxValue, TimeSpan? runtime = null,
                long? nodelimit = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken), int maxDegreeOfParallelism = 4)
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.ParallelBeamSearch(beamWidth, rank, filterWidth, maxDegreeOfParallelism).BestQualityState;
        }

        public static Task<TState> ParallelBeamSearchAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, Func<TState, float> rank,
                int beamWidth = 100, int filterWidth = int.MaxValue, TimeSpan? runtime = null,
                long? nodelimit = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken), int maxDegreeOfParallelism = 4)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelBeamSearch<TState, TChoice, TQuality>((TState)state, rank, beamWidth, filterWidth, runtime, nodelimit, callback, token, maxDegreeOfParallelism));
        }

        public static TState ParallelBeamSearch<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, Func<TState, float> rank,
                int beamWidth = 100, int filterWidth = int.MaxValue, TimeSpan? runtime = null,
                long? nodelimit = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken), int maxDegreeOfParallelism = 4)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.ParallelBeamSearch(beamWidth, rank, filterWidth, maxDegreeOfParallelism).BestQualityState;
        }

        public static Task<TState> ParallelPilotMethodAsync<TState, TQuality>(this IState<TState, TQuality> state,
                int beamWidth = 1, Func<TState, float> rank = null, int filterWidth = int.MaxValue,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken), int maxDegreeOfParallelism = 4)
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelPilotMethod((TState)state, beamWidth, rank, filterWidth, runtime, nodelimit, callback, token, maxDegreeOfParallelism));
        }

        public static TState ParallelPilotMethod<TState, TQuality>(this IState<TState, TQuality> state,
                int beamWidth = 1, Func<TState, float> rank = null, int filterWidth = int.MaxValue,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken), int maxDegreeOfParallelism = 4)
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.ParallelPilotMethod(beamWidth, rank, filterWidth, maxDegreeOfParallelism).BestQualityState;
        }

        public static Task<TState> ParallelPilotMethodAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state,
                int beamWidth = 1, Func<TState, float> rank = null, int filterWidth = int.MaxValue,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken), int maxDegreeOfParallelism = 4)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => ParallelPilotMethod<TState, TChoice, TQuality>((TState)state, beamWidth, rank, filterWidth, runtime, nodelimit, callback, token, maxDegreeOfParallelism));
        }

        public static TState ParallelPilotMethod<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state,
                int beamWidth = 1, Func<TState, float> rank = null, int filterWidth = int.MaxValue,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken), int maxDegreeOfParallelism = 4)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.ParallelPilotMethod(beamWidth, rank, filterWidth, maxDegreeOfParallelism).BestQualityState;
        }
    }
}