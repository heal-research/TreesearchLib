using System;
using TreesearchLib;

namespace SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var size = 10;

            ChooseSmallestProblem best = null;
            var control = SearchControl<Minimize>.Start().WithUpperBound(new Minimize(int.MaxValue))
                .WithImprovementCallback((ctrl, quality) => Console.WriteLine($"Found new best solution with {quality} after {ctrl.Elapsed}"))
                .SearchDepthFirst<ChooseSmallestProblem, int>(new ChooseSmallestProblem(size), ref best);
            Console.WriteLine($"SearchReversible {best} {best.Quality} {control.TotalNodesVisited}");
            best = null;
            SearchControl<Minimize>.Start().WithUpperBound(new Minimize(int.MaxValue))
                .WithImprovementCallback((ctrl, quality) => Console.WriteLine($"Found new best solution with {quality} after {ctrl.Elapsed}"))
                .SearchDepthFirst<ChooseSmallestProblem, int>(new ChooseSmallestProblem(size), ref best, utilizeUndo: false);
            Console.WriteLine($"Search {best} {best.Quality} {control.TotalNodesVisited}");
        }
    }
}
