using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TreesearchLib
{
    public static class Heuristics
    {
        public static Task<SearchControl<T, Q>> BeamSearchAsync<T, Q>(this SearchControl<T, Q> control, int beamWidth, IComparer<T> rank = null)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BeamSearch(control, beamWidth, rank), control.Cancellation);
        }

        public static SearchControl<T, Q> BeamSearch<T, Q>(this SearchControl<T, Q> control, int beamWidth, IComparer<T> rank = null)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (rank == null) rank = new BoundComparer<T, Q>();
            return DoBeamSearch(control, control.InitialState, beamWidth, rank);
        }

        public static Task<SearchControl<T, C, Q>> BeamSearchAsync<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth, IComparer<T> rank = null)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BeamSearch(control, beamWidth, rank), control.Cancellation);
        }

        public static SearchControl<T, C, Q> BeamSearch<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth, IComparer<T> rank = null)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (beamWidth <= 0) throw new ArgumentException("A beam width of 0 or less is not possible");
            if (rank == null) rank = new BoundComparer<T, C, Q>();
            return DoBeamSearch(control, control.InitialState, beamWidth, rank);
        }

        private static SearchControl<T, Q> DoBeamSearch<T, Q>(SearchControl<T, Q> control, T state, int beamWidth, IComparer<T> rank)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
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

        private static SearchControl<T, C, Q> DoBeamSearch<T, C, Q>(SearchControl<T, C, Q> control, T state, int beamWidth, IComparer<T> rank)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var searchState = new FIFOCollection<T>(state);
            var nextlayer = new List<T>();
            while (!control.ShouldStop())
            {
                nextlayer.Clear();
                while (searchState.TryGetNext(out var currentState))
                {
                    foreach (var choice in currentState.GetChoices())
                    {
                        var next = (T)currentState.Clone();
                        next.Apply(choice);
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
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => RakeSearch(control, rakeWidth), control.Cancellation);
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
            return Task.Run(() => RakeSearch(control, rakeWidth), control.Cancellation);
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
            return Task.Run(() => RakeAndBeamSearch(control, rakeWidth, beamWidth, rank), control.Cancellation);
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
            return Task.Run(() => RakeAndBeamSearch(control, rakeWidth, beamWidth, rank), control.Cancellation);
        }

        public static SearchControl<T, C, Q> RakeAndBeamSearch<T, C, Q>(this SearchControl<T, C, Q> control, int rakeWidth, int beamWidth, IComparer<T> rank = null)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (rank == null) rank = new BoundComparer<T, C, Q>();
            var rake = Algorithms.DoBreadthSearch(control, control.InitialState, int.MaxValue, int.MaxValue, rakeWidth);
            while (rake.TryGetNext(out var next) && !control.ShouldStop())
            {
                DoBeamSearch(control, next.Item2, beamWidth, rank);
            }
            return control;
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
        public static Task<TState> BeamSearchAsync<TState, TQuality>(this IState<TState, TQuality> state, int beamWidth = int.MaxValue,
                IComparer<TState> rank = null, TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => BeamSearch((TState)state, beamWidth, rank, runtime, callback, token));
        }
        public static TState BeamSearch<TState, TQuality>(this IState<TState, TQuality> state, int beamWidth = int.MaxValue,
                IComparer<TState> rank = null, TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.BeamSearch(beamWidth, rank).BestQualityState;
        }

        public static Task<TState> BeamSearchAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int beamWidth = int.MaxValue,
                IComparer<TState> rank = null, TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => BeamSearch<TState, TChoice, TQuality>((TState)state, beamWidth, rank, runtime, callback, token));
        }

        public static TState BeamSearch<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int beamWidth = int.MaxValue,
                IComparer<TState> rank = null, TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.BeamSearch(beamWidth, rank).BestQualityState;
        }

        public static Task<TState> RakeSearchAsync<TState, TQuality>(this IState<TState, TQuality> state, int rakeWidth = 100,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => RakeSearch((TState)state, rakeWidth, runtime, callback, token));
        }

        public static TState RakeSearch<TState, TQuality>(this IState<TState, TQuality> state, int rakeWidth = 100,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.RakeSearch(rakeWidth).BestQualityState;
        }

        public static Task<TState> RakeSearchAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int rakeWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => RakeSearch<TState, TChoice, TQuality>((TState)state, rakeWidth, runtime, callback, token));
        }

        public static TState RakeSearch<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int rakeWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.RakeSearch(rakeWidth).BestQualityState;
        }

        public static Task<TState> RakeAndBeamSearchAsync<TState, TQuality>(this IState<TState, TQuality> state, int rakeWidth = 100,
                int beamWidth = int.MaxValue, IComparer<TState> rank = null, TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => RakeAndBeamSearch((TState)state, rakeWidth, beamWidth, rank, runtime, callback, token));
        }

        public static TState RakeAndBeamSearch<TState, TQuality>(this IState<TState, TQuality> state, int rakeWidth = 100,
                int beamWidth = int.MaxValue, IComparer<TState> rank = null, TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.RakeAndBeamSearch(rakeWidth, beamWidth, rank).BestQualityState;
        }

        public static Task<TState> RakeAndBeamSearchAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int rakeWidth = int.MaxValue,
                int beamWidth = int.MaxValue, IComparer<TState> rank = null, TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => RakeAndBeamSearch<TState, TChoice, TQuality>((TState)state, rakeWidth, beamWidth, rank, runtime, callback, token));
        }

        public static TState RakeAndBeamSearch<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int rakeWidth = int.MaxValue,
                int beamWidth = int.MaxValue, IComparer<TState> rank = null, TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.RakeAndBeamSearch(rakeWidth, beamWidth, rank).BestQualityState;
        }
    }
}