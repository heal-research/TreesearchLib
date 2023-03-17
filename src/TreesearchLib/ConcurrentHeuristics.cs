using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="threads">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static Task<SearchControl<T, Q>> ParallelBeamSearchAsync<T, Q>(this SearchControl<T, Q> control, int beamWidth, Func<T, float> rank, int filterWidth = int.MaxValue, int threads = 4)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelBeamSearch(control, beamWidth, rank, filterWidth, threads));
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
        /// <param name="threads">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static SearchControl<T, Q> ParallelBeamSearch<T, Q>(this SearchControl<T, Q> control, int beamWidth, Func<T, float> rank, int filterWidth = int.MaxValue, int threads = 4)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            DoParallelBeamSearch(control, control.InitialState, beamWidth, rank, filterWidth, threads);
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
        /// <param name="threads">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static Task<SearchControl<T, C, Q>> ParallelBeamSearchAsync<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth, Func<T, float> rank, int filterWidth = int.MaxValue, int threads = 4)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelBeamSearch(control, beamWidth, rank, filterWidth, threads));
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
        /// <param name="threads">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static SearchControl<T, C, Q> ParallelBeamSearch<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth, Func<T, float> rank, int filterWidth = int.MaxValue, int threads = 4)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            DoParallelBeamSearch<T, C, Q>(control, control.InitialState, beamWidth, rank, filterWidth, threads);
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
        /// <param name="threads">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void DoParallelBeamSearch<T,Q>(ISearchControl<T, Q> control, T state, int beamWidth, Func<T, float> rank, int filterWidth, int threads)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (rank == null) throw new ArgumentNullException(nameof(rank));
            if (filterWidth == 1 && beamWidth > 1) throw new ArgumentException($"{nameof(beamWidth)} cannot exceed 1 when {nameof(filterWidth)} equals 1.");
            if (threads <= 0) throw new ArgumentException($"{threads} needs to be greater or equal than 1", nameof(threads));

            var currentLayer = new List<T>();
            currentLayer.Add(state);
            while (!control.ShouldStop())
            {
                var nextlayer = new ConcurrentQueue<(float rank, T state)>();
                Parallel.ForEach(currentLayer, new ParallelOptions { MaxDegreeOfParallelism = beamWidth },
                (currentState) =>
                {
                    foreach (var next in currentState.GetBranches().Take(filterWidth))
                    {
                        nextlayer.Enqueue((rank(next), next));
                    }
                });

                currentLayer.Clear();
                
                foreach (var next in nextlayer
                        .OrderBy(x => x.rank)
                        .Where(x => control.VisitNode(x.state) != VisitResult.Discard)
                        .Take(beamWidth)
                        .Select(x => x.state)) {
                    currentLayer.Add(next);
                }

                if (currentLayer.Count == 0 || control.ShouldStop())
                {
                    break;
                }
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
        /// <param name="threads">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void DoParallelBeamSearch<T, C, Q>(ISearchControl<T, Q> control, T state, int beamWidth, Func<T, float> rank, int filterWidth, int threads)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (rank == null) throw new ArgumentNullException(nameof(rank));
            if (filterWidth == 1 && beamWidth > 1) throw new ArgumentException($"{nameof(beamWidth)} cannot exceed 1 when {nameof(filterWidth)} equals 1.");
            if (threads <= 0) throw new ArgumentException($"{threads} needs to be greater or equal than 1", nameof(threads));
            
            var currentLayer = new List<T>();
            currentLayer.Add(state);
            while (!control.ShouldStop())
            {
                var nextlayer = new ConcurrentQueue<(float rank, T state)>();
                
                Parallel.ForEach(currentLayer, new ParallelOptions { MaxDegreeOfParallelism = threads },
                (currentState) =>
                {
                    foreach (var choice in currentState.GetChoices().Take(filterWidth))
                    {
                        var next = (T)currentState.Clone();
                        next.Apply(choice);

                        nextlayer.Enqueue((rank(next), next));
                    }
                });

                currentLayer.Clear();
                
                foreach (var next in nextlayer
                        .OrderBy(x => x.rank)
                        .Where(x => control.VisitNode(x.state) != VisitResult.Discard)
                        .Take(beamWidth)
                        .Select(x => x.state)) {
                    currentLayer.Add(next);
                }

                if (currentLayer.Count == 0 || control.ShouldStop())
                {
                    break;
                }
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
        /// <param name="threads">The maximum number of threads to use for the lookahead.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static Task<SearchControl<T, Q>> ParallelPilotMethodAsync<T, Q>(this SearchControl<T, Q> control, int beamWidth = 1, Func<T, float> rank = null, int filterWidth = 1, int threads = 4)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelPilotMethod(control, beamWidth, rank, filterWidth, threads));
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
        /// <param name="threads">The maximum number of threads to use for the search.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static SearchControl<T, Q> ParallelPilotMethod<T, Q>(this SearchControl<T, Q> control, int beamWidth = 1, Func<T, float> rank = null, int filterWidth = 1, int threads = 4)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            var state = control.InitialState;
            DoParallelPilotMethod<T, Q>(control, state, beamWidth, rank, filterWidth, threads);
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
        /// <param name="threads">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns></returns>
        public static void DoParallelPilotMethod<T, Q>(ISearchControl<T, Q> control, T state, int beamWidth, Func<T, float> rank, int filterWidth, int threads)
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
                var guard = new WrappedThreadSafeSearchControl<T, Q>(control); // wrap the control to make it thread-safe
                Parallel.ForEach(state.GetBranches(),
                    new ParallelOptions() { MaxDegreeOfParallelism = threads },
                    next => {
                        Q? quality;
                        if (next.IsTerminal)
                        {
                            // no lookahead required
                            quality = next.Quality;
                        } else
                        {
                            // wrap the search control, to do best quality tracking for this particular lookahead run only
                            var wrappedControl = new WrappedSearchControl<T, Q>(guard);
                            if (rank == null)
                            {
                                Algorithms.DoDepthSearch(wrappedControl, next, filterWidth: filterWidth);
                            } else
                            {
                                Heuristics.DoBeamSearch(wrappedControl, next, beamWidth: beamWidth, rank: rank, filterWidth: filterWidth);
                            }
                            quality = wrappedControl.BestQuality;
                        }

                        if (!quality.HasValue) return; // no solution achieved
                        lock (locker) {
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
        /// <param name="threads">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static Task<SearchControl<T, C, Q>> ParallelPilotMethodAsync<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth = 1, Func<T, float> rank = null, int filterWidth = 1, int threads = 4)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => ParallelPilotMethod(control, beamWidth, rank, filterWidth, threads));
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
        /// <param name="threads">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static SearchControl<T, C, Q> ParallelPilotMethod<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth = 1, Func<T, float> rank = null, int filterWidth = 1, int threads = 4)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            DoPilotMethod<T, C, Q>(control, state, beamWidth, rank, filterWidth, threads);
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
        /// <param name="threads">The maximum number of threads to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns></returns>
        public static void DoPilotMethod<T, C, Q>(ISearchControl<T, Q> control, T state, int beamWidth, Func<T, float> rank, int filterWidth, int threads)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (rank != null && beamWidth <= 0) throw new ArgumentException($"{beamWidth} needs to be greater or equal than 1 when beam search is used ({nameof(rank)} is non-null)", nameof(beamWidth));
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (filterWidth == 1 && beamWidth > 1) throw new ArgumentException($"{nameof(beamWidth)} parameter has no effect if {nameof(filterWidth)} is equal to 1", nameof(beamWidth));
            if (threads <= 0) throw new ArgumentException($"{threads} needs to be greater or equal than 1", nameof(threads));

            while (true)
            {
                C bestBranch = default(C);
                Q? bestBranchQuality = null;
                var locker = new object();
                var guard = new WrappedThreadSafeSearchControl<T, C, Q>(control); // wrap the control to make it thread safe
                Parallel.ForEach(state.GetChoices().ToList(),
                    new ParallelOptions() { MaxDegreeOfParallelism = threads },
                    choice =>
                    {
                        Q? quality;
                        var next = (T)state.Clone();
                        next.Apply(choice);
                        if (next.IsTerminal)
                        {
                            // no greedy lookahead required
                            quality = next.Quality;
                        } else
                        {
                            // wrap the search control, to do best quality tracking for this particular lookaheaed run only
                            var wrappedControl = new WrappedSearchControl<T, C, Q>(guard);
                            if (rank == null)
                            {
                                Algorithms.DoDepthSearch<T, C, Q>(wrappedControl, next, filterWidth: filterWidth);
                            } else
                            {
                                // use beam search as greedy lookahead
                                Heuristics.DoBeamSearch<T, C, Q>(wrappedControl, next, beamWidth: beamWidth, rank: rank, filterWidth: filterWidth);
                            }
                            quality = wrappedControl.BestQuality;
                        }

                        if (!quality.HasValue) return; // no solution achieved
                        lock (locker)
                        {
                            if (!bestBranchQuality.HasValue || quality.Value.IsBetter(bestBranchQuality.Value))
                            {
                                bestBranch = choice;
                                bestBranchQuality = quality;
                            }
                        }
                    }
                );
                if (!bestBranchQuality.HasValue) return;
                state.Apply(bestBranch);
            }
        }
    }
}