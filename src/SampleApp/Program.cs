using System;
using System.Collections.Generic;
using System.Linq;
using TreesearchLib;

namespace SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========= ChooseSmallestProblem =========");
            ChooseSmallestProblem();
            Console.WriteLine("=========    KnapsackProblem    =========");
            KnapsackProblem();
            Console.WriteLine("======= TravelingSalesmanProblem ========");
            TravelingSalesman();
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
            var size = 30;
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

            var resultBS1 = Maximize.Start(knapsack).BeamSearch(10, state => state.Bound.Value, 2);
            Console.WriteLine($"BeamSearch(10) {resultBS1.BestQuality} {resultBS1.VisitedNodes} ({(resultBS1.VisitedNodes / resultBS1.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultBS10 = Maximize.Start(knapsack).BeamSearch(100, state => state.Bound.Value, 2);
            Console.WriteLine($"BeamSearch(100) {resultBS10.BestQuality} {resultBS10.VisitedNodes} ({(resultBS10.VisitedNodes / resultBS10.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultRS1 = Maximize.Start(knapsack).RakeSearch(10);
            Console.WriteLine($"RakeSearch(10) {resultRS1.BestQuality} {resultRS1.VisitedNodes} ({(resultRS1.VisitedNodes / resultRS1.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultRS10 = Maximize.Start(knapsack).RakeSearch(100);
            Console.WriteLine($"RakeSearch(100) {resultRS10.BestQuality} {resultRS10.VisitedNodes} ({(resultRS10.VisitedNodes / resultRS10.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultRBS1 = Maximize.Start(knapsack).RakeAndBeamSearch(10, 10, state => state.Bound.Value, 2);
            Console.WriteLine($"RakeAndBeamSearch(10,10) {resultRBS1.BestQuality} {resultRBS1.VisitedNodes} ({(resultRBS1.VisitedNodes / resultRBS1.Elapsed.TotalSeconds):F2} nodes/sec)");

            var resultRBS10 = Maximize.Start(knapsack).RakeAndBeamSearch(100, 100, state => state.Bound.Value, 2);
            Console.WriteLine($"RakeAndBeamSearch(100,100) {resultRBS10.BestQuality} {resultRBS10.VisitedNodes} ({(resultRBS10.VisitedNodes / resultRBS10.Elapsed.TotalSeconds):F2} nodes/sec)");

            var resultPM = Maximize.Start(knapsack).PilotMethod();
            Console.WriteLine($"Pilot Method {resultPM.BestQuality} {resultPM.VisitedNodes} ({(resultPM.VisitedNodes / resultPM.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultNaiveLD = Maximize.Start(knapsack).NaiveLDSearch(3);
            Console.WriteLine($"NaiveLDSearch(3) {resultNaiveLD.BestQuality} {resultNaiveLD.VisitedNodes} ({(resultNaiveLD.VisitedNodes / resultNaiveLD.Elapsed.TotalSeconds):F2} nodes/sec)");

            var resultAnytimeLD = Maximize.Start(knapsack).AnytimeLDSearch(3);
            Console.WriteLine($"AnytimeLDSearch(3) {resultAnytimeLD.BestQuality} {resultAnytimeLD.VisitedNodes} ({(resultAnytimeLD.VisitedNodes / resultAnytimeLD.Elapsed.TotalSeconds):F2} nodes/sec)");

            var resultDFS1 = Maximize.Start(knapsack).DepthFirst();
            Console.WriteLine($"DFSearch reversible {resultDFS1.BestQuality} {resultDFS1.VisitedNodes} ({(resultDFS1.VisitedNodes / resultDFS1.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var knapsackNoUndo = new KnapsackNoUndo(profits, weights, capacity);

            var resultMonoBS1 = Maximize.Start(knapsackNoUndo).MonotonicBeamSearch(beamWidth: 1, rank: ksp => -ksp.Bound.Value);
            Console.WriteLine($"MonoBeam(1) {resultMonoBS1.BestQuality} {resultMonoBS1.VisitedNodes} ({(resultMonoBS1.VisitedNodes / resultMonoBS1.Elapsed.TotalSeconds):F2} nodes/sec)");
            var resultMonoBS2 = Maximize.Start(knapsackNoUndo).MonotonicBeamSearch(beamWidth: 2, rank: ksp => -ksp.Bound.Value);
            Console.WriteLine($"MonoBeam(2) {resultMonoBS2.BestQuality} {resultMonoBS2.VisitedNodes} ({(resultMonoBS2.VisitedNodes / resultMonoBS2.Elapsed.TotalSeconds):F2} nodes/sec)");
            var resultMonoBS5 = Maximize.Start(knapsackNoUndo).MonotonicBeamSearch(beamWidth: 5, rank: ksp => -ksp.Bound.Value);
            Console.WriteLine($"MonoBeam(5) {resultMonoBS5.BestQuality} {resultMonoBS5.VisitedNodes} ({(resultMonoBS5.VisitedNodes / resultMonoBS5.Elapsed.TotalSeconds):F2} nodes/sec)");
            var resultMonoBS10 = Maximize.Start(knapsackNoUndo).MonotonicBeamSearch(beamWidth: 10, rank: ksp => -ksp.Bound.Value);
            Console.WriteLine($"MonoBeam(10) {resultMonoBS10.BestQuality} {resultMonoBS10.VisitedNodes} ({(resultMonoBS10.VisitedNodes / resultMonoBS10.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultDFS2 = Maximize.Start(knapsackNoUndo).DepthFirst();
            Console.WriteLine($"DFSearch non-reversible {resultDFS2.BestQuality} {resultDFS2.VisitedNodes} ({(resultDFS2.VisitedNodes / resultDFS2.Elapsed.TotalSeconds):F2} nodes/sec)");
        }

        private static void TravelingSalesman()
        {
            var tsp = new TSP(Berlin52.GetDistances());
            
            var resultdf1 = Minimize.Start(tsp).DepthFirst(1);
            Console.WriteLine($"DepthFirst(1) {resultdf1.BestQuality} {resultdf1.VisitedNodes} ({(resultdf1.VisitedNodes / resultdf1.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultMonoBS1 = Minimize.Start(tsp).MonotonicBeamSearch(1, rank: t => t.Bound.Value, filterWidth: 3);
            Console.WriteLine($"MonoBeamSearch(1,3) {resultMonoBS1.BestQuality} {resultMonoBS1.VisitedNodes} ({(resultMonoBS1.VisitedNodes / resultMonoBS1.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultMonoBS10 = Minimize.Start(tsp).MonotonicBeamSearch(10, rank: t => t.Bound.Value, filterWidth: 3);
            Console.WriteLine($"MonoBeamSearch(10,3) {resultMonoBS10.BestQuality} {resultMonoBS10.VisitedNodes} ({(resultMonoBS10.VisitedNodes / resultMonoBS10.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultMonoBS100 = Minimize.Start(tsp).MonotonicBeamSearch(100, rank: t => t.Bound.Value, filterWidth: 3);
            Console.WriteLine($"MonoBeamSearch(100,3) {resultMonoBS100.BestQuality} {resultMonoBS100.VisitedNodes} ({(resultMonoBS100.VisitedNodes / resultMonoBS100.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultBS1 = Minimize.Start(tsp).BeamSearch(1, state => state.Bound.Value, 3);
            Console.WriteLine($"BeamSearch(1,3) {resultBS1.BestQuality} {resultBS1.VisitedNodes} ({(resultBS1.VisitedNodes / resultBS1.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultBS10 = Minimize.Start(tsp).BeamSearch(10, state => state.Bound.Value, 3);
            Console.WriteLine($"BeamSearch(10,3) {resultBS10.BestQuality} {resultBS10.VisitedNodes} ({(resultBS10.VisitedNodes / resultBS10.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultBS100 = Minimize.Start(tsp).BeamSearch(100, state => state.Bound.Value, 3);
            Console.WriteLine($"BeamSearch(100,3) {resultBS100.BestQuality} {resultBS100.VisitedNodes} ({(resultBS100.VisitedNodes / resultBS100.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultRS100 = Minimize.Start(tsp).RakeSearch(100);
            Console.WriteLine($"RakeSearch(100) {resultRS100.BestQuality} {resultRS100.VisitedNodes} ({(resultRS100.VisitedNodes / resultRS100.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultPM = Minimize.Start(tsp).PilotMethod(rank: state => state.Bound.Value);
            Console.WriteLine($"Pilot Method {resultPM.BestQuality} {resultPM.VisitedNodes} ({(resultPM.VisitedNodes / resultPM.Elapsed.TotalSeconds):F2} nodes/sec)");
            
            var resultAnytimeLD = Minimize.Start(tsp).AnytimeLDSearch(3);
            Console.WriteLine($"AnytimeLDSearch(3) {resultAnytimeLD.BestQuality} {resultAnytimeLD.VisitedNodes} ({(resultAnytimeLD.VisitedNodes / resultAnytimeLD.Elapsed.TotalSeconds):F2} nodes/sec)");
        }
    }
}
