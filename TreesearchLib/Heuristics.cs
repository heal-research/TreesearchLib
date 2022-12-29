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
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes, default is to compare bounds</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static Task<SearchControl<T, Q>> BeamSearchAsync<T, Q>(this SearchControl<T, Q> control, int beamWidth, IComparer<T> rank = null)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BeamSearch(control, beamWidth, rank));
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes, default is to compare bounds</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static SearchControl<T, Q> BeamSearch<T, Q>(this SearchControl<T, Q> control, int beamWidth, IComparer<T> rank = null)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (rank == null) rank = new BoundComparer<T, Q>();
            DoBeamSearch(control, control.InitialState, beamWidth, rank);
            return control;
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes, default is to compare bounds</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static Task<SearchControl<T, C, Q>> BeamSearchAsync<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth, IComparer<T> rank = null)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BeamSearch(control, beamWidth, rank));
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes, default is to compare bounds</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static SearchControl<T, C, Q> BeamSearch<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth, IComparer<T> rank = null)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (rank == null) rank = new BoundComparer<T, C, Q>();
            DoBeamSearch<T, C, Q>(control, control.InitialState, beamWidth, rank);
            return control;
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="state">The state to start the search from</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void DoBeamSearch<T,Q>(ISearchControl<T, Q> control, T state, int beamWidth, IComparer<T> rank)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (rank == null) throw new ArgumentNullException(nameof(rank));
            var currentLayer = new Queue<T>();
            currentLayer.Enqueue(state);
            var nextlayer = new List<T>();
            while (!control.ShouldStop())
            {
                nextlayer.Clear();

                while (currentLayer.Count > 0)
                {
                    var currentState = currentLayer.Dequeue();
                    foreach (var next in currentState.GetBranches())
                    {
                        var prune = !next.Bound.IsBetter(control.BestQuality); // this check _MUST be done BEFORE_ VisitNode, which may update BestQuality
                        control.VisitNode(next);

                        if (prune)
                        {
                            continue;
                        }

                        nextlayer.Add(next);
                    }

                    if (control.ShouldStop())
                    {
                        nextlayer.Clear();
                        break;
                    }
                }

                if (nextlayer.Count == 0)
                {
                    break;
                }

                foreach (var nextState in nextlayer.OrderBy(x => x, rank).Take(beamWidth))
                {
                    currentLayer.Enqueue(nextState);
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
        /// <param name="rank">The rank function that determines the order of nodes</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void DoBeamSearch<T, C, Q>(ISearchControl<T, Q> control, T state, int beamWidth, IComparer<T> rank)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (rank == null) throw new ArgumentNullException(nameof(rank));
            var currentLayer = new Queue<T>();
            currentLayer.Enqueue(state);
            var nextlayer = new List<T>();
            while (!control.ShouldStop())
            {
                nextlayer.Clear();

                while (currentLayer.Count > 0)
                {
                    var currentState = currentLayer.Dequeue();
                    foreach (var choice in currentState.GetChoices())
                    {
                        var next = (T)currentState.Clone();
                        next.Apply(choice);
                        var prune = !next.Bound.IsBetter(control.BestQuality); // this check _MUST be done BEFORE_ VisitNode, which may update BestQuality
                        control.VisitNode(next);

                        if (prune)
                        {
                            continue;
                        }

                        nextlayer.Add(next);
                    }

                    if (control.ShouldStop())
                    {
                        nextlayer.Clear();
                        break;
                    }
                }

                if (nextlayer.Count == 0)
                {
                    break;
                }

                foreach (var nextState in nextlayer.OrderBy(x => x, rank).Take(beamWidth))
                {
                    currentLayer.Enqueue(nextState);
                }
            }
        }

        public static Task<SearchControl<T, Q>> RakeSearchAsync<T, Q>(this SearchControl<T, Q> control, int rakeWidth)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => RakeSearch(control, rakeWidth));
        }

        public static SearchControl<T, Q> RakeSearch<T, Q>(this SearchControl<T, Q> control, int rakeWidth)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            var rake = Algorithms.DoSearch(control, control.InitialState, false, int.MaxValue, int.MaxValue, rakeWidth);
            while (rake.TryGetNext(out var next) && !control.ShouldStop())
            {
                Algorithms.DoSearch(control, next.Item2, true, 1, int.MaxValue, int.MaxValue);
            }
            return control;
        }

        public static Task<SearchControl<T, C, Q>> RakeSearchAsync<T, C, Q>(this SearchControl<T, C, Q> control, int rakeWidth)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => RakeSearch(control, rakeWidth));
        }

        public static SearchControl<T, C, Q> RakeSearch<T, C, Q>(this SearchControl<T, C, Q> control, int rakeWidth)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var rake = Algorithms.DoBreadthSearch<T, C, Q>(control, control.InitialState, int.MaxValue, int.MaxValue, rakeWidth);
            while (rake.TryGetNext(out var next) && !control.ShouldStop())
            {
                Algorithms.DoDepthSearch<T, C, Q>(control, next.Item2, beamWidth: 1);
            }
            return control;
        }

        public static Task<SearchControl<T, Q>> RakeAndBeamSearchAsync<T, Q>(this SearchControl<T, Q> control, int rakeWidth, int beamWidth, IComparer<T> rank = null)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => RakeAndBeamSearch(control, rakeWidth, beamWidth, rank));
        }

        public static SearchControl<T, Q> RakeAndBeamSearch<T, Q>(this SearchControl<T, Q> control, int rakeWidth, int beamWidth, IComparer<T> rank = null)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (rank == null) rank = new BoundComparer<T, Q>();
            var rake = Algorithms.DoSearch(control, control.InitialState, false, int.MaxValue, int.MaxValue, rakeWidth);
            while (rake.TryGetNext(out var next) && !control.ShouldStop())
            {
                DoBeamSearch(control, next.Item2, beamWidth, rank);
            }
            return control;
        }

        public static Task<SearchControl<T, C, Q>> RakeAndBeamSearchAsync<T, C, Q>(this SearchControl<T, C, Q> control, int rakeWidth, int beamWidth, IComparer<T> rank = null)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => RakeAndBeamSearch(control, rakeWidth, beamWidth, rank));
        }

        public static SearchControl<T, C, Q> RakeAndBeamSearch<T, C, Q>(this SearchControl<T, C, Q> control, int rakeWidth, int beamWidth, IComparer<T> rank = null)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (rank == null) rank = new BoundComparer<T, C, Q>();
            var rake = Algorithms.DoBreadthSearch<T, C, Q>(control, control.InitialState, int.MaxValue, int.MaxValue, rakeWidth);
            while (rake.TryGetNext(out var next) && !control.ShouldStop())
            {
                DoBeamSearch<T, C, Q>(control, next.Item2, beamWidth, rank);
            }
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
        /// <param name="beamWidth">The parameter that governs how many parallel lines through the search tree should be considered during lookahead. For values > 1, rank must be defined as BeamSearch will be used.</param>
        /// <param name="rank">A function that ranks states, if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static Task<SearchControl<T, Q>> PilotMethodAsync<T, Q>(this SearchControl<T, Q> control, int beamWidth = 1, IComparer<T> rank = null)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => PilotMethod(control, beamWidth, rank));
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
        /// <param name="rank">A function that ranks states, if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static SearchControl<T, Q> PilotMethod<T, Q>(this SearchControl<T, Q> control, int beamWidth = 1, IComparer<T> rank = null)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            var state = control.InitialState;
            DoPilotMethod<T, Q>(control, state, beamWidth, rank);
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
        /// <param name="rank">A function that ranks states, if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns></returns>
        public static void DoPilotMethod<T, Q>(ISearchControl<T, Q> control, T state, int beamWidth, IComparer<T> rank)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException($"{beamWidth} needs to be greater or equal than 1", nameof(beamWidth));
            while (true)
            {
                T bestBranch = default(T);
                Q? bestBranchQuality = null;
                foreach (var next in state.GetBranches())
                {
                    if (rank == null && beamWidth == 1)
                    {
                        Algorithms.DoSearch(control, next, depthFirst: true, beamWidth: beamWidth, depthLimit: int.MaxValue, nodesReached: int.MaxValue);
                    } else
                    {
                        DoBeamSearch(control, next, beamWidth: beamWidth, rank: rank);
                    }
                    var quality = next.Quality;
                    if (!quality.HasValue) continue; // no solution achieved
                    if (!bestBranchQuality.HasValue || quality.Value.IsBetter(bestBranchQuality.Value))
                    {
                        bestBranch = next;
                        bestBranchQuality = quality;
                    }
                }
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
        /// <param name="rank">A function that ranks states, if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static Task<SearchControl<T, C, Q>> PilotMethodAsync<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth = 1, IComparer<T> rank = null)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => PilotMethod(control, beamWidth, rank));
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
        /// <param name="rank">A function that ranks states, if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static SearchControl<T, C, Q> PilotMethod<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth = 1, IComparer<T> rank = null)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            DoPilotMethod<T, C, Q>(control, state, beamWidth, rank);
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
        /// <param name="rank">A function that ranks states, if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="C">The choice type</typeparam>
        /// <typeparam name="Q">The type of the objective</typeparam>
        /// <returns></returns>
        public static void DoPilotMethod<T, C, Q>(ISearchControl<T, Q> control, T state, int beamWidth, IComparer<T> rank)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException($"{beamWidth} needs to be greater or equal than 1", nameof(beamWidth));
            while (true)
            {
                C bestBranch = default(C);
                Q? bestBranchQuality = null;
                foreach (var choice in state.GetChoices())
                {
                    var next = (T)state.Clone();
                    next.Apply(choice);
                    if (rank == null && beamWidth == 1)
                    {
                        Algorithms.DoDepthSearch<T, C, Q>(control, next, beamWidth: beamWidth);
                    } else
                    {
                        DoBeamSearch<T, C, Q>(control, next, beamWidth: beamWidth, rank: rank);
                    }
                    var quality = next.Quality;
                    if (!quality.HasValue) continue; // no solution achieved
                    if (!bestBranchQuality.HasValue || quality.Value.IsBetter(bestBranchQuality.Value))
                    {
                        bestBranch = choice;
                        bestBranchQuality = quality;
                    }
                }
                if (!bestBranchQuality.HasValue) return;
                state.Apply(bestBranch);
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
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The instance that performs the runtim control and tracking</returns>
        public static Task<SearchControl<T, Q>> NaiveLDSearchAsync<T, Q>(this SearchControl<T, Q> control, int maxDiscrepancy)
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
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The instance that performs the runtim control and tracking</returns>
        public static SearchControl<T, Q> NaiveLDSearch<T, Q>(this SearchControl<T, Q> control, int maxDiscrepancy)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            DoNaiveLDSearch(control, state, maxDiscrepancy);
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
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="state">The state from which to start exploring</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void DoNaiveLDSearch<T, Q>(ISearchControl<T, Q> control, T state, int maxDiscrepancy)
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
                    var prune = !next.state.Bound.IsBetter(control.BestQuality); // this check _MUST be done BEFORE_ VisitNode, which may update BestQuality
                    control.VisitNode(next.state);

                    if (prune)
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
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The instance that performs the runtim control and tracking</returns>
        public static Task<SearchControl<T, C, Q>> NaiveLDSearchAsync<T, C, Q>(this SearchControl<T, C, Q> control, int maxDiscrepancy)
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
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The instance that performs the runtim control and tracking</returns>
        public static SearchControl<T, C, Q> NaiveLDSearch<T, C, Q>(this SearchControl<T, C, Q> control, int maxDiscrepancy)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            DoNaiveLDSearch<T, C, Q>(control, state, maxDiscrepancy);
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
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="state">The state from which to start exploring</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void DoNaiveLDSearch<T, C, Q>(ISearchControl<T, Q> control, T state, int maxDiscrepancy)
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
            
            while (searchState.TryGetNext(out var next) && !control.ShouldStop())
            {
                var (depth, choice, discrepancy) = next;
                while (depth < stateDepth)
                {
                    state.UndoLast();
                    stateDepth--;
                }
                state.Apply(choice);
                var prune = !state.Bound.IsBetter(control.BestQuality); // this check _MUST be done BEFORE_ VisitNode, which may update BestQuality
                control.VisitNode(state);
                stateDepth++;

                if (prune)
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
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static Task<SearchControl<T, Q>> AnytimeLDSearchAsync<T, Q>(this SearchControl<T, Q> control, int maxDiscrepancy)
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
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static SearchControl<T, Q> AnytimeLDSearch<T, Q>(this SearchControl<T, Q> control, int maxDiscrepancy)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            DoAnytimeLDSearch(control, state, maxDiscrepancy);
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
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="state">The state from which to start exploring</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void DoAnytimeLDSearch<T, Q>(ISearchControl<T, Q> control, T state, int maxDiscrepancy)
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
                    var prune = !next.Bound.IsBetter(control.BestQuality); // this check _MUST be done BEFORE_ VisitNode, which may update BestQuality
                    control.VisitNode(next);

                    if (!prune)
                    {
                        searchState[discrepancy].Push(next);
                    }
                }
                if (searchState[K].Count == 0)
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
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static Task<SearchControl<T, C, Q>> AnytimeLDSearchAsync<T, C, Q>(this SearchControl<T, C, Q> control, int maxDiscrepancy)
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
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static SearchControl<T, C, Q> AnytimeLDSearch<T, C, Q>(this SearchControl<T, C, Q> control, int maxDiscrepancy)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            DoAnytimeLDSearch<T, C, Q>(control, state, maxDiscrepancy);
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
        /// <param name="control">The instance that performs the runtim control and tracking</param>
        /// <param name="state">The state from which to start exploring</param>
        /// <param name="maxDiscrepancy">The parameter that limits the 2nd, 3rd, ... branch choices</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns></returns>
        public static void DoAnytimeLDSearch<T, C, Q>(ISearchControl<T, Q> control, T state, int maxDiscrepancy)
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

                var prune = !choiceState.Bound.IsBetter(control.BestQuality); // this check _MUST be done BEFORE_ VisitNode, which may update BestQuality
                control.VisitNode(choiceState);

                if (!prune)
                {
                    foreach (var entry in choiceState.GetChoices()
                        .Select((ch, i) => (choice: ch, discrepancy: K + i))
                        .TakeWhile(x => x.discrepancy <= maxDiscrepancy).Reverse())
                    {
                        searchState[entry.discrepancy].Push((entry.choice, entry.discrepancy == K ? choiceState : (T)choiceState.Clone()));
                    }
                }
                if (searchState[K].Count == 0)
                {
                    K++;
                }
            }
        }
    }

    /// <summary>
    /// A simple ranking method that compares the states' bounds
    /// </summary>
    /// <typeparam name="T">The state type</typeparam>
    /// <typeparam name="Q">The quality type</typeparam>
    public class BoundComparer<T, Q> : IComparer<T>
        where T : IState<T, Q>
        where Q : struct, IQuality<Q>
    {
        public int Compare(T x, T y)
        {
            return x.Bound.CompareTo(y.Bound);
        }
    }

    /// <summary>
    /// A simple ranking method that compares the states' bounds
    /// </summary>
    /// <typeparam name="T">The state type</typeparam>
    /// <typeparam name="C">The choice type</typeparam>
    /// <typeparam name="Q">The quality type</typeparam>
    public class BoundComparer<T, C, Q> : IComparer<T>
        where T : class, IMutableState<T, C, Q>
        where Q : struct, IQuality<Q>
    {
        public int Compare(T x, T y)
        {
            return x.Bound.CompareTo(y.Bound);
        }
    }

    public static class HeuristicStateExtensions
    {
        public static Task<TState> BeamSearchAsync<TState, TQuality>(this IState<TState, TQuality> state, IComparer<TState> rank,
                 int beamWidth = 100, TimeSpan? runtime = null,
                long? nodelimit = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => BeamSearch((TState)state, beamWidth, rank, runtime, nodelimit, callback, token));
        }
        public static TState BeamSearch<TState, TQuality>(this IState<TState, TQuality> state, int beamWidth = 100,
                IComparer<TState> rank = null, TimeSpan? runtime = null,
                long? nodelimit = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.BeamSearch(beamWidth, rank).BestQualityState;
        }

        public static Task<TState> BeamSearchAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int beamWidth = 100,
                IComparer<TState> rank = null, TimeSpan? runtime = null,
                long? nodelimit = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => BeamSearch<TState, TChoice, TQuality>((TState)state, beamWidth, rank, runtime, nodelimit, callback, token));
        }

        public static TState BeamSearch<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int beamWidth = 100,
                IComparer<TState> rank = null, TimeSpan? runtime = null,
                long? nodelimit = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.BeamSearch(beamWidth, rank).BestQualityState;
        }

        public static Task<TState> RakeSearchAsync<TState, TQuality>(this IState<TState, TQuality> state, int rakeWidth = 100,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => RakeSearch((TState)state, rakeWidth, runtime, nodelimit, callback, token));
        }

        public static TState RakeSearch<TState, TQuality>(this IState<TState, TQuality> state, int rakeWidth = 100,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.RakeSearch(rakeWidth).BestQualityState;
        }

        public static Task<TState> RakeSearchAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int rakeWidth = 100,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => RakeSearch<TState, TChoice, TQuality>((TState)state, rakeWidth, runtime, nodelimit, callback, token));
        }

        public static TState RakeSearch<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int rakeWidth = 100,
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
            return control.RakeSearch(rakeWidth).BestQualityState;
        }

        public static Task<TState> RakeAndBeamSearchAsync<TState, TQuality>(this IState<TState, TQuality> state, IComparer<TState> rank,
                int rakeWidth = 100, int beamWidth = 100,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => RakeAndBeamSearch((TState)state, rakeWidth, beamWidth, rank, runtime, nodelimit, callback, token));
        }

        public static TState RakeAndBeamSearch<TState, TQuality>(this IState<TState, TQuality> state,
                int rakeWidth = 100, int beamWidth = 100, IComparer<TState> rank = null,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.RakeAndBeamSearch(rakeWidth, beamWidth, rank).BestQualityState;
        }

        public static Task<TState> RakeAndBeamSearchAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, IComparer<TState> rank,
                int rakeWidth = 100, int beamWidth = 100,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => RakeAndBeamSearch<TState, TChoice, TQuality>((TState)state, rakeWidth, beamWidth, rank, runtime, nodelimit, callback, token));
        }

        public static TState RakeAndBeamSearch<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state,
                int rakeWidth = 100, int beamWidth = 100, IComparer<TState> rank = null,
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
            return control.RakeAndBeamSearch(rakeWidth, beamWidth, rank).BestQualityState;
        }

        public static Task<TState> PilotMethodAsync<TState, TQuality>(this IState<TState, TQuality> state,
                int beamWidth = 1, IComparer<TState> rank = null,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => PilotMethod((TState)state, beamWidth, rank, runtime, nodelimit, callback, token));
        }

        public static TState PilotMethod<TState, TQuality>(this IState<TState, TQuality> state,
                int beamWidth = 1, IComparer<TState> rank = null,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.PilotMethod(beamWidth, rank).BestQualityState;
        }

        public static Task<TState> PilotMethodAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state,
                int beamWidth = 1, IComparer<TState> rank = null,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => PilotMethod<TState, TChoice, TQuality>((TState)state, beamWidth, rank, runtime, nodelimit, callback, token));
        }

        public static TState PilotMethod<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state,
                int beamWidth = 1, IComparer<TState> rank = null,
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
            return control.PilotMethod(beamWidth, rank).BestQualityState;
        }

        public static Task<TState> MCTSAsync<TState>(this IState<TState, Maximize> state,
                double confidence = 1.414213562373095, bool adaptiveConfidence = true,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, Maximize> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, Maximize>
        {
            return Task.Run(() => MCTS(state, confidence, adaptiveConfidence, seed, runtime, nodelimit, callback, token));
        }

        public static TState MCTS<TState>(this IState<TState, Maximize> state,
                double confidence = 1.414213562373095, bool adaptiveConfidence = true,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, Maximize> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, Maximize>
        {
            if (!runtime.HasValue && !nodelimit.HasValue && (token == default(CancellationToken) || token == CancellationToken.None))
                throw new ArgumentException("No termination condition provided for MCTS");
            var control = SearchControl<TState, Maximize>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            Action<MCTSNode<TState, Maximize>, TState> updateNodeScore = (node, s) => node.Score += s.Quality.Value.Value;
            return MonteCarloTreeSearch<TState, Maximize>.Search(control, updateNodeScore, confidence, adaptiveConfidence, seed).State;
        }

        public static Task<TState> MCTSAsync<TState>(this IState<TState, Minimize> state,
                double confidence = 1.414213562373095, bool adaptiveConfidence = true,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, Minimize> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, Minimize>
        {
            return Task.Run(() => MCTS(state, confidence, adaptiveConfidence, seed, runtime, nodelimit, callback, token));
        }

        public static TState MCTS<TState>(this IState<TState, Minimize> state,
                double confidence = 1.414213562373095, bool adaptiveConfidence = true,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, Minimize> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, Minimize>
        {
            if (!runtime.HasValue && !nodelimit.HasValue && (token == default(CancellationToken) || token == CancellationToken.None))
                throw new ArgumentException("No termination condition provided for MCTS");
            var control = SearchControl<TState, Minimize>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            Action<MCTSNode<TState, Minimize>, TState> updateNodeScore = (node, s) => node.Score -= s.Quality.Value.Value;
            return MonteCarloTreeSearch<TState, Minimize>.Search(control, updateNodeScore, confidence, adaptiveConfidence, seed).State;
        }

        public static Task<TState> MCTSAsync<TState, TChoice>(this IMutableState<TState, TChoice, Maximize> state,
                double confidence = 1.414213562373095, bool adaptiveConfidence = true, 
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, Maximize> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, Maximize>
        {
            return Task.Run(() => MCTS(state, confidence, adaptiveConfidence, seed, runtime, nodelimit, callback, token));
        }

        public static TState MCTS<TState, TChoice>(this IMutableState<TState, TChoice, Maximize> state,
                double confidence = 1.414213562373095, bool adaptiveConfidence = true,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, Maximize> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, Maximize>
        {
            if (!runtime.HasValue && !nodelimit.HasValue && (token == default(CancellationToken) || token == CancellationToken.None))
                throw new ArgumentException("No termination condition provided for MCTS");
            var control = SearchControl<TState, TChoice, Maximize>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            Action<MCTSNode<TState, TChoice, Maximize>, TState> updateNodeScore = (node, s) => node.Score += s.Quality.Value.Value;
            return MonteCarloTreeSearch<TState, TChoice, Maximize>.Search(control, updateNodeScore, confidence, adaptiveConfidence, seed).State;
        }

        public static Task<TState> MCTSAsync<TState, TChoice>(this IMutableState<TState, TChoice, Minimize> state,
                double confidence = 1.414213562373095, bool adaptiveConfidence = true, 
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, Minimize> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, Minimize>
        {
            return Task.Run(() => MCTS(state, confidence, adaptiveConfidence, seed, runtime, nodelimit, callback, token));
        }

        public static TState MCTS<TState, TChoice>(this IMutableState<TState, TChoice, Minimize> state,
                double confidence = 1.414213562373095, bool adaptiveConfidence = true, 
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, Minimize> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, Minimize>
        {
            if (!runtime.HasValue && !nodelimit.HasValue && (token == default(CancellationToken) || token == CancellationToken.None))
                throw new ArgumentException("No termination condition provided for MCTS");
            var control = SearchControl<TState, TChoice, Minimize>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            Action<MCTSNode<TState, TChoice, Minimize>, TState> updateNodeScore = (node, s) => node.Score -= s.Quality.Value.Value;
            return MonteCarloTreeSearch<TState, TChoice, Minimize>.Search(control, updateNodeScore, confidence, adaptiveConfidence, seed).State;
        }
        
        public static Task<TState> NaiveLDSearchAsync<TState, TQuality>(this IState<TState, TQuality> state, int maxDiscrepancy = 1,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => NaiveLDSearch(state, maxDiscrepancy, seed, runtime, nodelimit, callback, token));
        }
        
        public static TState NaiveLDSearch<TState, TQuality>(this IState<TState, TQuality> state, int maxDiscrepancy = 1,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.NaiveLDSearch(maxDiscrepancy).BestQualityState;
        }
        
        public static Task<TState> NaiveLDSearchAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int maxDiscrepancy = 1,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => NaiveLDSearch(state, maxDiscrepancy, seed, runtime, nodelimit, callback, token));
        }
        
        public static TState NaiveLDSearch<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int maxDiscrepancy = 1,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.NaiveLDSearch(maxDiscrepancy).BestQualityState;
        }

        public static Task<TState> AnytimeLDSearchAsync<TState, TQuality>(this IState<TState, TQuality> state, int maxDiscrepancy = 1,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => AnytimeLDSearch(state, maxDiscrepancy, seed, runtime, nodelimit, callback, token));
        }
        
        public static TState AnytimeLDSearch<TState, TQuality>(this IState<TState, TQuality> state, int maxDiscrepancy = 1,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.AnytimeLDSearch(maxDiscrepancy).BestQualityState;
        }
        
        public static Task<TState> AnytimeLDSearchAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int maxDiscrepancy = 1,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => AnytimeLDSearch(state, maxDiscrepancy, seed, runtime, nodelimit, callback, token));
        }
        
        public static TState AnytimeLDSearch<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int maxDiscrepancy = 1,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.NaiveLDSearch(maxDiscrepancy).BestQualityState;
        }
    }
}