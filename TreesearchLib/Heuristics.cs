using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TreesearchLib
{
    public static class Heuristics
    {
        public static Task<SearchControl<T, Q>> BeamSearchAsync<T, Q>(this SearchControl<T, Q> control, int beamWidth, IComparer<T> rank)
            where T : class, IState<T, Q>
            where Q : struct, IQuality<Q>, IComparable<Q>
        {
            return Task.Run(() => BeamSearch(control, beamWidth, rank), control.Cancellation);
        }

        public static SearchControl<T, Q> BeamSearch<T, Q>(this SearchControl<T, Q> control, int beamWidth, IComparer<T> rank)
            where T : class, IState<T, Q>
            where Q : struct, IQuality<Q>, IComparable<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            var searchState = new FIFOCollection<T>(control.InitialState);
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
                    if (control.ShouldStop()) {
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
    }
}