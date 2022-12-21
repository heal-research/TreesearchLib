using System;
using System.Linq;
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
            DoSearch<T, Q>(control, control.InitialState, true, beamWidth, int.MaxValue, int.MaxValue);
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
            DoSearch<T, Q>(control, control.InitialState, false, beamWidth, depthLimit, int.MaxValue);
            return control;
        }

        public static Task<SearchControlUndo<T, C, Q>> DepthFirstAsync<T, C, Q>(this SearchControlUndo<T, C, Q> control, int beamWidth = int.MaxValue)
            where T : class, IUndoState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => DepthFirst<T, C, Q>(control, beamWidth), control.Cancellation);
        }

        public static SearchControlUndo<T, C, Q> DepthFirst<T, C, Q>(this SearchControlUndo<T, C, Q> control, int beamWidth = int.MaxValue)
            where T : class, IUndoState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            var searchState = new LIFOCollection<Tuple<int, C>>();
            var stateDepth = 0;
            foreach (var entry in state.GetChoices().Take(beamWidth).Reverse().Select(choice => Tuple.Create(stateDepth, choice)))
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
                control.VisitNode(state);
                stateDepth++;
                
                if (!state.Bound.IsBetter(control.BestQuality))
                {
                    continue;
                }


                foreach (var entry in state.GetChoices().Take(beamWidth).Reverse().Select(ch => Tuple.Create(stateDepth, ch)))
                {
                    searchState.Store(entry);
                }
            }
            return control;
        }

        public static Task<SearchControlUndo<T, C, Q>> BreadthFirstAsync<T, C, Q>(this SearchControlUndo<T, C, Q> control, int beamWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IUndoState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return Task.Run(() => BreadthFirst<T, C, Q>(control, beamWidth, depthLimit), control.Cancellation);
        }

        public static SearchControlUndo<T, C, Q> BreadthFirst<T, C, Q>(this SearchControlUndo<T, C, Q> control, int beamWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IUndoState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var state = (T)control.InitialState.Clone();
            var searchState = new FIFOCollection<Tuple<int, T>>();
            searchState.Store(Tuple.Create(0, state));

            while (searchState.TryGetNext(out var tup) && !control.ShouldStop())
            {
                var (depth, currentState) = tup;

                foreach (var next in currentState.GetChoices().Take(beamWidth))
                {
                    var clone = (T)currentState.Clone();
                    clone.Apply(next);
                    control.VisitNode(clone);

                    if (!clone.Bound.IsBetter(control.BestQuality) || depth + 1 >= depthLimit)
                    {
                        continue;
                    }

                    if (!clone.Quality.HasValue)
                        searchState.Store(Tuple.Create(depth + 1, clone));
                }
            }
            return control;
        }

        internal static IStateCollection<(int, T)> DoSearch<T, Q>(SearchControl<T, Q> control, T state, bool depthFirst, int beamWidth, int depthLimit, int nodesReached)
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
                    control.VisitNode(next);

                    if (!next.Bound.IsBetter(control.BestQuality) || depth + 1 >= depthLimit)
                    {
                        continue;
                    }

                    if (!next.Quality.HasValue)
                        searchState.Store((depth + 1, next));
                }
                if (searchState.Nodes >= nodesReached)
                    return searchState;
            }
            return searchState;
        }
    }
}