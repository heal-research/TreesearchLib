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
            var pmax = 100;
            var pmin = 1;
            var capfactor = 0.5;
            var random = new Random(13);
            var profits = Enumerable.Range(0, size).Select(x => random.Next(pmin, pmax)).ToArray();
            var weights = Enumerable.Range(0, size).Select(x => random.Next(1, 100)).ToArray();
            var capacity = (int)Math.Round(weights.Sum() * capfactor);
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

            var resultBS1 = Maximize.Start(knapsack).BeamSearch(10);
            Console.WriteLine($"BeamSearch(10) {resultBS1.BestQuality} {resultBS1.VisitedNodes} ({(resultBS1.VisitedNodes / resultBS1.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultBS10 = Maximize.Start(knapsack).BeamSearch(100);
            Console.WriteLine($"BeamSearch(100) {resultBS10.BestQuality} {resultBS10.VisitedNodes} ({(resultBS10.VisitedNodes / resultBS10.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultRS1 = Maximize.Start(knapsack).RakeSearch(10);
            Console.WriteLine($"RakeSearch(10) {resultRS1.BestQuality} {resultRS1.VisitedNodes} ({(resultRS1.VisitedNodes / resultRS1.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultRS10 = Maximize.Start(knapsack).RakeSearch(100);
            Console.WriteLine($"RakeSearch(100) {resultRS10.BestQuality} {resultRS10.VisitedNodes} ({(resultRS10.VisitedNodes / resultRS10.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultRBS1 = Maximize.Start(knapsack).RakeAndBeamSearch(10, 10);
            Console.WriteLine($"RakeAndBeamSearch(10,10) {resultRBS1.BestQuality} {resultRBS1.VisitedNodes} ({(resultRBS1.VisitedNodes / resultRBS1.Elapsed.TotalSeconds):F2} nodes/sec)");

            var resultRBS10 = Maximize.Start(knapsack).RakeAndBeamSearch(100, 100);
            Console.WriteLine($"RakeAndBeamSearch(100,100) {resultRBS10.BestQuality} {resultRBS10.VisitedNodes} ({(resultRBS10.VisitedNodes / resultRBS10.Elapsed.TotalSeconds):F2} nodes/sec)");

            var resultPM = Maximize.Start(knapsack).PilotMethod();
            Console.WriteLine($"Pilot Method {resultPM.BestQuality} {resultPM.VisitedNodes} ({(resultPM.VisitedNodes / resultPM.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultNaiveLD = Maximize.Start(knapsack).NaiveLDSearch(3);
            Console.WriteLine($"NaiveLDSearch(3) {resultNaiveLD.BestQuality} {resultNaiveLD.VisitedNodes} ({(resultNaiveLD.VisitedNodes / resultNaiveLD.Elapsed.TotalSeconds):F2} nodes/sec)");

            var resultAnytimeLD = Maximize.Start(knapsack).AnytimeLDSearch(3);
            Console.WriteLine($"AnytimeLDSearch(3) {resultAnytimeLD.BestQuality} {resultAnytimeLD.VisitedNodes} ({(resultAnytimeLD.VisitedNodes / resultAnytimeLD.Elapsed.TotalSeconds):F2} nodes/sec)");

            var resultDFS1 = Maximize.Start(knapsack).DepthFirst();
            Console.WriteLine($"DFSearch reversible {resultDFS1.BestQuality} {resultDFS1.VisitedNodes} ({(resultDFS1.VisitedNodes / resultDFS1.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultDFS2 = Maximize.Start(knapsackNoUndo).DepthFirst();
            Console.WriteLine($"DFSearch non-reversible {resultDFS2.BestQuality} {resultDFS2.VisitedNodes} ({(resultDFS2.VisitedNodes / resultDFS2.Elapsed.TotalSeconds):F2} nodes/sec)");

            var resultMCTS = Maximize.Start(knapsack).WithNodeLimit(resultDFS1.VisitedNodes)
                .WithRuntimeLimit(resultDFS1.Elapsed);
            MonteCarloTreeSearch<Knapsack, bool, Maximize>.Search(resultMCTS, (node, state) => node.Score += state.Quality.Value.Value);
            Console.WriteLine($"MCTS reversible with {resultMCTS.VisitedNodes} nodes {resultMCTS.BestQuality} {resultMCTS.VisitedNodes} ({(resultMCTS.VisitedNodes / resultMCTS.Elapsed.TotalSeconds):F2} nodes/sec)");
        }
    }
}
