using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TreesearchLib
{
    public static class Algorithms
    {
        public static Task<SearchControl<T, Q>> DepthFirstAsync<T, Q>(this SearchControl<T, Q> control, int beamWidth = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => DepthFirst(control, beamWidth: beamWidth), control.Cancellation);
        }

        public static SearchControl<T, Q> DepthFirst<T, Q>(this SearchControl<T, Q> control, int beamWidth = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            DoSearch(control, control.InitialState, true, beamWidth, int.MaxValue, int.MaxValue);
            return control;
        }

        public static Task<SearchControl<T, Q>> BreadthFirstAsync<T, Q>(this SearchControl<T, Q> control, int beamWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BreadthFirst(control, beamWidth, depthLimit), control.Cancellation);
        }

        public static SearchControl<T, Q> BreadthFirst<T, Q>(this SearchControl<T, Q> control, int beamWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            DoSearch(control, control.InitialState, false, beamWidth, depthLimit, int.MaxValue);
            return control;
        }

        public static Task<SearchControl<T, C, Q>> DepthFirstAsync<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => DepthFirst(control, beamWidth), control.Cancellation);
        }

        public static SearchControl<T, C, Q> DepthFirst<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            DoDepthSearch<T, C, Q>(control, state, beamWidth);
            return control;
        }

        public static Task<SearchControl<T, C, Q>> BreadthFirstAsync<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BreadthFirst<T, C, Q>(control, beamWidth, depthLimit), control.Cancellation);
        }

        public static SearchControl<T, C, Q> BreadthFirst<T, C, Q>(this SearchControl<T, C, Q> control, int beamWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            DoBreadthSearch<T, C, Q>(control, state, beamWidth, depthLimit, int.MaxValue);
            return control;
        }

        public static IStateCollection<(int, T)> DoSearch<T, Q>(ISearchControl<T, Q> control, T state, bool depthFirst, int beamWidth, int depthLimit, int nodesReached)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            var searchState = depthFirst ? (IStateCollection<(int, T)>)new LIFOCollection<(int, T)>() : new FIFOCollection<(int, T)>();
            searchState.Store((0, state));
            if (searchState.Nodes >= nodesReached)
                return searchState;
            
            while (searchState.TryGetNext(out var tup) && !control.ShouldStop())
            {
                var (depth, currentState) = tup;
                var branches = currentState.GetBranches();
                if (depthFirst) branches = branches.Reverse(); // the first choices are supposed to be preferable
                foreach (var next in branches.Take(beamWidth))
                {
                    
                    var prune = !next.Bound.IsBetter(control.BestQuality); // this check _MUST be done BEFORE_ VisitNode, which may update BestQuality
                    control.VisitNode(next);

                    if (prune || depth + 1 >= depthLimit)
                    {
                        continue;
                    }

                    searchState.Store((depth + 1, next));
                }
                if (searchState.Nodes >= nodesReached)
                    return searchState;
            }
            return searchState;
        }

        public static void DoDepthSearch<T, C, Q>(ISearchControl<T, Q> control, T state, int beamWidth = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var searchState = new LIFOCollection<(int, C)>();
            var stateDepth = 0;
            foreach (var entry in state.GetChoices().Take(beamWidth).Reverse().Select(choice => (stateDepth, choice)))
            {
                searchState.Store(entry);
            }

            while (searchState.TryGetNext(out var next) && !control.ShouldStop())
            {
                var (depth, choice) = next;
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


                foreach (var entry in state.GetChoices().Take(beamWidth).Reverse().Select(ch => (stateDepth, ch)))
                {
                    searchState.Store(entry);
                }
            }
        }

        public static IStateCollection<(int, T)> DoBreadthSearch<T, C, Q>(ISearchControl<T, Q> control, T state, int beamWidth, int depthLimit, int nodesReached)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var searchState = new FIFOCollection<(int, T)>();
            searchState.Store((0, state));
            if (searchState.Nodes >= nodesReached)
                return searchState;

            while (searchState.TryGetNext(out var tup) && !control.ShouldStop())
            {
                var (depth, currentState) = tup;

                foreach (var next in currentState.GetChoices().Take(beamWidth))
                {
                    var clone = (T)currentState.Clone();
                    clone.Apply(next);
                    var prune = !clone.Bound.IsBetter(control.BestQuality); // this check _MUST be done BEFORE_ VisitNode, which may update BestQuality
                    control.VisitNode(clone);

                    if (prune || depth + 1 >= depthLimit)
                    {
                        continue;
                    }

                    searchState.Store((depth + 1, clone));
                }
                if (searchState.Nodes >= nodesReached)
                    return searchState;
            }
            return searchState;
        }
    }

    public static class AlgorithmStateExtensions
    {
        public static Task<TState> DepthFirstAsync<TState, TQuality>(this IState<TState, TQuality> state, int beamWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => DepthFirst((TState)state, beamWidth, runtime, callback, token));
        }
        public static TState DepthFirst<TState, TQuality>(this IState<TState, TQuality> state, int beamWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.DepthFirst(beamWidth).BestQualityState;
        }

        public static Task<TState> DepthFirstAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int beamWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => DepthFirst<TState, TChoice, TQuality>((TState)state, beamWidth, runtime, callback, token));
        }

        public static TState DepthFirst<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int beamWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.DepthFirst(beamWidth).BestQualityState;
        }

        public static Task<TState> BreadthFirstAsync<TState, TQuality>(this IState<TState, TQuality> state, int beamWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => BreadthFirst((TState)state, beamWidth, runtime, callback, token));
        }

        public static TState BreadthFirst<TState, TQuality>(this IState<TState, TQuality> state, int beamWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.BreadthFirst(beamWidth).BestQualityState;
        }

        public static Task<TState> BreadthFirstAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int beamWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => BreadthFirst<TState, TChoice, TQuality>((TState)state, beamWidth, runtime, callback, token));
        }

        public static TState BreadthFirst<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int beamWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.BreadthFirst(beamWidth).BestQualityState;
        }
    }
}