using System;
using System.Collections.Generic;
using System.Linq;

namespace Treesearch
{

    public class Searcher
    {
        public static void Search<T, C>(ISearchState<T> searchState, ref T bestSolution, SearchLimits limits)
        where T : class, ISearchable<C>, ICloneable
        {
            var choices = new List<C>();
            limits.ResetVisitedNodes(searchState.Nodes());
            while (true)
            {
                if (!searchState.TryGetNext(out var currentState)) break;
                if (limits.ShouldStop(searchState.SearchType))
                {
                    break;
                }
                var noChoice = true;
                currentState.FillChoices(choices);

                foreach (var choice in choices.Take(Math.Min(choices.Count, limits.BeamWidth)))
                {
                    var next = (T)currentState.Clone();
                    next.Apply(choice);
                    limits.VisitNode();
                    noChoice = false;
                    var lb = next.LowerBound;

                    if (lb.IsWorseOrEqual(limits.UpperBound) && bestSolution != null)
                    {
                        continue;
                    }
                    var qual = next.Quality;
                    if (qual.HasValue)
                    {
                        if (qual.Value.IsBetter(limits.UpperBound) || bestSolution == null)
                        {
                            limits.FoundSolution(qual.Value);
                            bestSolution = next;
                        }
                        if (next.ConstructOrChange() == ChoiceType.Construct)
                        {
                            continue;
                        }
                    }

                    searchState.Store(next);
                }
                if (noChoice)
                {
                    Console.WriteLine($"no_choice after: {searchState.Nodes()}");
                }
            }
        }

        public static void SearchReversible<T, C>(T state, ref T bestSolution, SearchLimits limits)
        where T : ISearchableReversible<C>, ICloneable
        {
            var searchState = new DFSState<Tuple<int, C>>();
            var choices = new List<C>();
            state.FillChoices(choices);
            var initialDepth = state.ChoicesMade;
            foreach (var entry in choices.Take(Math.Min(choices.Count, limits.BeamWidth)).Select(choice => Tuple.Create(initialDepth, choice)))
            {
                searchState.Store(entry);
            }

            limits.ResetVisitedNodes(0);
            while (searchState.TryGetNext(out var next))
            {
                var (depth, choice) = next;
                if (limits.ShouldStop(SearchType.Depth)) break;
                while (next.Item1 < state.ChoicesMade)
                {
                    state.UndoLast();
                }
                state.Apply(choice);
                limits.VisitNode();
                var lb = state.LowerBound;
                if (lb.IsWorseOrEqual(limits.UpperBound) && bestSolution != null)
                {
                    state.UndoLast();
                    continue;
                }
                var qual = state.Quality;
                if (qual.HasValue)
                {
                    if (qual.Value.IsBetter(limits.UpperBound) || bestSolution == null)
                    {
                        limits.FoundSolution(qual.Value);
                        bestSolution = (T)state.Clone();
                        state.UndoLast();
                    }
                }
                depth = state.ChoicesMade;
                state.FillChoices(choices);
                foreach (var entry in choices.Take(Math.Min(choices.Count, limits.BeamWidth)).Select(ch => Tuple.Create(depth, ch)))
                {
                    searchState.Store(entry);
                }
            }
        }
    }
}