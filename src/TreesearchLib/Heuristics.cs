using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TreesearchLib
{
    public static class Heuristics
    {
        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The state to start the search from</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes (lower is better)</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <param name="depthLimit">The maximum depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void BeamSearch<T, Q>(ISearchControl<T, Q> control, T state,
            int beamWidth, Func<T, float> rank, int filterWidth, int depthLimit)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (rank == null) throw new ArgumentNullException(nameof(rank));
            if (filterWidth == 1 && beamWidth > 1) throw new ArgumentException($"{nameof(beamWidth)} cannot exceed 1 when {nameof(filterWidth)} equals 1.");
            if (depthLimit <= 0) throw new ArgumentException($"{nameof(depthLimit)} must greater than 0.", nameof(depthLimit));

            var searchState = new PriorityBiLevelFIFOCollection<T>(state);
            BeamSearch(control, searchState, 0, beamWidth, rank, filterWidth, depthLimit);
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="searchState">The algorithm's inner state</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes (lower is better)</param>
        /// <param name="depth">The current depth of the search</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <param name="depthLimit">The maximum depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void BeamSearch<T, Q>(ISearchControl<T, Q> control,
            PriorityBiLevelFIFOCollection<T> searchState, int depth, int beamWidth,
            Func<T, float> rank, int filterWidth, int depthLimit)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            while (!control.ShouldStop() && searchState.CurrentLayerNodes > 0 && depth < depthLimit)
            {
                while (!control.ShouldStop() && searchState.TryFromCurrentLayerQueue(out var currentState))
                {
                    foreach (var next in currentState.GetBranches().Take(filterWidth))
                    {
                        if (control.VisitNode(next) == VisitResult.Discard)
                        {
                            continue;
                        }

                        searchState.ToNextLayerQueue(next, rank(next));
                    }
                }
                if (searchState.CurrentLayerNodes == 0)
                {
                    searchState.AdvanceLayer(beamWidth);
                    depth++;
                }
            }
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The state to start the search from</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes (lower is better)</param>
        /// <param name="depth">The current depth of the search</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <param name="depthLimit">The maximum depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void BeamSearch<T, C, Q>(ISearchControl<T, Q> control, T state, int depth,
            int beamWidth, Func<T, float> rank, int filterWidth, int depthLimit)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (rank == null) throw new ArgumentNullException(nameof(rank));
            if (filterWidth == 1 && beamWidth > 1) throw new ArgumentException($"{nameof(beamWidth)} cannot exceed 1 when {nameof(filterWidth)} equals 1.");
            if (depthLimit <= 0) throw new ArgumentException($"{nameof(depthLimit)} must greater than 0.", nameof(depthLimit));

            var searchState = new PriorityBiLevelFIFOCollection<T>(state);
            BeamSearch<T, C, Q>(control, searchState, beamWidth, rank, depth, filterWidth, depthLimit);
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="searchState">The algorithm's inner state</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes (lower is better)</param>
        /// <param name="depth">The current depth of the search</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <param name="depthLimit">The maximum depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void BeamSearch<T, C, Q>(ISearchControl<T, Q> control,
            PriorityBiLevelFIFOCollection<T> searchState, int beamWidth,
            Func<T, float> rank, int depth, int filterWidth, int depthLimit)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            while (!control.ShouldStop() && searchState.CurrentLayerNodes > 0 && depth < depthLimit)
            {
                while (!control.ShouldStop() && searchState.TryFromCurrentLayerQueue(out var currentState))
                {
                    foreach (var choice in currentState.GetChoices().Take(filterWidth))
                    {
                        var next = (T)currentState.Clone();
                        next.Apply(choice);
                        if (control.VisitNode(next) == VisitResult.Discard)
                        {
                            continue;
                        }

                        searchState.ToNextLayerQueue(next, rank(next));
                    }
                }
                if (searchState.CurrentLayerNodes == 0)
                {
                    searchState.AdvanceLayer(beamWidth);
                    depth++;
                }
            }
        }

        /// <summary>
        /// Rake search performs a breadth-first search until a level is reached with <paramref name="rakeWidth"/>
        /// nodes and then from each node a lookahead with the given method is performed.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The state to start the search from</param>
        /// <param name="rakeWidth">The number of nodes to reach, before proceeding with a simple greedy heuristic</param>
        /// <param name="lookahead">The lookahead method to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static void RakeSearch<T, Q>(
            ISearchControl<T, Q> control, T state, int rakeWidth, Lookahead<T, Q> lookahead)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (rakeWidth <= 0) throw new ArgumentException($"{nameof(rakeWidth)} must be greater than 0", nameof(rakeWidth));
            if (lookahead == null) throw new ArgumentNullException(nameof(lookahead));

            var (_, rake) = Algorithms.BreadthSearch(control, state, 0, filterWidth: int.MaxValue, depthLimit: int.MaxValue, rakeWidth);
            var i = 0;
            while (i < rakeWidth && !control.ShouldStop() && rake.TryGetNext(out var next))
            {
                lookahead(control, next);
                i++;
            }
        }

        /// <summary>
        /// Rake search performs a breadth-first search until a level is reached with <paramref name="rakeWidth"/>
        /// nodes and then from each node a lookahead with the given method is performed.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The state to start the search from</param>
        /// <param name="rakeWidth">The number of nodes to reach, before proceeding with a simple greedy heuristic</param>
        /// <param name="lookahead">The lookahead method to use</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static void RakeSearch<T, C, Q>(
            ISearchControl<T, Q> control, T state, int rakeWidth, Lookahead<T, C, Q> lookahead)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (rakeWidth <= 0) throw new ArgumentException($"{nameof(rakeWidth)} must be greater than 0", nameof(rakeWidth));
            if (lookahead == null) throw new ArgumentNullException(nameof(lookahead));

            var (_, rake) = Algorithms.BreadthSearch<T, C, Q>(control, state, depth: 0, filterWidth: int.MaxValue, depthLimit: int.MaxValue, rakeWidth);
            var i = 0;
            while (i < rakeWidth && !control.ShouldStop() && rake.TryGetNext(out var next))
            {
                lookahead(control, next);
                i++;
            }
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, the lookahead can be configured. During lookahead, at least a state with
        /// a quality must be achieved to identify the better branch.
        /// </summary>
        /// <remarks>
        /// Voßs, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="state">The state from which the pilot method should start operating</param>
        /// <param name="depth">The depth of the current state</param>
        /// <param name="lookahead">The lookahead method to use</param>
        /// <param name="filterWidth">How many descendents will be considered for lookahead</param>
        /// <param name="depthLimit">The maximum depth of the pilot search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns></returns>
        public static void PilotMethod<T, Q>(ISearchControl<T, Q> control, T state, int depth,
                Lookahead<T, Q> lookahead, int filterWidth, int depthLimit)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (depthLimit <= 0) throw new ArgumentException($"{depthLimit} needs to be greater than 0", nameof(depthLimit));
            if (lookahead == null) throw new ArgumentNullException(nameof(lookahead));

            while (!control.ShouldStop() && depth < depthLimit)
            {
                T bestBranch = default(T);
                Q? bestBranchQuality = null;
                var first = false;
                foreach (var next in state.GetBranches().Take(filterWidth))
                {
                    if (!first)
                    {
                        bestBranch = next; // remember first branch, if all lookahead fail to reach a terminal state
                        first = true;
                    }
                    Q? quality;
                    if (next.IsTerminal)
                    {
                        // no lookahead required
                        quality = next.Quality;
                    } else
                    {
                        // wrap the search control, to do best quality tracking for this particular lookahead run only
                        var wrappedControl = new WrappedSearchControl<T, Q>(control);
                        lookahead(wrappedControl, next);
                        quality = wrappedControl.BestQuality;
                    }

                    if (!quality.HasValue) continue; // no solution achieved
                    if (!bestBranchQuality.HasValue || quality.Value.IsBetter(bestBranchQuality.Value))
                    {
                        bestBranch = next;
                        bestBranchQuality = quality;
                    }
                }
                if (!bestBranchQuality.HasValue && !first)
                {
                    return; // no more branches
                }
                state = bestBranch;
                depth++;
            }
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, the lookahead can be configured. During lookahead, at least a state with
        /// a quality must be achieved to identify the better branch.
        /// </summary>
        /// <remarks>
        /// Voßs, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="state">The state from which the pilot method should start operating</param>
        /// <param name="depth">The depth of the current state</param>
        /// <param name="lookahead">The lookahead method to use</param>
        /// <param name="filterWidth">How many descendents will be considered for lookahead</param>
        /// <param name="depthLimit">The maximum depth of the pilot search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns></returns>
        public static void PilotMethod<T, C, Q>(ISearchControl<T, Q> control, T state, int depth,
                Lookahead<T, C, Q> lookahead, int filterWidth, int depthLimit)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (depthLimit <= 0) throw new ArgumentException($"{depthLimit} needs to be greater than 0", nameof(depthLimit));
            if (lookahead == null) throw new ArgumentNullException(nameof(lookahead));

            while (!control.ShouldStop() && depth < depthLimit)
            {
                C bestBranch = default(C);
                Q? bestBranchQuality = null;
                var first = false;
                foreach (var choice in state.GetChoices().Take(filterWidth).ToList())
                {
                    if (!first)
                    {
                        bestBranch = choice; // remember first branch, if all lookahead fail to reach a terminal state
                        first = true;
                    }
                    Q? quality;
                    state.Apply(choice);
                    
                    if (state.IsTerminal)
                    {
                        // no greedy lookahead required
                        quality = state.Quality;
                    } else
                    {
                        var wrappedControl = new WrappedSearchControl<T, C, Q>(control);
                        lookahead(wrappedControl, state);
                        quality = wrappedControl.BestQuality;
                    }

                    state.UndoLast();

                    if (!quality.HasValue) continue; // no solution achieved
                    if (!bestBranchQuality.HasValue || quality.Value.IsBetter(bestBranchQuality.Value))
                    {
                        bestBranch = choice;
                        bestBranchQuality = quality;
                    }
                }
                if (!bestBranchQuality.HasValue && !first)
                {
                    return; // no more choices
                }
                state.Apply(bestBranch);
                depth++;
            }
        }

        /// <summary>
        /// The limited discrepancy (LD) search assumes branches are sorted according to a heuristic and generally taking the
        /// first branch leads to better outcomes. It assumes that there is a discrepancy, i.e., penalty, of N for visiting the
        /// (N+1)th branch in a certain node. This penalty accumulates along the search path and the parameter
        /// <paramref name="maxDiscrepancy"/> limits this accumulated penalty. The search thus visits only part of a tree.
        /// 
        /// For instance, a maxDiscrepancy of 2 allows any leaf to be reachable where always the first branch has been picked,
        /// where at most 2 times the second branch has been picked or where at most once the third branch was picked. While this
        /// reduces the size of the search tree, it still grows quickly with increasing values of maxDiscrepancy.
        /// 
        /// The naive implementation will not progress to visit nodes with lower discrepancy before visiting those with higher
        /// discrepancies. For instance, a node with discrepancy 2 might be visited before a node with discrepancy 1. Consider
        /// the "anytime" implementation of LD search if you want to interrupt the search, before it is completed and want to
        /// ensure that all nodes with lower discrepancies are considered before those with higher ones.
        /// </summary>
        /// <remarks>
        /// Limited discrepancy search has been described by
        /// Harvey, W.D. and Ginsberg, M.L., 1995, August. Limited discrepancy search. In IJCAI (1) (pp. 607-615).
        /// 
        /// The implementation here differs, because we don't assume a fixed number of branches per node, nor that the
        /// depth is fixed and known for each branch a priori.
        /// </remarks>
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="state">The state from which to start exploring</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void NaiveLDSearch<T, Q>(ISearchControl<T, Q> control, T state, int maxDiscrepancy)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (maxDiscrepancy < 0) throw new ArgumentException(nameof(maxDiscrepancy), $"{maxDiscrepancy} must be >= 0");
            var searchState = new LIFOCollection<(T, int)>();
            searchState.Store((state, 0));

            while (searchState.TryGetNext(out var tup) && !control.ShouldStop())
            {
                var (currentState, discrepancy) = tup;
                foreach (var next in currentState.GetBranches()
                    .Select((s, i) => (state: s, discrepancy: discrepancy + i))
                    .TakeWhile(x => x.discrepancy <= maxDiscrepancy).Reverse())
                {
                    if (control.VisitNode(next.state) == VisitResult.Discard)
                    {
                        discrepancy++;
                        continue;
                    }

                    searchState.Store(next);
                    discrepancy++; // 2nd and further branches have a penalty
                }
            }
        }

        /// <summary>
        /// The limited discrepancy (LD) search assumes branches are sorted according to a heuristic and generally taking the
        /// first branch leads to better outcomes. It assumes that there is a discrepancy, i.e., penalty, of N for visiting the
        /// (N+1)th branch in a certain node. This penalty accumulates along the search path and the parameter
        /// <paramref name="maxDiscrepancy"/> limits this accumulated penalty. The search thus visits only part of a tree.
        /// 
        /// For instance, a maxDiscrepancy of 2 allows any leaf to be reachable where always the first branch has been picked,
        /// where at most 2 times the second branch has been picked or where at most once the third branch was picked. While this
        /// reduces the size of the search tree, it still grows quickly with increasing values of maxDiscrepancy.
        /// 
        /// The naive implementation will not progress to visit nodes with lower discrepancy before visiting those with higher
        /// discrepancies. For instance, a node with discrepancy 2 might be visited before a node with discrepancy 1. Consider
        /// the "anytime" implementation of LD search if you want to interrupt the search, before it is completed and want to
        /// ensure that all nodes with lower discrepancies are considered before those with higher ones.
        /// </summary>
        /// <remarks>
        /// Limited discrepancy search has been described by
        /// Harvey, W.D. and Ginsberg, M.L., 1995, August. Limited discrepancy search. In IJCAI (1) (pp. 607-615).
        /// 
        /// The implementation here differs, because we don't assume a fixed number of branches per node, nor that the
        /// depth is fixed and known for each branch a priori.
        /// </remarks>
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="state">The state from which to start exploring</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void NaiveLDSearch<T, C, Q>(ISearchControl<T, Q> control, T state, int maxDiscrepancy)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (maxDiscrepancy < 0) throw new ArgumentException(nameof(maxDiscrepancy), $"{maxDiscrepancy} must be >= 0");
            var searchState = new LIFOCollection<(int depth, C choice, int discrepancy)>();
            var stateDepth = 0;

            foreach (var entry in state.GetChoices()
                .Select((choice, i) => (depth: stateDepth, choice, discrepancy: i))
                .TakeWhile(x => x.discrepancy <= maxDiscrepancy).Reverse())
            {
                searchState.Store(entry);
            }

            while (!control.ShouldStop() && searchState.TryGetNext(out var next))
            {
                var (depth, choice, discrepancy) = next;
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

                foreach (var entry in state.GetChoices()
                    .Select((ch, i) => (depth: stateDepth, choice: ch, discrepancy: discrepancy + i))
                    .TakeWhile(x => x.discrepancy <= maxDiscrepancy).Reverse())
                {
                    searchState.Store(entry);
                }
            }
        }

        /// <summary>
        /// The limited discrepancy (LD) search assumes branches are sorted according to a heuristic and generally taking the
        /// first branch leads to better outcomes. It assumes that there is a discrepancy, i.e., penalty, of N for visiting the
        /// (N+1)th branch in a certain node. This penalty accumulates along the search path and the parameter
        /// <paramref name="maxDiscrepancy"/> limits this accumulated penalty. The search thus visits only part of a tree.
        /// 
        /// For instance, a maxDiscrepancy of 2 allows any leaf to be reachable where always the first branch has been picked,
        /// where at most 2 times the second branch has been picked or where at most once the third branch was picked. While this
        /// reduces the size of the search tree, it still grows quickly with increasing values of maxDiscrepancy.
        /// 
        /// The anytime implementation first processes all nodes with discrepancy K, before moving to nodes with discrepancy K+1.
        /// If you intend to always completely visit all reachable nodes, the naive implementation can be chosen instead. If the
        /// intent is to limit the runtime, then this implementation should be chosen.
        /// </summary>
        /// <remarks>
        /// Limited discrepancy search has been described by
        /// Harvey, W.D. and Ginsberg, M.L., 1995, August. Limited discrepancy search. In IJCAI (1) (pp. 607-615).
        /// 
        /// The implementation here differs, because we don't assume a fixed number of branches per node, nor that the
        /// depth is fixed and known for each branch a priori.
        /// </remarks>
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="state">The state from which to start exploring</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void AnytimeLDSearch<T, Q>(ISearchControl<T, Q> control, T state, int maxDiscrepancy)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (maxDiscrepancy < 0) throw new ArgumentException(nameof(maxDiscrepancy), $"{maxDiscrepancy} must be >= 0");
            var searchState = new Stack<T>[maxDiscrepancy + 1];
            for (var i = 0; i <= maxDiscrepancy; i++)
            {
                searchState[i] = new Stack<T>();
            }
            searchState[0].Push(state);
            var K = 0; // the active discrepancy that is explored
            while (K <= maxDiscrepancy && !control.ShouldStop())
            {
                var currentState = searchState[K].Pop();
                foreach (var tup in currentState.GetBranches()
                    .Select((b, i) => (state: b, discrepancy: K + i))
                    .TakeWhile(x => x.discrepancy <= maxDiscrepancy).Reverse())
                {
                    var (next, discrepancy) = tup;

                    if (control.VisitNode(next) == VisitResult.Ok)
                    {
                        searchState[discrepancy].Push(next);
                    }
                }
                while (K <= maxDiscrepancy && searchState[K].Count == 0)
                {
                    K++;
                }
            }
        }

        /// <summary>
        /// The limited discrepancy (LD) search assumes branches are sorted according to a heuristic and generally taking the
        /// first branch leads to better outcomes. It assumes that there is a discrepancy, i.e., penalty, of N for visiting the
        /// (N+1)th branch in a certain node. This penalty accumulates along the search path and the parameter
        /// <paramref name="maxDiscrepancy"/> limits this accumulated penalty. The search thus visits only part of a tree.
        /// 
        /// For instance, a maxDiscrepancy of 2 allows any leaf to be reachable where always the first branch has been picked,
        /// where at most 2 times the second branch has been picked or where at most once the third branch was picked. While this
        /// reduces the size of the search tree, it still grows quickly with increasing values of maxDiscrepancy.
        /// 
        /// The anytime implementation first processes all nodes with discrepancy K, before moving to nodes with discrepancy K+1.
        /// If you intend to always completely visit all reachable nodes, the naive implementation can be chosen instead. If the
        /// intent is to limit the runtime, then this implementation should be chosen.
        /// There is a performance penalty of the anytime version, as the search cannot rely on undoing moves and thus has to do
        /// some cloning of states.
        /// </summary>
        /// <remarks>
        /// Limited discrepancy search has been described by
        /// Harvey, W.D. and Ginsberg, M.L., 1995, August. Limited discrepancy search. In IJCAI (1) (pp. 607-615).
        /// 
        /// The implementation here differs, because we don't assume a fixed number of branches per node, nor that the
        /// depth is fixed and known for each branch a priori.
        /// </remarks>
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="state">The state from which to start exploring</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void AnytimeLDSearch<T, C, Q>(ISearchControl<T, Q> control, T state, int maxDiscrepancy)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (maxDiscrepancy < 0) throw new ArgumentException(nameof(maxDiscrepancy), $"{maxDiscrepancy} must be >= 0");
            var searchState = new Stack<(C choice, T choiceState)>[maxDiscrepancy + 1];
            for (var i = 0; i <= maxDiscrepancy; i++)
            {
                searchState[i] = new Stack<(C, T)>();
            }
            var K = 0;
            foreach (var entry in state.GetChoices()
                .Select((choice, i) => (choice, discrepancy: K + i))
                .TakeWhile(x => x.discrepancy <= maxDiscrepancy).Reverse())
            {
                searchState[entry.discrepancy].Push((entry.choice, entry.discrepancy == K ? state : (T)state.Clone()));
            }
            while (K <= maxDiscrepancy && !control.ShouldStop())
            {
                var next = searchState[K].Pop();
                var (choice, choiceState) = (next.choice, next.choiceState);
                choiceState.Apply(choice);

                if (control.VisitNode(choiceState) == VisitResult.Ok)
                {
                    foreach (var entry in choiceState.GetChoices()
                        .Select((ch, i) => (choice: ch, discrepancy: K + i))
                        .TakeWhile(x => x.discrepancy <= maxDiscrepancy).Reverse())
                    {
                        searchState[entry.discrepancy].Push((entry.choice, entry.discrepancy == K ? choiceState : (T)choiceState.Clone()));
                    }
                }
                while (K <= maxDiscrepancy && searchState[K].Count == 0)
                {
                    K++;
                }
            }
        }

        /// <summary>
        /// Monotonic beam search uses several parallel beams which are iteratively updated.
        /// Each beam may only choose among branches that beams before it have made available.
        /// This behavior ensures that the call with beamWidth = n + 1 achieves as good results
        /// as with beamWidth = n. For standard beam search this property does not hold and a
        /// bigger beam search might lead to worse results (against the general trend of achieving
        /// better solutions).
        /// </summary>
        /// <remarks>
        /// The algorithms is described in Lemons, S., López, C.L., Holte, R.C. and Ruml, W., 2022.
        /// Beam Search: Faster and Monotonic. arXiv preprint arXiv:2204.02929.
        /// </remarks>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The state to start from</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes, default is to compare bounds</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void MonotonicBeamSearch<T, Q>(ISearchControl<T, Q> control, T state,
            int beamWidth, Func<T, float> rank, int filterWidth)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (rank == null) throw new ArgumentNullException(nameof(rank));
            if (filterWidth == 1 && beamWidth > 1) throw new ArgumentException($"{nameof(beamWidth)} cannot exceed 1 when {nameof(filterWidth)} equals 1.");

            var beams = new T[beamWidth];
            beams[0] = state;
            var candidates = new Priority_Queue.StablePriorityQueue<StateNode<T, Q>>(beamWidth * 10);
            while (!Equals(beams[0], default(T)) && !control.ShouldStop())
            {
                for (var b = 0; b < beams.Length; b++)
                {
                    if (Equals(beams[b], default(T)))
                    {
                        // launch a new beam (this does not violate monotonicity, because inactive beams will be shifted to the end of the beam array)
                        if (candidates.Count > 0)
                        {
                            beams[b] = candidates.Dequeue().State;
                            continue;
                        } else break; // no more active beams will follow
                    }

                    var currentState = beams[b];

                    foreach (var next in currentState.GetBranches().Take(filterWidth))
                    {
                        if (control.VisitNode(next) == VisitResult.Discard || next.IsTerminal)
                        {
                            continue;
                        }

                        if (candidates.Count == candidates.MaxSize)
                        {
                            candidates.Resize(candidates.MaxSize * 2);
                        }
                        candidates.Enqueue(new StateNode<T, Q>(next), rank(next));
                    }

                    if (candidates.Count == 0) // no more candidates, beam will become inactive and shifted to the end
                    {
                        var k = b;
                        for (; k < beams.Length - 1; k++)
                        {
                            beams[k] = beams[k + 1]; // shift the remaining beams to the left
                            if (Equals(beams[k], default(T)))
                            {
                                break; // only inactive beams follow
                            }
                        }
                        beams[beams.Length - 1] = default(T);
                        if (b == k) break;
                        b--;
                        continue;
                    }

                    beams[b] = candidates.Dequeue().State;

                    if (control.ShouldStop())
                    {
                        return;
                    }
                }

                candidates.Clear();
            }
        }

        /// <summary>
        /// Monotonic beam search uses several parallel beams which are iteratively updated.
        /// Each beam may only choose among branches that beams before it have made available.
        /// This behavior ensures that the call with beamWidth = n + 1 achieves as good results
        /// as with beamWidth = n. For standard beam search this property does not hold and a
        /// bigger beam search might lead to worse results (against the general trend of achieving
        /// better solutions).
        /// </summary>
        /// <remarks>
        /// The algorithms is described in Lemons, S., López, C.L., Holte, R.C. and Ruml, W., 2022.
        /// Beam Search: Faster and Monotonic. arXiv preprint arXiv:2204.02929.
        /// </remarks>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The state to start from</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes, default is to compare bounds</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void MonotonicBeamSearch<T, C, Q>(ISearchControl<T, Q> control, T state,
            int beamWidth, Func<T, float> rank, int filterWidth)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 1", nameof(filterWidth));
            if (rank == null) throw new ArgumentNullException(nameof(rank));
            if (filterWidth == 1 && beamWidth > 1) throw new ArgumentException($"{nameof(beamWidth)} cannot exceed 1 when {nameof(filterWidth)} equals 1.");

            var beams = new T[beamWidth];
            beams[0] = state;
            var candidates = new Priority_Queue.StablePriorityQueue<StateNode<T, C, Q>>(beamWidth * 10);
            while (!Equals(beams[0], default(T)) && !control.ShouldStop())
            {
                for (var b = 0; b < beams.Length; b++)
                {
                    if (Equals(beams[b], default(T)))
                    {
                        // launch a new beam (this does not violate monotonicity, because inactive beams will be shifted to the end of the beam array)
                        if (candidates.Count > 0)
                        {
                            beams[b] = candidates.Dequeue().State;
                            continue;
                        } else break; // no more active beams will follow
                    }

                    var currentState = beams[b];

                    foreach (var choice in currentState.GetChoices().Take(filterWidth))
                    {
                        var next = (T)currentState.Clone();
                        next.Apply(choice);
                        if (control.VisitNode(next) == VisitResult.Discard || next.IsTerminal)
                        {
                            continue;
                        }

                        if (candidates.Count == candidates.MaxSize)
                        {
                            candidates.Resize(candidates.MaxSize * 2);
                        }
                        candidates.Enqueue(new StateNode<T, C, Q>(next), rank(next));
                    }

                    if (candidates.Count == 0) // no more candidates, beam will become inactive and shifted to the end
                    {
                        var k = b;
                        for (; k < beams.Length - 1; k++)
                        {
                            beams[k] = beams[k + 1]; // shift the remaining beams to the left
                            if (Equals(beams[k], default(T)))
                            {
                                break; // only inactive beams follow
                            }
                        }
                        beams[beams.Length - 1] = default(T);
                        if (b == k) break;
                        b--;
                        continue;
                    }

                    beams[b] = candidates.Dequeue().State;

                    if (control.ShouldStop())
                    {
                        return;
                    }
                }

                candidates.Clear();
            }
        }
    }

    internal class StateNode<TState, TQuality> : Priority_Queue.StablePriorityQueueNode
        where TState : IState<TState, TQuality>
        where TQuality : struct, IQuality<TQuality>
    {
        private readonly TState state;
        public TState State => state;

        public StateNode(TState state)
        {
            this.state = state;
        }
    }

    internal class StateNode<TState, TChoice, TQuality> : Priority_Queue.StablePriorityQueueNode
        where TState : class, IMutableState<TState, TChoice, TQuality>
        where TQuality : struct, IQuality<TQuality>
    {
        private readonly TState state;
        public TState State => state;

        public StateNode(TState state)
        {
            this.state = state;
        }
    }

    public static class HeuristicExtensions
    {
        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes (lower is better)</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <param name="depthLimit">The maximum depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static Task<SearchControl<T, Q>> BeamSearchAsync<T, Q>(
            this SearchControl<T, Q> control, int beamWidth, Func<T, float> rank,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BeamSearch(control, beamWidth, rank, filterWidth, depthLimit));
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes (lower is better)</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <param name="depthLimit">The maximum depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static SearchControl<T, Q> BeamSearch<T, Q>(
            this SearchControl<T, Q> control, int beamWidth, Func<T, float> rank,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            Heuristics.BeamSearch(control, control.InitialState, beamWidth, rank, filterWidth, depthLimit);
            return control;
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes (lower is better)</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <param name="depthLimit">The maximum depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static Task<SearchControl<T, C, Q>> BeamSearchAsync<T, C, Q>(
            this SearchControl<T, C, Q> control, int beamWidth, Func<T, float> rank,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BeamSearch(control, beamWidth, rank, filterWidth, depthLimit));
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes (lower is better)</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <param name="depthLimit">The maximum depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static SearchControl<T, C, Q> BeamSearch<T, C, Q>(
            this SearchControl<T, C, Q> control, int beamWidth, Func<T, float> rank,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            Heuristics.BeamSearch<T, C, Q>(control, control.InitialState, 0, beamWidth, rank, filterWidth, depthLimit);
            return control;
        }

        /// <summary>
        /// Rake search performs a breadth-first search until a level is reached with <paramref name="rakeWidth"/>
        /// nodes and then from each node a lookahead with the given method is performed.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="rakeWidth">The number of nodes to reach, before proceeding with a simple greedy heuristic</param>
        /// <param name="lookahead">The lookahead method to use, <see cref="LA.DFSLookahead"/> with filterWidth = 1, will be used if null</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, Q>> RakeSearchAsync<T, Q>(
            this SearchControl<T, Q> control, int rakeWidth, Lookahead<T, Q> lookahead = null)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => RakeSearch(control, rakeWidth, lookahead));
        }

        /// <summary>
        /// Rake search performs a breadth-first search until a level is reached with <paramref name="rakeWidth"/>
        /// nodes and then from each node a lookahead with the given method is performed.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="rakeWidth">The number of nodes to reach, before proceeding with a simple greedy heuristic</param>
        /// <param name="lookahead">The lookahead method to use, <see cref="LA.DFSLookahead"/> with filterWidth = 1, will be used if null</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, Q> RakeSearch<T, Q>(
            this SearchControl<T, Q> control, int rakeWidth, Lookahead<T, Q> lookahead = null)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (rakeWidth <= 0) throw new ArgumentOutOfRangeException(nameof(rakeWidth), "rakeWidth must be greater than 0");
            if (lookahead == null) lookahead = LA.DFSLookahead<T, Q>(filterWidth: 1);
            Heuristics.RakeSearch(control, control.InitialState, rakeWidth, lookahead);
            return control;
        }

        /// <summary>
        /// Rake search performs a breadth-first search until a level is reached with <paramref name="rakeWidth"/>
        /// nodes and then from each node a lookahead with the given method is performed.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="rakeWidth">The number of nodes to reach, before proceeding with a simple greedy heuristic</param>
        /// <param name="lookahead">The lookahead method to use, <see cref="LA.DFSLookahead"/> with filterWidth = 1, will be used if null</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static Task<SearchControl<T, C, Q>> RakeSearchAsync<T, C, Q>(
            this SearchControl<T, C, Q> control, int rakeWidth, Lookahead<T, C, Q> lookahead = null)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => RakeSearch(control, rakeWidth, lookahead));
        }

        /// <summary>
        /// Rake search performs a breadth-first search until a level is reached with <paramref name="rakeWidth"/>
        /// nodes and then from each node a lookahead with the given method is performed.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="rakeWidth">The number of nodes to reach, before proceeding with a simple greedy heuristic</param>
        /// <param name="lookahead">The lookahead method to use, <see cref="LA.DFSLookahead"/> with filterWidth = 1, will be used if null</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        public static SearchControl<T, C, Q> RakeSearch<T, C, Q>(
            this SearchControl<T, C, Q> control, int rakeWidth, Lookahead<T, C, Q> lookahead = null)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (rakeWidth <= 0) throw new ArgumentOutOfRangeException(nameof(rakeWidth), "rakeWidth must be greater than 0");
            if (lookahead == null) lookahead = LA.DFSLookahead<T, C, Q>(filterWidth: 1);
            Heuristics.RakeSearch(control, (T)control.InitialState.Clone(), rakeWidth, lookahead);
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
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        [Obsolete("Use RakeSearchAsync with LA.BeamSearchLookahead")]
        public static Task<SearchControl<T, Q>> RakeAndBeamSearchAsync<T, Q>(
            this SearchControl<T, Q> control, int rakeWidth, int beamWidth,
            Func<T, float> rank, int filterWidth = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => RakeAndBeamSearch(control, rakeWidth, beamWidth, rank, filterWidth));
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
        /// <param name="depthLimit">To limit the depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        [Obsolete("Use RakeSearch with LA.BeamSearchLookahead")]
        public static SearchControl<T, Q> RakeAndBeamSearch<T, Q>(
            this SearchControl<T, Q> control, int rakeWidth, int beamWidth,
            Func<T, float> rank, int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            var (_, rake) = Algorithms.BreadthSearch(control, control.InitialState, depth: 0, filterWidth: int.MaxValue, depthLimit: int.MaxValue, rakeWidth);
            var i = 0;
            while (i < rakeWidth && rake.TryGetNext(out var next) && !control.ShouldStop())
            {
                Heuristics.BeamSearch(control, next, beamWidth, rank, filterWidth, depthLimit);
                i++;
            }
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
        /// <param name="depthLimit">To limit the depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        [Obsolete("Use RakeSearchAsync with LA.BeamSearchLookahead")]
        public static Task<SearchControl<T, C, Q>> RakeAndBeamSearchAsync<T, C, Q>(
            this SearchControl<T, C, Q> control, int rakeWidth, int beamWidth,
            Func<T, float> rank, int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => RakeAndBeamSearch(control, rakeWidth, beamWidth, rank, filterWidth, depthLimit));
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
        /// <param name="depthLimit">To limit the depth of the search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The runtime control instance</returns>
        [Obsolete("Use RakeSearch with LA.BeamSearchLookahead")]
        public static SearchControl<T, C, Q> RakeAndBeamSearch<T, C, Q>(
            this SearchControl<T, C, Q> control, int rakeWidth, int beamWidth,
            Func<T, float> rank, int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var (_, rake) = Algorithms.BreadthSearch<T, C, Q>(control, control.InitialState, depth: 0, filterWidth: int.MaxValue, depthLimit: int.MaxValue, rakeWidth);
            var i = 0;
            while (i < rakeWidth && rake.TryGetNext(out var next) && !control.ShouldStop())
            {
                Heuristics.BeamSearch<T, C, Q>(control, next, 0, beamWidth, rank, filterWidth, depthLimit);
                i++;
            }
            return control;
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, the lookahead can be configured. During lookahead, at least a state with
        /// a quality must be achieved to identify the better branch.
        /// </summary>
        /// <remarks>
        /// Voßs, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="lookahead">The lookahead method to use, <see cref="LA.DFSLookahead"/> with filterWidth = 1, will be used if null</param>
        /// <param name="filterWidth">How many descendents will be considered for lookahead</param>
        /// <param name="depthLimit">The maximum depth for the pilot search.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static Task<SearchControl<T, Q>> PilotMethodAsync<T, Q>(
                this SearchControl<T, Q> control, Lookahead<T, Q> lookahead = null,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => PilotMethod(control, lookahead, filterWidth, depthLimit));
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, the lookahead can be configured. During lookahead, at least a state with
        /// a quality must be achieved to identify the better branch.
        /// </summary>
        /// <remarks>
        /// Voßs, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="lookahead">The lookahead method to use, <see cref="LA.DFSLookahead"/> with filterWidth = 1, will be used if null</param>
        /// <param name="filterWidth">How many descendents will be considered for lookahead</param>
        /// <param name="depthLimit">The maximum depth for the pilot search.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static SearchControl<T, Q> PilotMethod<T, Q>(
                this SearchControl<T, Q> control, Lookahead<T, Q> lookahead = null,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (lookahead == null) lookahead = LA.DFSLookahead<T, Q>(filterWidth: 1);
            Heuristics.PilotMethod<T, Q>(control, control.InitialState, depth: 0, lookahead, filterWidth, depthLimit);
            return control;
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, the lookahead can be configured. During lookahead, at least a state with
        /// a quality must be achieved to identify the better branch.
        /// </summary>
        /// <remarks>
        /// Voßs, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="lookahead">The lookahead method to use, <see cref="LA.DFSLookahead"/> with filterWidth = 1, will be used if null</param>
        /// <param name="filterWidth">How many descendents will be considered for lookahead</param>
        /// <param name="depthLimit">The maximum depth of the pilot search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static Task<SearchControl<T, C, Q>> PilotMethodAsync<T, C, Q>(
                this SearchControl<T, C, Q> control, Lookahead<T, C, Q> lookahead = null,
                int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => PilotMethod(control, lookahead, filterWidth, depthLimit));
        }

        /// <summary>
        /// In the PILOT method a lookahead is performed to determine the most promising branch to continue.
        /// In this implementation, the lookahead can be configured. During lookahead, at least a state with
        /// a quality must be achieved to identify the better branch.
        /// </summary>
        /// <remarks>
        /// Voßs, S., Fink, A. & Duin, C. Looking Ahead with the Pilot Method. Ann Oper Res 136, 285–302 (2005).
        /// https://doi.org/10.1007/s10479-005-2060-2
        /// </remarks>
        /// <param name="control">Runtime control and best solution tracking.</param>
        /// <param name="lookahead">The lookahead method to use, <see cref="LA.DFSLookahead"/> with filterWidth = 1, will be used if null</param>
        /// <param name="filterWidth">How many descendents will be considered for lookahead</param>
        /// <param name="depthLimit">The maximum depth of the pilot search</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static SearchControl<T, C, Q> PilotMethod<T, C, Q>(
            this SearchControl<T, C, Q> control, Lookahead<T, C, Q> lookahead = null,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (lookahead == null) lookahead = LA.DFSLookahead<T, C, Q>(filterWidth: 1);
            var state = (T)control.InitialState.Clone();
            Heuristics.PilotMethod<T, C, Q>(control, state, depth: 0, lookahead, filterWidth, depthLimit);
            return control;
        }

        /// <summary>
        /// The limited discrepancy (LD) search assumes branches are sorted according to a heuristic and generally taking the
        /// first branch leads to better outcomes. It assumes that there is a discrepancy, i.e., penalty, of N for visiting the
        /// (N+1)th branch in a certain node. This penalty accumulates along the search path and the parameter
        /// <paramref name="maxDiscrepancy"/> limits this accumulated penalty. The search thus visits only part of a tree.
        /// 
        /// For instance, a maxDiscrepancy of 2 allows any leaf to be reachable where always the first branch has been picked,
        /// where at most 2 times the second branch has been picked or where at most once the third branch was picked. While this
        /// reduces the size of the search tree, it still grows quickly with increasing values of maxDiscrepancy.
        /// 
        /// The naive implementation will not progress to visit nodes with lower discrepancy before visiting those with higher
        /// discrepancies. For instance, a node with discrepancy 2 might be visited before a node with discrepancy 1. Consider
        /// the "anytime" implementation of LD search if you want to interrupt the search, before it is completed and want to
        /// ensure that all nodes with lower discrepancies are considered before those with higher ones.
        /// </summary>
        /// <remarks>
        /// Limited discrepancy search has been described by
        /// Harvey, W.D. and Ginsberg, M.L., 1995, August. Limited discrepancy search. In IJCAI (1) (pp. 607-615).
        /// 
        /// The implementation here differs, because we don't assume a fixed number of branches per node, nor that the
        /// depth is fixed and known for each branch a priori.
        /// </remarks>
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The instance that performs the runtim control and tracking</returns>
        public static Task<SearchControl<T, Q>> NaiveLDSearchAsync<T, Q>(
            this SearchControl<T, Q> control, int maxDiscrepancy)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => NaiveLDSearch(control, maxDiscrepancy));
        }


        /// <summary>
        /// The limited discrepancy (LD) search assumes branches are sorted according to a heuristic and generally taking the
        /// first branch leads to better outcomes. It assumes that there is a discrepancy, i.e., penalty, of N for visiting the
        /// (N+1)th branch in a certain node. This penalty accumulates along the search path and the parameter
        /// <paramref name="maxDiscrepancy"/> limits this accumulated penalty. The search thus visits only part of a tree.
        /// 
        /// For instance, a maxDiscrepancy of 2 allows any leaf to be reachable where always the first branch has been picked,
        /// where at most 2 times the second branch has been picked or where at most once the third branch was picked. While this
        /// reduces the size of the search tree, it still grows quickly with increasing values of maxDiscrepancy.
        /// 
        /// The naive implementation will not progress to visit nodes with lower discrepancy before visiting those with higher
        /// discrepancies. For instance, a node with discrepancy 2 might be visited before a node with discrepancy 1. Consider
        /// the "anytime" implementation of LD search if you want to interrupt the search, before it is completed and want to
        /// ensure that all nodes with lower discrepancies are considered before those with higher ones.
        /// </summary>
        /// <remarks>
        /// Limited discrepancy search has been described by
        /// Harvey, W.D. and Ginsberg, M.L., 1995, August. Limited discrepancy search. In IJCAI (1) (pp. 607-615).
        /// 
        /// The implementation here differs, because we don't assume a fixed number of branches per node, nor that the
        /// depth is fixed and known for each branch a priori.
        /// </remarks>
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The instance that performs the runtim control and tracking</returns>
        public static SearchControl<T, Q> NaiveLDSearch<T, Q>(
            this SearchControl<T, Q> control, int maxDiscrepancy)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            Heuristics.NaiveLDSearch(control, state, maxDiscrepancy);
            return control;
        }

        /// <summary>
        /// The limited discrepancy (LD) search assumes branches are sorted according to a heuristic and generally taking the
        /// first branch leads to better outcomes. It assumes that there is a discrepancy, i.e., penalty, of N for visiting the
        /// (N+1)th branch in a certain node. This penalty accumulates along the search path and the parameter
        /// <paramref name="maxDiscrepancy"/> limits this accumulated penalty. The search thus visits only part of a tree.
        /// 
        /// For instance, a maxDiscrepancy of 2 allows any leaf to be reachable where always the first branch has been picked,
        /// where at most 2 times the second branch has been picked or where at most once the third branch was picked. While this
        /// reduces the size of the search tree, it still grows quickly with increasing values of maxDiscrepancy.
        /// 
        /// The naive implementation will not progress to visit nodes with lower discrepancy before visiting those with higher
        /// discrepancies. For instance, a node with discrepancy 2 might be visited before a node with discrepancy 1. Consider
        /// the "anytime" implementation of LD search if you want to interrupt the search, before it is completed and want to
        /// ensure that all nodes with lower discrepancies are considered before those with higher ones.
        /// </summary>
        /// <remarks>
        /// Limited discrepancy search has been described by
        /// Harvey, W.D. and Ginsberg, M.L., 1995, August. Limited discrepancy search. In IJCAI (1) (pp. 607-615).
        /// 
        /// The implementation here differs, because we don't assume a fixed number of branches per node, nor that the
        /// depth is fixed and known for each branch a priori.
        /// </remarks>
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The instance that performs the runtim control and tracking</returns>
        public static Task<SearchControl<T, C, Q>> NaiveLDSearchAsync<T, C, Q>(
            this SearchControl<T, C, Q> control, int maxDiscrepancy)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => NaiveLDSearch(control, maxDiscrepancy));
        }

        /// <summary>
        /// The limited discrepancy (LD) search assumes branches are sorted according to a heuristic and generally taking the
        /// first branch leads to better outcomes. It assumes that there is a discrepancy, i.e., penalty, of N for visiting the
        /// (N+1)th branch in a certain node. This penalty accumulates along the search path and the parameter
        /// <paramref name="maxDiscrepancy"/> limits this accumulated penalty. The search thus visits only part of a tree.
        /// 
        /// For instance, a maxDiscrepancy of 2 allows any leaf to be reachable where always the first branch has been picked,
        /// where at most 2 times the second branch has been picked or where at most once the third branch was picked. While this
        /// reduces the size of the search tree, it still grows quickly with increasing values of maxDiscrepancy.
        /// 
        /// The naive implementation will not progress to visit nodes with lower discrepancy before visiting those with higher
        /// discrepancies. For instance, a node with discrepancy 2 might be visited before a node with discrepancy 1. Consider
        /// the "anytime" implementation of LD search if you want to interrupt the search, before it is completed and want to
        /// ensure that all nodes with lower discrepancies are considered before those with higher ones.
        /// </summary>
        /// <remarks>
        /// Limited discrepancy search has been described by
        /// Harvey, W.D. and Ginsberg, M.L., 1995, August. Limited discrepancy search. In IJCAI (1) (pp. 607-615).
        /// 
        /// The implementation here differs, because we don't assume a fixed number of branches per node, nor that the
        /// depth is fixed and known for each branch a priori.
        /// </remarks>
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The instance that performs the runtim control and tracking</returns>
        public static SearchControl<T, C, Q> NaiveLDSearch<T, C, Q>(
            this SearchControl<T, C, Q> control, int maxDiscrepancy)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            Heuristics.NaiveLDSearch<T, C, Q>(control, state, maxDiscrepancy);
            return control;
        }

        /// <summary>
        /// The limited discrepancy (LD) search assumes branches are sorted according to a heuristic and generally taking the
        /// first branch leads to better outcomes. It assumes that there is a discrepancy, i.e., penalty, of N for visiting the
        /// (N+1)th branch in a certain node. This penalty accumulates along the search path and the parameter
        /// <paramref name="maxDiscrepancy"/> limits this accumulated penalty. The search thus visits only part of a tree.
        /// 
        /// For instance, a maxDiscrepancy of 2 allows any leaf to be reachable where always the first branch has been picked,
        /// where at most 2 times the second branch has been picked or where at most once the third branch was picked. While this
        /// reduces the size of the search tree, it still grows quickly with increasing values of maxDiscrepancy.
        /// 
        /// The anytime implementation first processes all nodes with discrepancy K, before moving to nodes with discrepancy K+1.
        /// If you intend to always completely visit all reachable nodes, the naive implementation can be chosen instead. If the
        /// intent is to limit the runtime, then this implementation should be chosen.
        /// </summary>
        /// <remarks>
        /// Limited discrepancy search has been described by
        /// Harvey, W.D. and Ginsberg, M.L., 1995, August. Limited discrepancy search. In IJCAI (1) (pp. 607-615).
        /// 
        /// The implementation here differs, because we don't assume a fixed number of branches per node, nor that the
        /// depth is fixed and known for each branch a priori.
        /// </remarks>
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static Task<SearchControl<T, Q>> AnytimeLDSearchAsync<T, Q>(
            this SearchControl<T, Q> control, int maxDiscrepancy)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => AnytimeLDSearch(control, maxDiscrepancy));
        }

        /// <summary>
        /// The limited discrepancy (LD) search assumes branches are sorted according to a heuristic and generally taking the
        /// first branch leads to better outcomes. It assumes that there is a discrepancy, i.e., penalty, of N for visiting the
        /// (N+1)th branch in a certain node. This penalty accumulates along the search path and the parameter
        /// <paramref name="maxDiscrepancy"/> limits this accumulated penalty. The search thus visits only part of a tree.
        /// 
        /// For instance, a maxDiscrepancy of 2 allows any leaf to be reachable where always the first branch has been picked,
        /// where at most 2 times the second branch has been picked or where at most once the third branch was picked. While this
        /// reduces the size of the search tree, it still grows quickly with increasing values of maxDiscrepancy.
        /// 
        /// The anytime implementation first processes all nodes with discrepancy K, before moving to nodes with discrepancy K+1.
        /// If you intend to always completely visit all reachable nodes, the naive implementation can be chosen instead. If the
        /// intent is to limit the runtime, then this implementation should be chosen.
        /// </summary>
        /// <remarks>
        /// Limited discrepancy search has been described by
        /// Harvey, W.D. and Ginsberg, M.L., 1995, August. Limited discrepancy search. In IJCAI (1) (pp. 607-615).
        /// 
        /// The implementation here differs, because we don't assume a fixed number of branches per node, nor that the
        /// depth is fixed and known for each branch a priori.
        /// </remarks>
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static SearchControl<T, Q> AnytimeLDSearch<T, Q>(
            this SearchControl<T, Q> control, int maxDiscrepancy)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            Heuristics.AnytimeLDSearch(control, state, maxDiscrepancy);
            return control;
        }

        /// <summary>
        /// The limited discrepancy (LD) search assumes branches are sorted according to a heuristic and generally taking the
        /// first branch leads to better outcomes. It assumes that there is a discrepancy, i.e., penalty, of N for visiting the
        /// (N+1)th branch in a certain node. This penalty accumulates along the search path and the parameter
        /// <paramref name="maxDiscrepancy"/> limits this accumulated penalty. The search thus visits only part of a tree.
        /// 
        /// For instance, a maxDiscrepancy of 2 allows any leaf to be reachable where always the first branch has been picked,
        /// where at most 2 times the second branch has been picked or where at most once the third branch was picked. While this
        /// reduces the size of the search tree, it still grows quickly with increasing values of maxDiscrepancy.
        /// 
        /// The anytime implementation first processes all nodes with discrepancy K, before moving to nodes with discrepancy K+1.
        /// If you intend to always completely visit all reachable nodes, the naive implementation can be chosen instead. If the
        /// intent is to limit the runtime, then this implementation should be chosen.
        /// There is a performance penalty of the anytime version, as the search cannot rely on undoing moves and thus has to do
        /// some cloning of states.
        /// </summary>
        /// <remarks>
        /// Limited discrepancy search has been described by
        /// Harvey, W.D. and Ginsberg, M.L., 1995, August. Limited discrepancy search. In IJCAI (1) (pp. 607-615).
        /// 
        /// The implementation here differs, because we don't assume a fixed number of branches per node, nor that the
        /// depth is fixed and known for each branch a priori.
        /// </remarks>
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static Task<SearchControl<T, C, Q>> AnytimeLDSearchAsync<T, C, Q>(
            this SearchControl<T, C, Q> control, int maxDiscrepancy)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => AnytimeLDSearch(control, maxDiscrepancy));
        }

        /// <summary>
        /// The limited discrepancy (LD) search assumes branches are sorted according to a heuristic and generally taking the
        /// first branch leads to better outcomes. It assumes that there is a discrepancy, i.e., penalty, of N for visiting the
        /// (N+1)th branch in a certain node. This penalty accumulates along the search path and the parameter
        /// <paramref name="maxDiscrepancy"/> limits this accumulated penalty. The search thus visits only part of a tree.
        /// 
        /// For instance, a maxDiscrepancy of 2 allows any leaf to be reachable where always the first branch has been picked,
        /// where at most 2 times the second branch has been picked or where at most once the third branch was picked. While this
        /// reduces the size of the search tree, it still grows quickly with increasing values of maxDiscrepancy.
        /// 
        /// The anytime implementation first processes all nodes with discrepancy K, before moving to nodes with discrepancy K+1.
        /// If you intend to always completely visit all reachable nodes, the naive implementation can be chosen instead. If the
        /// intent is to limit the runtime, then this implementation should be chosen.
        /// There is a performance penalty of the anytime version, as the search cannot rely on undoing moves and thus has to do
        /// some cloning of states.
        /// </summary>
        /// <remarks>
        /// Limited discrepancy search has been described by
        /// Harvey, W.D. and Ginsberg, M.L., 1995, August. Limited discrepancy search. In IJCAI (1) (pp. 607-615).
        /// 
        /// The implementation here differs, because we don't assume a fixed number of branches per node, nor that the
        /// depth is fixed and known for each branch a priori.
        /// </remarks>
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static SearchControl<T, C, Q> AnytimeLDSearch<T, C, Q>(
            this SearchControl<T, C, Q> control, int maxDiscrepancy)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            Heuristics.AnytimeLDSearch<T, C, Q>(control, state, maxDiscrepancy);
            return control;
        }

        /// <summary>
        /// Monotonic beam search uses several parallel beams which are iteratively updated.
        /// Each beam may only choose among branches that beams before it have made available.
        /// This behavior ensures that the call with beamWidth = n + 1 achieves as good results
        /// as with beamWidth = n. For standard beam search this property does not hold and a
        /// bigger beam search might lead to worse results (against the general trend of achieving
        /// better solutions).
        /// </summary>
        /// <remarks>
        /// The algorithms is described in Lemons, S., López, C.L., Holte, R.C. and Ruml, W., 2022.
        /// Beam Search: Faster and Monotonic. arXiv preprint arXiv:2204.02929.
        /// </remarks>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes, default is to compare bounds</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static Task<SearchControl<T, Q>> MonotonicBeamSearchAsync<T, Q>(
            this SearchControl<T, Q> control, int beamWidth, Func<T, float> rank,
            int filterWidth = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => MonotonicBeamSearch(control, beamWidth, rank, filterWidth));
        }

        /// <summary>
        /// Monotonic beam search uses several parallel beams which are iteratively updated.
        /// Each beam may only choose among branches that beams before it have made available.
        /// This behavior ensures that the call with beamWidth = n + 1 achieves as good results
        /// as with beamWidth = n. For standard beam search this property does not hold and a
        /// bigger beam search might lead to worse results (against the general trend of achieving
        /// better solutions).
        /// </summary>
        /// <remarks>
        /// The algorithms is described in Lemons, S., López, C.L., Holte, R.C. and Ruml, W., 2022.
        /// Beam Search: Faster and Monotonic. arXiv preprint arXiv:2204.02929.
        /// </remarks>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes, default is to compare bounds</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static SearchControl<T, Q> MonotonicBeamSearch<T, Q>(
            this SearchControl<T, Q> control, int beamWidth, Func<T, float> rank,
            int filterWidth = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            Heuristics.MonotonicBeamSearch(control, control.InitialState, beamWidth, rank, filterWidth);
            return control;
        }

        /// <summary>
        /// Monotonic beam search uses several parallel beams which are iteratively updated.
        /// Each beam may only choose among branches that beams before it have made available.
        /// This behavior ensures that the call with beamWidth = n + 1 achieves as good results
        /// as with beamWidth = n. For standard beam search this property does not hold and a
        /// bigger beam search might lead to worse results (against the general trend of achieving
        /// better solutions).
        /// </summary>
        /// <remarks>
        /// The algorithms is described in Lemons, S., López, C.L., Holte, R.C. and Ruml, W., 2022.
        /// Beam Search: Faster and Monotonic. arXiv preprint arXiv:2204.02929.
        /// </remarks>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes, default is to compare bounds</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static Task<SearchControl<T, C, Q>> MonotonicBeamSearchAsync<T, C, Q>(
            this SearchControl<T, C, Q> control, int beamWidth, Func<T, float> rank,
            int filterWidth = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => MonotonicBeamSearch(control, beamWidth, rank, filterWidth));
        }

        /// <summary>
        /// Monotonic beam search uses several parallel beams which are iteratively updated.
        /// Each beam may only choose among branches that beams before it have made available.
        /// This behavior ensures that the call with beamWidth = n + 1 achieves as good results
        /// as with beamWidth = n. For standard beam search this property does not hold and a
        /// bigger beam search might lead to worse results (against the general trend of achieving
        /// better solutions).
        /// </summary>
        /// <remarks>
        /// The algorithms is described in Lemons, S., López, C.L., Holte, R.C. and Ruml, W., 2022.
        /// Beam Search: Faster and Monotonic. arXiv preprint arXiv:2204.02929.
        /// </remarks>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes, default is to compare bounds</param>
        /// <param name="filterWidth">The maximum number of descendents per node</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static SearchControl<T, C, Q> MonotonicBeamSearch<T, C, Q>(
            this SearchControl<T, C, Q> control, int beamWidth, Func<T, float> rank,
            int filterWidth = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            Heuristics.MonotonicBeamSearch<T, C, Q>(control, control.InitialState, beamWidth, rank, filterWidth);
            return control;
        }

        public static Task<TState> BeamSearchAsync<TState, TQuality>(
            this IState<TState, TQuality> state, int beamWidth, Func<TState, float> rank,
            int filterWidth = int.MaxValue, TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => BeamSearch((TState)state, beamWidth, rank, filterWidth,
                runtime, nodelimit, callback, token)
            );
        }
        public static TState BeamSearch<TState, TQuality>(
            this IState<TState, TQuality> state, int beamWidth, Func<TState, float> rank,
            int filterWidth = int.MaxValue, TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.BeamSearch(beamWidth, rank, filterWidth).BestQualityState;
        }

        public static Task<TState> BeamSearchAsync<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int beamWidth,
            Func<TState, float> rank, int filterWidth = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => BeamSearch<TState, TChoice, TQuality>(
                (TState)state, beamWidth, rank, filterWidth, runtime, nodelimit,
                callback, token)
            );
        }

        public static TState BeamSearch<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int beamWidth,
            Func<TState, float> rank, int filterWidth = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.BeamSearch(beamWidth, rank, filterWidth).BestQualityState;
        }

        public static Task<TState> RakeSearchAsync<TState, TQuality>(
            this IState<TState, TQuality> state, int rakeWidth,
            Lookahead<TState, TQuality> lookahead = null,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => RakeSearch((TState)state, rakeWidth, lookahead, runtime,
                nodelimit, callback, token)
            );
        }

        public static TState RakeSearch<TState, TQuality>(
            this IState<TState, TQuality> state, int rakeWidth,
            Lookahead<TState, TQuality> lookahead = null,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.RakeSearch(rakeWidth, lookahead).BestQualityState;
        }

        public static Task<TState> RakeSearchAsync<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int rakeWidth,
            Lookahead<TState, TChoice, TQuality> lookahead = null,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => RakeSearch<TState, TChoice, TQuality>(
                (TState)state, rakeWidth, lookahead, runtime, nodelimit, callback, token)
            );
        }

        public static TState RakeSearch<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int rakeWidth,
            Lookahead<TState, TChoice, TQuality> lookahead = null,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.RakeSearch(rakeWidth, lookahead).BestQualityState;
        }

        [Obsolete("Use RakeSearchAsync with BeamSearchLookahead instead.")]
        public static Task<TState> RakeAndBeamSearchAsync<TState, TQuality>(
            this IState<TState, TQuality> state, int rakeWidth, int beamWidth, 
            Func<TState, float> rank, int filterWidth = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => RakeAndBeamSearch((TState)state, rakeWidth,
                beamWidth, rank, filterWidth, runtime, nodelimit, callback, token)
            );
        }

        [Obsolete("Use RakeSearch with BeamSearchLookahead instead.")]
        public static TState RakeAndBeamSearch<TState, TQuality>(
            this IState<TState, TQuality> state, int rakeWidth, int beamWidth,
            Func<TState, float> rank, int filterWidth = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.RakeAndBeamSearch(rakeWidth, beamWidth, rank,
                filterWidth).BestQualityState;
        }

        [Obsolete("Use RakeSearchAsync with BeamSearchLookahead instead.")]
        public static Task<TState> RakeAndBeamSearchAsync<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int rakeWidth, int beamWidth,
            Func<TState, float> rank, int filterWidth = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => RakeAndBeamSearch<TState, TChoice, TQuality>(
                (TState)state, rakeWidth, beamWidth, rank, filterWidth, runtime,
                nodelimit, callback, token)
            );
        }

        [Obsolete("Use RakeSearch with BeamSearchLookahead instead.")]
        public static TState RakeAndBeamSearch<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int rakeWidth, int beamWidth,
            Func<TState, float> rank, int filterWidth = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.RakeAndBeamSearch(rakeWidth, beamWidth,
                rank, filterWidth).BestQualityState;
        }

        public static Task<TState> PilotMethodAsync<TState, TQuality>(
            this IState<TState, TQuality> state, Lookahead<TState, TQuality> lookahead = null,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => PilotMethod((TState)state, lookahead,
                filterWidth, depthLimit, runtime, nodelimit, callback, token)
            );
        }

        public static TState PilotMethod<TState, TQuality>(
            this IState<TState, TQuality> state, Lookahead<TState, TQuality> lookahead = null,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (lookahead == null) lookahead = LA.DFSLookahead<TState, TQuality>(filterWidth: 1);
            return control.PilotMethod(lookahead, filterWidth, depthLimit).BestQualityState;
        }

        public static Task<TState> PilotMethodAsync<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, Lookahead<TState, TChoice, TQuality> lookahead = null,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => PilotMethod<TState, TChoice, TQuality>(
                (TState)state, lookahead, filterWidth, depthLimit, runtime,
                nodelimit, callback, token)
            );
        }

        public static TState PilotMethod<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, Lookahead<TState, TChoice, TQuality> lookahead = null,
            int filterWidth = int.MaxValue, int depthLimit = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            if (lookahead == null) lookahead = LA.DFSLookahead<TState, TChoice, TQuality>(filterWidth: 1);
            return control.PilotMethod(lookahead, filterWidth, depthLimit).BestQualityState;
        }

        public static Task<TState> NaiveLDSearchAsync<TState, TQuality>(
            this IState<TState, TQuality> state, int maxDiscrepancy,
            int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => NaiveLDSearch(state, maxDiscrepancy, seed, runtime,
                nodelimit, callback, token)
            );
        }

        public static TState NaiveLDSearch<TState, TQuality>(
            this IState<TState, TQuality> state, int maxDiscrepancy,
            int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.NaiveLDSearch(maxDiscrepancy).BestQualityState;
        }

        public static Task<TState> NaiveLDSearchAsync<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int maxDiscrepancy,
            int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => NaiveLDSearch(state, maxDiscrepancy, seed,
                runtime, nodelimit, callback, token)
            );
        }

        public static TState NaiveLDSearch<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int maxDiscrepancy,
            int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.NaiveLDSearch(maxDiscrepancy).BestQualityState;
        }

        public static Task<TState> AnytimeLDSearchAsync<TState, TQuality>(
            this IState<TState, TQuality> state, int maxDiscrepancy,
            int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => AnytimeLDSearch(state, maxDiscrepancy, seed,
                runtime, nodelimit, callback, token)
            );
        }

        public static TState AnytimeLDSearch<TState, TQuality>(
            this IState<TState, TQuality> state, int maxDiscrepancy,
            int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.AnytimeLDSearch(maxDiscrepancy).BestQualityState;
        }

        public static Task<TState> AnytimeLDSearchAsync<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int maxDiscrepancy,
            int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => AnytimeLDSearch(state, maxDiscrepancy, seed,
                runtime, nodelimit, callback, token)
            );
        }

        public static TState AnytimeLDSearch<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int maxDiscrepancy,
            int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.NaiveLDSearch(maxDiscrepancy).BestQualityState;
        }

        public static Task<TState> MonotonicBeamSearchAsync<TState, TQuality>(
            this IState<TState, TQuality> state, int beamWidth,
            Func<TState, float> rank, int filterWidth = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => MonotonicBeamSearch((TState)state, beamWidth, rank,
                filterWidth, runtime, nodelimit, callback, token)
            );
        }
        public static TState MonotonicBeamSearch<TState, TQuality>(
            this IState<TState, TQuality> state, int beamWidth,
            Func<TState, float> rank, int filterWidth = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state)
                .WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.MonotonicBeamSearch(beamWidth, rank, filterWidth).BestQualityState;
        }

        public static Task<TState> MonotonicBeamSearchAsync<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int beamWidth,
            Func<TState, float> rank, int filterWidth = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => MonotonicBeamSearch<TState, TChoice, TQuality>(
                (TState)state, beamWidth, rank, filterWidth, runtime, nodelimit,
                callback, token)
            );
        }

        public static TState MonotonicBeamSearch<TState, TChoice, TQuality>(
            this IMutableState<TState, TChoice, TQuality> state, int beamWidth,
            Func<TState, float> rank, int filterWidth = int.MaxValue,
            TimeSpan? runtime = null, long? nodelimit = null,
            QualityCallback<TState, TQuality> callback = null,
            CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.MonotonicBeamSearch(beamWidth, rank, filterWidth).BestQualityState;
        }
    }
}