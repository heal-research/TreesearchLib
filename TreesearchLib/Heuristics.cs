using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TreesearchLib
{
    public static class Heuristics
    {
        public static Task<SearchControl<T, Q>> BeamSearchAsync<T, Q>(this SearchControl<T, Q> control, int beamWidth, IComparer<T> rank = null)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>, IComparable<Q>
        {
            return Task.Run(() => BeamSearch(control, beamWidth, rank), control.Cancellation);
        }

        public static SearchControl<T, Q> BeamSearch<T, Q>(this SearchControl<T, Q> control, int beamWidth, IComparer<T> rank = null)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>, IComparable<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (rank == null) rank = new BoundComparer<T, Q>();
            return DoBeamSearch(control, control.InitialState, beamWidth, rank);
        }

        private static SearchControl<T, Q> DoBeamSearch<T, Q>(SearchControl<T, Q> control, T state, int beamWidth, IComparer<T> rank)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>, IComparable<Q>
        {
            var searchState = new FIFOCollection<T>(state);
            var nextlayer = new List<T>();
            while (!control.ShouldStop())
            {
                nextlayer.Clear();
                while (searchState.TryGetNext(out var currentState))
                {
                    foreach (var next in currentState.GetBranches())
                    {
                        control.VisitNode(next);

                        if (!next.Bound.IsBetter(control.BestQuality))
                        {
                            continue;
                        }

                        if (!next.Quality.HasValue)
                            nextlayer.Add(next);
                    }
                    if (control.ShouldStop())
                    {
                        searchState.Clear();
                        break;
                    }
                }

                if (nextlayer.Count == 0)
                {
                    break;
                }

                foreach (var nextState in nextlayer.OrderBy(x => x, rank).Take(beamWidth))
                {
                    searchState.Store(nextState);
                }
            }
            return control;
        }

        public static Task<SearchControl<T, Q>> RakeSearchAsync<T, Q>(this SearchControl<T, Q> control, int rakeWidth)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>, IComparable<Q>
        {
            return Task.Run(() => RakeSearch(control, rakeWidth), control.Cancellation);
        }

        public static SearchControl<T, Q> RakeSearch<T, Q>(this SearchControl<T, Q> control, int rakeWidth)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>, IComparable<Q>
        {
            var rake = Algorithms.DoSearch(control, control.InitialState, false, int.MaxValue, int.MaxValue, rakeWidth);
            while (rake.TryGetNext(out var next) && !control.ShouldStop())
            {
                Algorithms.DoSearch(control, next.Item2, true, 1, int.MaxValue, int.MaxValue);
            }
            return control;
        }

        public static Task<SearchControl<T, Q>> RakeAndBeamSearchAsync<T, Q>(this SearchControl<T, Q> control, int rakeWidth, int beamWidth, IComparer<T> rank = null)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>, IComparable<Q>
        {
            return Task.Run(() => RakeAndBeamSearch(control, rakeWidth, beamWidth, rank), control.Cancellation);
        }

        public static SearchControl<T, Q> RakeAndBeamSearch<T, Q>(this SearchControl<T, Q> control, int rakeWidth, int beamWidth, IComparer<T> rank = null)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>, IComparable<Q>
        {
            if (rank == null) rank = new BoundComparer<T, Q>();
            var rake = Algorithms.DoSearch(control, control.InitialState, false, int.MaxValue, int.MaxValue, rakeWidth);
            while (rake.TryGetNext(out var next) && !control.ShouldStop())
            {
                DoBeamSearch(control, next.Item2, beamWidth, rank);
            }
            return control;
        }
    }

    public class BoundComparer<T, Q> : IComparer<T>
        where T : IState<T, Q>
        where Q : struct, IQuality<Q>, IComparable<Q>
    {
        public int Compare(T x, T y)
        {
            return x.Bound.CompareTo(y.Bound);
        }
    }
}