using System;
using System.Linq;

namespace TreesearchLib
{
    public class Searcher
    {
        public static void Search<T, C, Q>(ISearchState<T> searchState, ref T bestState, SearchControl<Q> control)
        where T : class, ISearchable<C, Q>
        where Q : struct, IQuality<Q>
        {
            while (searchState.TryGetNext(out var currentState) && !control.ShouldStop(searchState.SearchType))
            {
                foreach (var choice in currentState.GetChoices().Take(control.BeamWidth))
                {
                    var next = (T)currentState.Clone();
                    next.Apply(choice);
                    control.VisitNode();
                    var lb = next.LowerBound;
                    var qual = next.Quality;

                    if (!lb.IsBetter(control.UpperBound) && (bestState != null || !qual.HasValue))
                    {
                        continue;
                    }
                    if (qual.HasValue && (qual.Value.IsBetter(control.UpperBound) || bestState == null))
                    {
                        control.FoundSolution(qual.Value);
                        bestState = next;
                    }

                    searchState.Store(next);
                }
            }
        }

        public static void SearchWithUndo<T, C, Q>(T state, ref T bestState, SearchControl<Q> control)
        where T : class, ISearchableWithUndo<C, Q>
        where Q : struct, IQuality<Q>
        {
            var searchState = new DFSState<Tuple<int, C>>();
            var initialDepth = state.ChoicesMade;
            foreach (var entry in state.GetChoices().Take(control.BeamWidth).Select(choice => Tuple.Create(initialDepth, choice)))
            {
                searchState.Store(entry);
            }

            while (searchState.TryGetNext(out var next) && !control.ShouldStop(SearchType.Depth))
            {
                var (depth, choice) = next;
                while (next.Item1 < state.ChoicesMade)
                {
                    state.UndoLast();
                }
                state.Apply(choice);
                control.VisitNode();
                var lb = state.LowerBound;
                var qual = state.Quality;

                if (!lb.IsBetter(control.UpperBound) && (bestState != null || !qual.HasValue))
                {
                    continue;
                }
                if (qual.HasValue && (qual.Value.IsBetter(control.UpperBound) || bestState == null))
                {
                    control.FoundSolution(qual.Value);
                    bestState = (T)state.Clone();
                }
                depth = state.ChoicesMade;
                foreach (var entry in state.GetChoices().Take(control.BeamWidth).Select(ch => Tuple.Create(depth, ch)))
                {
                    searchState.Store(entry);
                }
            }
        }
    }
}