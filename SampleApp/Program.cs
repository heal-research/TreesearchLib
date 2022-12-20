using System;
using System.Linq;
using TreesearchLib;

namespace SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var size = 10;

            var control = Minimize.Start(new ChooseSmallestProblem(size)).WithUpperBound(new Minimize(int.MaxValue))
                .WithImprovementCallback((ctrl, state, quality) => Console.WriteLine($"Found new best solution with {quality} after {ctrl.Elapsed}"))
                .DepthFirst();
            Console.WriteLine($"SearchReversible {control.BestQualityState} {control.BestQuality} {control.VisitedNodes}");
            var control2 = Minimize.Start(new ChooseSmallestProblem(size).Wrap()).WithUpperBound(new Minimize(int.MaxValue))
                .WithImprovementCallback((ctrl, state, quality) => Console.WriteLine($"Found new best solution with {quality} after {ctrl.Elapsed}"))
                .DepthFirst();
            Console.WriteLine($"Search {control.BestQualityState} {control.BestQuality} {control.VisitedNodes}");

            size = 100;
            var random = new Random(13);
            var profits = Enumerable.Range(0, size).Select(x => random.Next(1, 100)).ToArray();
            var weights = Enumerable.Range(0, size).Select(x => random.Next(1, 100)).ToArray();
            var capacity = (int)Math.Round(weights.Sum() / 2.0);

            var knapsack = new Knapsack()
            {
                Profits = profits,
                Weights = weights,
                Capacity = capacity
            };

            var result = Maximize.Start(knapsack).WithImprovementCallback((ctrl, state, quality) => Console.WriteLine($"Found solution with {quality} after {ctrl.Elapsed}"))
                .DepthFirst();
            Console.WriteLine($"Search {result.BestQualityState} {result.BestQuality} {result.VisitedNodes}");
        }
    }
}
