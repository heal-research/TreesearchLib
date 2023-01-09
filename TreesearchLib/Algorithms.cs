using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TreesearchLib
{
    public static class Algorithms
    {
        public static Task<SearchControl<T, Q>> DepthFirstAsync<T, Q>(this SearchControl<T, Q> control, int filterWidth = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => DepthFirst(control, filterWidth: filterWidth));
        }

        public static SearchControl<T, Q> DepthFirst<T, Q>(this SearchControl<T, Q> control, int filterWidth = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            DoDepthSearch(control, control.InitialState, filterWidth);
            return control;
        }

        public static Task<SearchControl<T, Q>> BreadthFirstAsync<T, Q>(this SearchControl<T, Q> control, int filterWidth = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BreadthFirst(control, filterWidth));
        }

        public static SearchControl<T, Q> BreadthFirst<T, Q>(this SearchControl<T, Q> control, int filterWidth = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            DoBreadthSearch(control, control.InitialState, filterWidth, int.MaxValue, int.MaxValue);
            return control;
        }

        public static Task<SearchControl<T, C, Q>> DepthFirstAsync<T, C, Q>(this SearchControl<T, C, Q> control, int filterWidth = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => DepthFirst(control, filterWidth));
        }

        public static SearchControl<T, C, Q> DepthFirst<T, C, Q>(this SearchControl<T, C, Q> control, int filterWidth = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            DoDepthSearch<T, C, Q>(control, state, filterWidth);
            return control;
        }

        public static Task<SearchControl<T, C, Q>> BreadthFirstAsync<T, C, Q>(this SearchControl<T, C, Q> control, int filterWidth = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BreadthFirst<T, C, Q>(control, filterWidth));
        }

        public static SearchControl<T, C, Q> BreadthFirst<T, C, Q>(this SearchControl<T, C, Q> control, int filterWidth = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            DoBreadthSearch<T, C, Q>(control, state, filterWidth, int.MaxValue, int.MaxValue);
            return control;
        }
        
        public static void DoDepthSearch<T, Q>(ISearchControl<T, Q> control, T state, int filterWidth = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            var searchState = new LIFOCollection<T>();
            while (searchState.TryGetNext(out var currentState) && !control.ShouldStop())
            {
                foreach (var next in currentState.GetBranches().Reverse().Take(filterWidth))
                {
                    if (control.VisitNode(next) == VisitResult.Discard)
                    {
                        continue;
                    }

                    searchState.Store(next);
                }
            }
        }

        public static IStateCollection<T> DoBreadthSearch<T, Q>(ISearchControl<T, Q> control, T state, int filterWidth, int depthLimit, int nodesReached)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            if (depthLimit <= 0) throw new ArgumentException($"{depthLimit} needs to be breater or equal than 0", nameof(depthLimit));
            if (nodesReached <= 0) throw new ArgumentException($"{nodesReached} needs to be breater or equal than 0", nameof(nodesReached));
            var searchState = new BiLevelFIFOCollection<T>(state);
            var depth = 0;
            while (searchState.GetQueueNodes > 0 && depth < depthLimit && searchState.GetQueueNodes < nodesReached && !control.ShouldStop())
            {
                while (searchState.TryFromGetQueue(out var currentState) && !control.ShouldStop())
                {
                    foreach (var next in currentState.GetBranches().Take(filterWidth))
                    {
                        if (control.VisitNode(next) == VisitResult.Discard)
                        {
                            continue;
                        }

                        searchState.ToPutQueue(next);
                    }
                }
                depth++;
                searchState.SwapQueues();
            }
            return searchState.ToSingleLevel();
        }

        public static void DoDepthSearch<T, C, Q>(ISearchControl<T, Q> control, T state, int filterWidth = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            var searchState = new LIFOCollection<(int, C)>();
            var stateDepth = 0;
            foreach (var entry in state.GetChoices().Take(filterWidth).Reverse().Select(choice => (stateDepth, choice)))
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
                stateDepth++;

                if (control.VisitNode(state) == VisitResult.Discard)
                {
                    continue;
                }


                foreach (var entry in state.GetChoices().Take(filterWidth).Reverse().Select(ch => (stateDepth, ch)))
                {
                    searchState.Store(entry);
                }
            }
        }

        public static IStateCollection<T> DoBreadthSearch<T, C, Q>(ISearchControl<T, Q> control, T state, int filterWidth, int depthLimit, int nodesReached)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            if (filterWidth <= 0) throw new ArgumentException($"{filterWidth} needs to be greater or equal than 0", nameof(filterWidth));
            if (depthLimit <= 0) throw new ArgumentException($"{depthLimit} needs to be breater or equal than 0", nameof(depthLimit));
            if (nodesReached <= 0) throw new ArgumentException($"{nodesReached} needs to be breater or equal than 0", nameof(nodesReached));
            var searchState = new BiLevelFIFOCollection<T>(state);
            var depth = 0;
            while (searchState.GetQueueNodes > 0 && depth < depthLimit && searchState.GetQueueNodes < nodesReached && !control.ShouldStop())
            {
                while (searchState.TryFromGetQueue(out var currentState) && !control.ShouldStop())
                {
                    foreach (var next in currentState.GetChoices().Take(filterWidth))
                    {
                        var clone = (T)currentState.Clone();
                        clone.Apply(next);

                        if (control.VisitNode(clone) == VisitResult.Discard)
                        {
                            continue;
                        }

                        searchState.ToPutQueue(clone);
                    }
                }
                depth++;
                searchState.SwapQueues();
            }
            return searchState.ToSingleLevel();
        }
    }

    public static class AlgorithmStateExtensions
    {
        public static Task<TState> DepthFirstAsync<TState, TQuality>(this IState<TState, TQuality> state, int filterWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => DepthFirst((TState)state, filterWidth, runtime, callback, token));
        }
        public static TState DepthFirst<TState, TQuality>(this IState<TState, TQuality> state, int filterWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.DepthFirst(filterWidth).BestQualityState;
        }

        public static Task<TState> DepthFirstAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int filterWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => DepthFirst<TState, TChoice, TQuality>((TState)state, filterWidth, runtime, callback, token));
        }

        public static TState DepthFirst<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int filterWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.DepthFirst(filterWidth).BestQualityState;
        }

        public static Task<TState> BreadthFirstAsync<TState, TQuality>(this IState<TState, TQuality> state, int filterWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => BreadthFirst((TState)state, filterWidth, runtime, callback, token));
        }

        public static TState BreadthFirst<TState, TQuality>(this IState<TState, TQuality> state, int filterWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.BreadthFirst(filterWidth).BestQualityState;
        }

        public static Task<TState> BreadthFirstAsync<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int filterWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            return Task.Run(() => BreadthFirst<TState, TChoice, TQuality>((TState)state, filterWidth, runtime, callback, token));
        }

        public static TState BreadthFirst<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state, int filterWidth = int.MaxValue,
                TimeSpan? runtime = null, QualityCallback<TState, TQuality> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            var control = SearchControl<TState, TChoice, TQuality>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            return control.BreadthFirst(filterWidth).BestQualityState;
        }
    }
}