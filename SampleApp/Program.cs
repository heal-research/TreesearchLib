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
            var control2 = Minimize.Start(new ChooseSmallestProblem(size).NoUndo()).WithUpperBound(new Minimize(int.MaxValue))
                .WithImprovementCallback((ctrl, state, quality) => Console.WriteLine($"Found new best solution with {quality} after {ctrl.Elapsed}"))
                .DepthFirst();
            Console.WriteLine($"Search {control.BestQualityState} {control.BestQuality} {control.VisitedNodes}");

            size = 40;
            var random = new Random(13);
            var profits = Enumerable.Range(0, size).Select(x => random.Next(1, 100)).ToArray();
            var weights = Enumerable.Range(0, size).Select(x => random.Next(1, 100)).ToArray();
            var capacity = (int)Math.Round(weights.Sum() / 2.0);
            // sort items by how desirable they are in terms of profit / weight ratio
            var profitability = Enumerable.Range(0, size).Select(x => (item: x, ratio: profits[x] / (double)weights[x]))
                .OrderByDescending(x => x.ratio).ToArray();
            var sortKey = new int[size];
            for (var i = 0; i < size; i++)
                sortKey[profitability[i].item] = i;
            Array.Sort(sortKey.ToArray(), profits);
            Array.Sort(sortKey.ToArray(), weights);

            var knapsack = new Knapsack()
            {
                Profits = profits,
                Weights = weights,
                Capacity = capacity
            };
            
            var result = Maximize.Start(knapsack).WithImprovementCallback((ctrl, state, quality) => Console.WriteLine($"Found solution with {quality} after {ctrl.Elapsed}"))
                .DepthFirst();
            Console.WriteLine($"DFSearch {result.BestQualityState} {result.BestQuality} {result.VisitedNodes} ({(result.VisitedNodes / result.Elapsed.TotalSeconds):F2} nodes/sec)");

            var result2 = Maximize.Start(knapsack.NoUndo()).WithImprovementCallback((ctrl, state, quality) => Console.WriteLine($"Found solution with {quality} after {ctrl.Elapsed}"))
                .BeamSearch(100);
            Console.WriteLine($"BeamSearch {result2.BestQualityState} {result2.BestQuality} {result2.VisitedNodes} ({(result2.VisitedNodes / result2.Elapsed.TotalSeconds):F2} nodes/sec)");

            var result3 = Maximize.Start(knapsack.NoUndo()).WithImprovementCallback((ctrl, state, quality) => Console.WriteLine($"Found solution with {quality} after {ctrl.Elapsed}"))
                .RakeAndBeamSearch(100, 100);
            Console.WriteLine($"RakeAndBeamSearch {result3.BestQualityState} {result3.BestQuality} {result3.VisitedNodes} ({(result3.VisitedNodes / result3.Elapsed.TotalSeconds):F2} nodes/sec)");
        }
    }
}
