using System;
using TreesearchLib;

namespace SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var size = 35;

            ChooseSmallestProblem best = null;
            var limits = new SearchLimits();
            Searcher.SearchReversible<ChooseSmallestProblem, int>(new ChooseSmallestProblem(size), ref best, limits);
            Console.WriteLine($"SearchReversible {best} {best.Quality}");
            best = null;
            var state = new DFSState<ChooseSmallestProblem>();
            state.Store(new ChooseSmallestProblem(size));
            limits = new SearchLimits();
            Searcher.Search<ChooseSmallestProblem, int>(state, ref best, limits);
            Console.WriteLine($"Search {best} {best.Quality}");
        }
    }
}
