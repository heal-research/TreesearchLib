using System;
using System.Linq;

namespace TreesearchLib
{
    public class Searcher
    {
        public static void Search<T, C>(ISearchState<T> searchState, ref T bestState, SearchLimits limits)
        where T : class, ISearchable<C>, ICloneable
        {
            limits.ResetVisitedNodes(searchState.Nodes());
            while (searchState.TryGetNext(out var currentState) && !limits.ShouldStop(searchState.SearchType))
            {

                foreach (var choice in currentState.GetChoices().Take(limits.BeamWidth))
                {
                    var next = (T)currentState.Clone();
                    next.Apply(choice);
                    limits.VisitNode();
                    var lb = next.LowerBound;
                    var qual = next.Quality;

                    if (lb.IsWorseOrEqual(limits.UpperBound) && (bestState != null || !qual.HasValue))
                    {
                        continue;
                    }
                    if (qual.HasValue && (qual.Value.IsBetter(limits.UpperBound) || bestState == null))
                    {
                        limits.FoundSolution(qual.Value);
                        bestState = next;
                    }

                    searchState.Store(next);
                }
            }
        }

        public static void SearchReversible<T, C>(T state, ref T bestState, SearchLimits limits)
        where T : ISearchableReversible<C>, ICloneable
        {
            var searchState = new DFSState<Tuple<int, C>>();
            var initialDepth = state.ChoicesMade;
            foreach (var entry in state.GetChoices().Take(limits.BeamWidth).Select(choice => Tuple.Create(initialDepth, choice)))
            {
                searchState.Store(entry);
            }

            limits.ResetVisitedNodes(0);
            while (searchState.TryGetNext(out var next) && !limits.ShouldStop(SearchType.Depth))
            {
                var (depth, choice) = next;
                while (next.Item1 < state.ChoicesMade)
                {
                    state.UndoLast();
                }
                state.Apply(choice);
                limits.VisitNode();
                var lb = state.LowerBound;
                var qual = state.Quality;

                if (lb.IsWorseOrEqual(limits.UpperBound) && (bestState != null || !qual.HasValue))
                {
                    state.UndoLast(); // should be unnecessary here as this will be handled in the 2nd while loop
                    continue;
                }
                if (qual.HasValue && (qual.Value.IsBetter(limits.UpperBound) || bestState == null))
                {
                    limits.FoundSolution(qual.Value);
                    bestState = (T)state.Clone();
                    state.UndoLast(); // I don't understand this
                }
                depth = state.ChoicesMade;
                foreach (var entry in state.GetChoices().Take(limits.BeamWidth).Select(ch => Tuple.Create(depth, ch)))
                {
                    searchState.Store(entry);
                }
            }
        }
    }
}