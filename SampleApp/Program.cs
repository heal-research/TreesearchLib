using System;
using System.Linq;
using TreesearchLib;

namespace SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            ChooseSmallestProblem();
            KnapsackProblem();
        }

        private static void ChooseSmallestProblem()
        {
            var size = 10;

            // Ability to start with the respective quality and build a configuration, finally call an algorithm
            var control = Minimize.Start(new ChooseSmallestProblem(size)).WithUpperBound(new Minimize(int.MaxValue))
                .WithImprovementCallback((ctrl, state, quality) => Console.WriteLine($"Found new best solution with {quality} after {ctrl.Elapsed}"))
                .DepthFirst();
            Console.WriteLine($"SearchReversible {control.BestQualityState} {control.BestQuality} {control.VisitedNodes}");
            
            var control2 = Minimize.Start(new ChooseSmallestProblem(size).NoUndo()).WithUpperBound(new Minimize(int.MaxValue))
                .WithImprovementCallback((ctrl, state, quality) => Console.WriteLine($"Found new best solution with {quality} after {ctrl.Elapsed}"))
                .DepthFirst();
            Console.WriteLine($"Search {control.BestQualityState} {control.BestQuality} {control.VisitedNodes}");

            // Another quicker way if only the state is of interest is to call the respective algorithm on the state object
            var solution = new ChooseSmallestProblem(size).DepthFirst();
        }

        private static void KnapsackProblem()
        {
            var size = 45;
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

            // The knapsack implementation aims to provide efficient states for reversible search (only DFS), as well as for non-reversible search
            var knapsack = new Knapsack(profits, weights, capacity);
            var knapsackNoUndo = new KnapsackNoUndo(profits, weights, capacity);

            var resultDFS1 = Maximize.Start(knapsack).DepthFirst();
            Console.WriteLine($"DFSearch reversible {resultDFS1.BestQuality} {resultDFS1.VisitedNodes} ({(resultDFS1.VisitedNodes / resultDFS1.Elapsed.TotalSeconds):F2} nodes/sec)");

            var resultDFS2 = Maximize.Start(knapsackNoUndo).DepthFirst();
            Console.WriteLine($"DFSearch non-reversible {resultDFS2.BestQuality} {resultDFS2.VisitedNodes} ({(resultDFS2.VisitedNodes / resultDFS2.Elapsed.TotalSeconds):F2} nodes/sec)");

            var resultBS1 = Maximize.Start(knapsack).BeamSearch(100);
            Console.WriteLine($"BeamSearch wrapped {resultBS1.BestQuality} {resultBS1.VisitedNodes} ({(resultBS1.VisitedNodes / resultBS1.Elapsed.TotalSeconds):F2} nodes/sec)");

            var resultBS2 = Maximize.Start(knapsackNoUndo).BeamSearch(100);
            Console.WriteLine($"BeamSearch non-reversible {resultBS2.BestQuality} {resultBS2.VisitedNodes} ({(resultBS2.VisitedNodes / resultBS2.Elapsed.TotalSeconds):F2} nodes/sec)");

            var resultRBS1 = Maximize.Start(knapsack).RakeAndBeamSearch(100, 100);
            Console.WriteLine($"RakeAndBeamSearch wrapped {resultRBS1.BestQuality} {resultRBS1.VisitedNodes} ({(resultRBS1.VisitedNodes / resultRBS1.Elapsed.TotalSeconds):F2} nodes/sec)");

            var resultRBS2 = Maximize.Start(knapsackNoUndo).RakeAndBeamSearch(100, 100);
            Console.WriteLine($"RakeAndBeamSearch non-reversible {resultRBS2.BestQuality} {resultRBS2.VisitedNodes} ({(resultRBS2.VisitedNodes / resultRBS2.Elapsed.TotalSeconds):F2} nodes/sec)");
        }
    }
}
