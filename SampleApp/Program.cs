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
            SearchLimits<Minimize>.WithUpperBound(new Minimize(int.MaxValue))
                .SearchWithUndo<ChooseSmallestProblem, int>(new ChooseSmallestProblem(size), ref best);
            Console.WriteLine($"SearchReversible {best} {best.Quality}");
            best = null;
            SearchLimits<Minimize>.WithUpperBound(new Minimize(int.MaxValue))
                .SearchDepthFirst<ChooseSmallestProblem, int>(new ChooseSmallestProblem(size), ref best);
            Console.WriteLine($"Search {best} {best.Quality}");
        }
    }
}
