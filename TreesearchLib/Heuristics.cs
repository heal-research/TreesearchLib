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
        /// Beam search uses several parallel traces. When called without a rank function, it is assumed
        /// that the order in which the branches are yielded should be used. To prevent a collapse of the
        /// search tree by a node with a large number of branches, a round-robin strategy is used. Thus, the
        /// first branch from every node at a layer is used, before the second branch.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        public static Task<SearchControl<T, Q>> BeamSearchAsync<T, Q>(this SearchControl<T, Q> control, int beamWidth)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BeamSearch(control, beamWidth));
        }
        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static Task<SearchControl<T, Q>> BeamSearchAsync<T, Q>(this SearchControl<T, Q> control, int beamWidth, IComparer<T> rank)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BeamSearch(control, beamWidth, rank));
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called without a rank function, it is assumed
        /// that the order in which the branches are yielded should be used. To prevent a collapse of the
        /// search tree by a node with a large number of branches, a round-robin strategy is used. Thus, the
        /// first branch from every node at a layer is used, before the second branch.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static SearchControl<T, Q> BeamSearch<T, Q>(this SearchControl<T, Q> control, int beamWidth)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            return DoBeamSearch(control, control.InitialState, beamWidth);
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static SearchControl<T, Q> BeamSearch<T, Q>(this SearchControl<T, Q> control, int beamWidth, IComparer<T> rank)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (rank == null) throw new ArgumentNullException(nameof(rank));
            return DoBeamSearch(control, control.InitialState, beamWidth, rank);
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called without a rank function, it is assumed
        /// that the order in which the branches are yielded should be used. To prevent a collapse of the
        /// search tree by a node with a large number of branches, a round-robin strategy is used. Thus, the
        /// first branch from every node at a layer is used, before the second branch.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        public static Task<SearchControl<T, C, Q>> BeamSearchAsync<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BeamSearch(control, beamWidth));
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static Task<SearchControl<T, C, Q>> BeamSearchAsync<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth, IComparer<T> rank)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BeamSearch(control, beamWidth, rank));
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called without a rank function, it is assumed
        /// that the order in which the branches are yielded should be used. To prevent a collapse of the
        /// search tree by a node with a large number of branches, a round-robin strategy is used. Thus, the
        /// first branch from every node at a layer is used, before the second branch.
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static SearchControl<T, C, Q> BeamSearch<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            return DoBeamSearch(control, control.InitialState, beamWidth);
        }

        /// <summary>
        /// Beam search uses several parallel traces. When called with a rank function, all nodes of the next layer are gathered
        /// and then sorted by the rank function (using a stable sort).
        /// </summary>
        /// <param name="control">The runtime control and tracking</param>
        /// <param name="beamWidth">The maximum number of parallel traces</param>
        /// <param name="rank">The rank function that determines the order of nodes</param>
        /// <typeparam name="T">The state type</typeparam>
        /// <typeparam name="Q">The quality type</typeparam>
        /// <returns>The runtime control and tracking instance after the search</returns>
        public static SearchControl<T, C, Q> BeamSearch<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth, IComparer<T> rank)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (rank == null) throw new ArgumentNullException(nameof(rank));
            return DoBeamSearch(control, control.InitialState, beamWidth, rank);
        }

        private static SearchControl<T, Q> DoBeamSearch<T, Q>(SearchControl<T, Q> control, T state, int beamWidth)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            var currentLayer = new Queue<T>();
            currentLayer.Enqueue(state);
            var nextlayer = new List<Queue<T>> { new Queue<T>() };

            while (!control.ShouldStop())
            {
                var branch = 0;
                var nothing = true;

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

                        nextlayer[branch].Enqueue(next);
                        nothing = false;

                        if (nextlayer[branch].Count >= beamWidth)
                        {
                            // not necessary to store more solutions per branch than beamWidth
                            break;
                        }
                    }
                    if (control.ShouldStop())
                    {
                        nothing = true;
                        break;
                    } else
                    {
                        branch++;
                        if (nextlayer.Count == branch)
                        {
                            nextlayer.Add(new Queue<T>());
                        }
                    }
                }

                if (nothing)
                {
                    break;
                }

                while (!nothing && currentLayer.Count < beamWidth)
                {
                    nothing = true;
                    for (var i = 0; i < nextlayer.Count; i++)
                    {
                        if (nextlayer[i].Count > 0)
                        {
                            currentLayer.Enqueue(nextlayer[i].Dequeue());
                            nothing = false;
                        }
                        if (currentLayer.Count >= beamWidth)
                        {
                            break;
                        }
                    }
                }

                if (currentLayer.Count == beamWidth)
                {
                    // empty next layer for new round
                    for (var i = 0; i < nextlayer.Count; i++)
                    {
                        nextlayer[i].Clear();
                    }
                } // else nextlayer has been emptied in the previous loop
            }
            return control;
        }

        private static SearchControl<T, Q> DoBeamSearch<T,Q>(SearchControl<T, Q> control, T state, int beamWidth, IComparer<T> rank)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
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
            return control;
        }

        private static SearchControl<T, C, Q> DoBeamSearch<T, C, Q>(SearchControl<T, C, Q> control, T state, int beamWidth)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var currentLayer = new Queue<T>();
            currentLayer.Enqueue(state);
            var nextlayer = new List<Queue<T>> { new Queue<T>() };
            while (!control.ShouldStop())
            {
                var branch = 0;
                var nothing = true;

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

                        nextlayer[branch].Enqueue(next);
                        nothing = false;

                        if (nextlayer[branch].Count >= beamWidth)
                        {
                            // not necessary to store more solutions per branch than beamWidth
                            break;
                        }
                    }
                    if (control.ShouldStop())
                    {
                        nothing = true;
                        break;
                    } else
                    {
                        branch++;
                        if (nextlayer.Count == branch)
                        {
                            nextlayer.Add(new Queue<T>());
                        }
                    }
                }

                if (nothing)
                {
                    break;
                }

                while (!nothing && currentLayer.Count < beamWidth)
                {
                    nothing = true;
                    for (var i = 0; i < nextlayer.Count; i++)
                    {
                        if (nextlayer[i].Count > 0)
                        {
                            currentLayer.Enqueue(nextlayer[i].Dequeue());
                            nothing = false;
                        }
                        if (currentLayer.Count >= beamWidth)
                        {
                            break;
                        }
                    }
                }

                if (currentLayer.Count == beamWidth)
                {
                    // empty next layer for new round
                    for (var i = 0; i < nextlayer.Count; i++)
                    {
                        nextlayer[i].Clear();
                    }
                } // else nextlayer has been emptied in the previous loop
            }
            return control;
        }

        private static SearchControl<T, C, Q> DoBeamSearch<T, C, Q>(SearchControl<T, C, Q> control, T state, int beamWidth, IComparer<T> rank)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
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
            return control;
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
            var rake = Algorithms.DoBreadthSearch(control, control.InitialState, int.MaxValue, int.MaxValue, rakeWidth);
            while (rake.TryGetNext(out var next) && !control.ShouldStop())
            {
                Algorithms.DoDepthSearch(control, next.Item2, beamWidth: 1);
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
            var rake = Algorithms.DoSearch(control, control.InitialState, false, int.MaxValue, int.MaxValue, rakeWidth);
            while (rake.TryGetNext(out var next) && !control.ShouldStop())
            {
                if (rank != null) DoBeamSearch(control, next.Item2, beamWidth, rank);
                else DoBeamSearch(control, next.Item2, beamWidth);
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
            var rake = Algorithms.DoBreadthSearch(control, control.InitialState, int.MaxValue, int.MaxValue, rakeWidth);
            while (rake.TryGetNext(out var next) && !control.ShouldStop())
            {
                if (rank != null) DoBeamSearch(control, next.Item2, beamWidth, rank);
                else DoBeamSearch(control, next.Item2, beamWidth);
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
        /// <param name="beamWidth">The parameter that governs how many parallel lines through the search tree should be considered during lookahead</param>
        /// <param name="rank">A function that ranks states, if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Q"></typeparam>
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
        /// <param name="beamWidth">The parameter that governs how many parallel lines through the search tree should be considered during lookahead</param>
        /// <param name="rank">A function that ranks states, if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Q"></typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static SearchControl<T, Q> PilotMethod<T, Q>(this SearchControl<T, Q> control, int beamWidth = 1, IComparer<T> rank = null)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException($"{beamWidth} needs to be greater or equal than 1", nameof(beamWidth));
            var state = control.InitialState;
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
                if (!bestBranchQuality.HasValue) return control;
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
        /// <param name="beamWidth">The parameter that governs how many parallel lines through the search tree should be considered during lookahead</param>
        /// <param name="rank">A function that ranks states, if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Q"></typeparam>
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
        /// <param name="beamWidth">The parameter that governs how many parallel lines through the search tree should be considered during lookahead</param>
        /// <param name="rank">A function that ranks states, if it is null the rank is implicit by the order in which the branches are generated.</param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="Q"></typeparam>
        /// <returns>The control object with the tracking.</returns>
        public static SearchControl<T, C, Q> PilotMethod<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth = 1, IComparer<T> rank = null)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException($"{beamWidth} needs to be greater or equal than 1", nameof(beamWidth));
            var state = (T)control.InitialState.Clone();
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
                        Algorithms.DoDepthSearch(control, next, beamWidth: beamWidth);
                    } else
                    {
                        DoBeamSearch(control, next, beamWidth: beamWidth, rank: rank);
                    }
                    var quality = next.Quality;
                    if (!quality.HasValue) continue; // no solution achieved
                    if (!bestBranchQuality.HasValue || quality.Value.IsBetter(bestBranchQuality.Value))
                    {
                        bestBranch = choice;
                        bestBranchQuality = quality;
                    }
                }
                if (!bestBranchQuality.HasValue) return control;
                state.Apply(bestBranch);
            }
        }
    }

    public class BoundComparer<T, Q> : IComparer<T>
        where T : IState<T, Q>
        where Q : struct, IQuality<Q>
    {
        public int Compare(T x, T y)
        {
            return x.Bound.CompareTo(y.Bound);
        }
    }

    public class BoundAndIndexComparer<T, Q> : IComparer<(int, T)>
        where T : IState<T, Q>
        where Q : struct, IQuality<Q>
    {
        public int Compare((int, T) x, (int, T) y)
        {
            var comp = x.Item2.Bound.CompareTo(y.Item2.Bound);
            if (comp == 0)
            {
                comp = x.Item1.CompareTo(y.Item1);
            }
            return comp;
        }
    }

    public class BoundComparer<T, C, Q> : IComparer<T>
        where T : class, IMutableState<T, C, Q>
        where Q : struct, IQuality<Q>
    {
        public int Compare(T x, T y)
        {
            return x.Bound.CompareTo(y.Bound);
        }
    }

    public class BoundAndIndexComparer<T, C, Q> : IComparer<(int, T)>
        where T : class, IMutableState<T, C, Q>
        where Q : struct, IQuality<Q>
    {
        public int Compare((int, T) x, (int, T) y)
        {
            var comp = x.Item2.Bound.CompareTo(y.Item2.Bound);
            if (comp == 0)
            {
                comp = x.Item1.CompareTo(y.Item1);
            }
            return comp;
        }
    }

    public static class HeuristicStateExtensions
    {
        public static Task<TState> BeamSearchAsync<TState, TQuality>(this IState<TState, TQuality> state, int beamWidth = 100,
                IComparer<TState> rank = null, TimeSpan? runtime = null,
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
            return (rank != null ? control.BeamSearch(beamWidth, rank) : control.BeamSearch(beamWidth)).BestQualityState;
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
            return (rank != null ? control.BeamSearch(beamWidth, rank) : control.BeamSearch(beamWidth)).BestQualityState;
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

        public static Task<TState> RakeAndBeamSearchAsync<TState, TQuality>(this IState<TState, TQuality> state, int rakeWidth = 100,
                int beamWidth = int.MaxValue, IComparer<TState> rank = null,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => RakeAndBeamSearch((TState)state, rakeWidth, beamWidth, rank, runtime, nodelimit, callback, token));
        }

        public static TState RakeAndBeamSearch<TState, TQuality>(this IState<TState, TQuality> state, int rakeWidth = 100,
                int beamWidth = int.MaxValue, IComparer<TState> rank = null,
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

        public static Task<TState> RakeAndBeamSearchAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int rakeWidth = 100,
                int beamWidth = int.MaxValue, IComparer<TState> rank = null,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => RakeAndBeamSearch<TState, TChoice, TQuality>((TState)state, rakeWidth, beamWidth, rank, runtime, nodelimit, callback, token));
        }

        public static TState RakeAndBeamSearch<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int rakeWidth = 100,
                int beamWidth = int.MaxValue, IComparer<TState> rank = null,
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
                int beamWidth = int.MaxValue, IComparer<TState> rank = null,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => PilotMethod((TState)state, beamWidth, rank, runtime, nodelimit, callback, token));
        }

        public static TState PilotMethod<TState, TQuality>(this IState<TState, TQuality> state,
                int beamWidth = int.MaxValue, IComparer<TState> rank = null,
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
                int beamWidth = int.MaxValue, IComparer<TState> rank = null,
                TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => PilotMethod<TState, TChoice, TQuality>((TState)state, beamWidth, rank, runtime, nodelimit, callback, token));
        }

        public static TState PilotMethod<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state,
                int beamWidth = int.MaxValue, IComparer<TState> rank = null,
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
    }
}