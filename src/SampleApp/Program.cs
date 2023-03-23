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
            Console.WriteLine("========= SchedulingProblem =============");
            SchedulingProblem();
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

            var resultBS10 = Maximize.Start(knapsack).BeamSearch(10, state => -state.Bound.Value);
            Console.WriteLine($"{"BeamSearch(10)",55} {resultBS10.BestQuality,12} {resultBS10.VisitedNodes,6} ({(resultBS10.VisitedNodes / resultBS10.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParBS1 = Maximize.Start(knapsack).ParallelBeamSearch(10, state => -state.Bound.Value);
            Console.WriteLine($"{"ParallelBeamSearch(10)",55} {resultParBS1.BestQuality,12} {resultParBS1.VisitedNodes,6} ({(resultParBS1.VisitedNodes / resultParBS1.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultBS100 = Maximize.Start(knapsack).BeamSearch(100, state => -state.Bound.Value);
            Console.WriteLine($"{"BeamSearch(100)",55} {resultBS10.BestQuality,12} {resultBS10.VisitedNodes,6} ({(resultBS10.VisitedNodes / resultBS10.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParBS10 = Maximize.Start(knapsack).ParallelBeamSearch(100, state => -state.Bound.Value);
            Console.WriteLine($"{"ParallelBeamSearch(100)",55} {resultParBS10.BestQuality,12} {resultParBS10.VisitedNodes,6} ({(resultParBS10.VisitedNodes / resultParBS10.Elapsed.TotalSeconds),12:F2} nodes/sec)");

            var resultMonoBS1 = Maximize.Start(knapsack).MonotonicBeamSearch(beamWidth: 1, rank: ksp => -ksp.Bound.Value);
            Console.WriteLine($"{"MonoBeam(1)",55} {resultMonoBS1.BestQuality,12} {resultMonoBS1.VisitedNodes,6} ({(resultMonoBS1.VisitedNodes / resultMonoBS1.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultMonoBS2 = Maximize.Start(knapsack).MonotonicBeamSearch(beamWidth: 2, rank: ksp => -ksp.Bound.Value);
            Console.WriteLine($"{"MonoBeam(2)",55} {resultMonoBS2.BestQuality,12} {resultMonoBS2.VisitedNodes,6} ({(resultMonoBS2.VisitedNodes / resultMonoBS2.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultMonoBS5 = Maximize.Start(knapsack).MonotonicBeamSearch(beamWidth: 5, rank: ksp => -ksp.Bound.Value);
            Console.WriteLine($"{"MonoBeam(5)",55} {resultMonoBS5.BestQuality,12} {resultMonoBS5.VisitedNodes,6} ({(resultMonoBS5.VisitedNodes / resultMonoBS5.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultMonoBS10 = Maximize.Start(knapsack).MonotonicBeamSearch(beamWidth: 10, rank: ksp => -ksp.Bound.Value);
            Console.WriteLine($"{"MonoBeam(10)",55} {resultMonoBS10.BestQuality,12} {resultMonoBS10.VisitedNodes,6} ({(resultMonoBS10.VisitedNodes / resultMonoBS10.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            
            var resultRS10 = Maximize.Start(knapsack).RakeSearch(10);
            Console.WriteLine($"{"RakeSearch(10)",55} {resultRS10.BestQuality,12} {resultRS10.VisitedNodes,6} ({(resultRS10.VisitedNodes / resultRS10.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParRS10 = Maximize.Start(knapsack).ParallelRakeSearch(10);
            Console.WriteLine($"{"ParallelRakeSearch(10)",55} {resultParRS10.BestQuality,12} {resultParRS10.VisitedNodes,6} ({(resultParRS10.VisitedNodes / resultParRS10.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultRS100 = Maximize.Start(knapsack).RakeSearch(100);
            Console.WriteLine($"{"RakeSearch(100)",55} {resultRS100.BestQuality,12} {resultRS100.VisitedNodes,6} ({(resultRS100.VisitedNodes / resultRS100.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParRS100 = Maximize.Start(knapsack).ParallelRakeSearch(100);
            Console.WriteLine($"{"ParallelRakeSearch(100)",55} {resultParRS100.BestQuality,12} {resultParRS100.VisitedNodes,6} ({(resultParRS100.VisitedNodes / resultParRS100.Elapsed.TotalSeconds),12:F2} nodes/sec)");

            var resultRBS1010 = Maximize.Start(knapsack).RakeAndBeamSearch(10, 10, state => -state.Bound.Value);
            Console.WriteLine($"{"RakeAndBeamSearch(10,10)",55} {resultRBS1010.BestQuality,12} {resultRBS1010.VisitedNodes,6} ({(resultRBS1010.VisitedNodes / resultRBS1010.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParRBS1010 = Maximize.Start(knapsack).ParallelRakeAndBeamSearch(10, 10, state => -state.Bound.Value);
            Console.WriteLine($"{"ParallelRakeAndBeamSearch(10,10)",55} {resultParRBS1010.BestQuality,12} {resultParRBS1010.VisitedNodes,6} ({(resultParRBS1010.VisitedNodes / resultParRBS1010.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultRBS100100 = Maximize.Start(knapsack).RakeAndBeamSearch(100, 100, state => -state.Bound.Value);
            Console.WriteLine($"{"RakeAndBeamSearch(100,100)",55} {resultRBS100100.BestQuality,12} {resultRBS100100.VisitedNodes,6} ({(resultRBS100100.VisitedNodes / resultRBS100100.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParRBS100100 = Maximize.Start(knapsack).ParallelRakeAndBeamSearch(100, 100, state => -state.Bound.Value);
            Console.WriteLine($"{"ParallelRakeAndBeamSearch(100,100)",55} {resultParRBS100100.BestQuality,12} {resultParRBS100100.VisitedNodes,6} ({(resultParRBS100100.VisitedNodes / resultParRBS100100.Elapsed.TotalSeconds),12:F2} nodes/sec)");

            var resultPM = Maximize.Start(knapsack).PilotMethod();
            Console.WriteLine($"{"Pilot Method",55} {resultPM.BestQuality,12} {resultPM.VisitedNodes,6} ({(resultPM.VisitedNodes / resultPM.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParPM = Maximize.Start(knapsack).ParallelPilotMethod();
            Console.WriteLine($"{"Parallel Pilot Method",55} {resultParPM.BestQuality,12} {resultParPM.VisitedNodes,6} ({(resultParPM.VisitedNodes / resultParPM.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultPMBS10 = Maximize.Start(knapsack).PilotMethod(beamWidth: 10, rank: ksp => -ksp.Bound.Value, filterWidth: int.MaxValue);
            Console.WriteLine($"{"Pilot Method with Beam Search(10)",55} {resultPMBS10.BestQuality,12} {resultPMBS10.VisitedNodes,6} ({(resultPMBS10.VisitedNodes / resultPMBS10.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParPMBS10 = Maximize.Start(knapsack).ParallelPilotMethod(beamWidth: 10, rank: ksp => -ksp.Bound.Value, filterWidth: int.MaxValue);
            Console.WriteLine($"{"Parallel Pilot Method with Beam Search(10)",55} {resultParPMBS10.BestQuality,12} {resultParPMBS10.VisitedNodes,6} ({(resultParPMBS10.VisitedNodes / resultParPMBS10.Elapsed.TotalSeconds),12:F2} nodes/sec)");

            var resultNaiveLD = Maximize.Start(knapsack).NaiveLDSearch(3);
            Console.WriteLine($"{"NaiveLDSearch(3)",55} {resultNaiveLD.BestQuality,12} {resultNaiveLD.VisitedNodes,6} ({(resultNaiveLD.VisitedNodes / resultNaiveLD.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultAnytimeLD = Maximize.Start(knapsack).AnytimeLDSearch(3);
            Console.WriteLine($"{"AnytimeLDSearch(3)",55} {resultAnytimeLD.BestQuality,12} {resultAnytimeLD.VisitedNodes,6} ({(resultAnytimeLD.VisitedNodes / resultAnytimeLD.Elapsed.TotalSeconds),12:F2} nodes/sec)");

            var resultDFS1 = Maximize.Start(knapsack).DepthFirst();
            Console.WriteLine($"{"DFSearch reversible",55} {resultDFS1.BestQuality,12} {resultDFS1.VisitedNodes,6} ({(resultDFS1.VisitedNodes / resultDFS1.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParDFS1 = Maximize.Start(knapsack).ParallelDepthFirst();
            Console.WriteLine($"{"Parallel DFSearch reversible",55} {resultParDFS1.BestQuality,12} {resultParDFS1.VisitedNodes,6} ({(resultParDFS1.VisitedNodes / resultParDFS1.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultBFS = Maximize.Start(knapsack).BreadthFirst();
            Console.WriteLine($"{"BFSearch reversible",55} {resultBFS.BestQuality,12} {resultBFS.VisitedNodes,6} ({(resultBFS.VisitedNodes / resultBFS.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParBFS = Maximize.Start(knapsack).ParallelBreadthFirst();
            Console.WriteLine($"{"Parallel BFSearch reversible",55} {resultParBFS.BestQuality,12} {resultParBFS.VisitedNodes,6} ({(resultParBFS.VisitedNodes / resultParBFS.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            
            var knapsackNoUndo = new KnapsackNoUndo(profits, weights, capacity);
            
            var resultNoUndoBS10 = Maximize.Start(knapsackNoUndo).BeamSearch(10, state => -state.Bound.Value);
            Console.WriteLine($"{"BeamSearch(10) non-reversible",55} {resultNoUndoBS10.BestQuality,12} {resultNoUndoBS10.VisitedNodes,6} ({(resultNoUndoBS10.VisitedNodes / resultNoUndoBS10.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParNoUndoBS10 = Maximize.Start(knapsackNoUndo).ParallelBeamSearch(10, state => -state.Bound.Value);
            Console.WriteLine($"{"Parallel BeamSearch(10) non-reversible",55} {resultParNoUndoBS10.BestQuality,12} {resultParNoUndoBS10.VisitedNodes,6} ({(resultParNoUndoBS10.VisitedNodes / resultParNoUndoBS10.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultNoUndoBS100 = Maximize.Start(knapsackNoUndo).BeamSearch(100, state => -state.Bound.Value);
            Console.WriteLine($"{"BeamSearch(100) non-reversible",55} {resultNoUndoBS100.BestQuality,12} {resultNoUndoBS100.VisitedNodes,6} ({(resultNoUndoBS100.VisitedNodes / resultNoUndoBS100.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParNoUndoBS100 = Maximize.Start(knapsackNoUndo).ParallelBeamSearch(100, state => -state.Bound.Value);
            Console.WriteLine($"{"Parallel BeamSearch(100) non-reversible",55} {resultParNoUndoBS100.BestQuality,12} {resultParNoUndoBS100.VisitedNodes,6} ({(resultParNoUndoBS100.VisitedNodes / resultParNoUndoBS100.Elapsed.TotalSeconds),12:F2} nodes/sec)");

            var resultParMonoBS1 = Maximize.Start(knapsack).MonotonicBeamSearch(beamWidth: 1, rank: ksp => -ksp.Bound.Value);
            Console.WriteLine($"{"MonoBeam(1)",55} {resultParMonoBS1.BestQuality,12} {resultParMonoBS1.VisitedNodes,6} ({(resultParMonoBS1.VisitedNodes / resultParMonoBS1.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParMonoBS2 = Maximize.Start(knapsack).MonotonicBeamSearch(beamWidth: 2, rank: ksp => -ksp.Bound.Value);
            Console.WriteLine($"{"MonoBeam(2)",55} {resultParMonoBS2.BestQuality,12} {resultParMonoBS2.VisitedNodes,6} ({(resultParMonoBS2.VisitedNodes / resultParMonoBS2.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParMonoBS5 = Maximize.Start(knapsack).MonotonicBeamSearch(beamWidth: 5, rank: ksp => -ksp.Bound.Value);
            Console.WriteLine($"{"MonoBeam(5)",55} {resultParMonoBS5.BestQuality,12} {resultParMonoBS5.VisitedNodes,6} ({(resultParMonoBS5.VisitedNodes / resultParMonoBS5.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParMonoBS10 = Maximize.Start(knapsack).MonotonicBeamSearch(beamWidth: 10, rank: ksp => -ksp.Bound.Value);
            Console.WriteLine($"{"MonoBeam(10)",55} {resultParMonoBS10.BestQuality,12} {resultParMonoBS10.VisitedNodes,6} ({(resultParMonoBS10.VisitedNodes / resultParMonoBS10.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            
            var resultNoUndoRS10 = Maximize.Start(knapsackNoUndo).RakeSearch(10);
            Console.WriteLine($"{"RakeSearch(10) non-reversible",55} {resultNoUndoRS10.BestQuality,12} {resultNoUndoRS10.VisitedNodes,6} ({(resultNoUndoRS10.VisitedNodes / resultNoUndoRS10.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParNoUndoRS10 = Maximize.Start(knapsackNoUndo).ParallelRakeSearch(10);
            Console.WriteLine($"{"Parallel RakeSearch(10) non-reversible",55} {resultParNoUndoRS10.BestQuality,12} {resultParNoUndoRS10.VisitedNodes,6} ({(resultParNoUndoRS10.VisitedNodes / resultParNoUndoRS10.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultNoUndoRS100 = Maximize.Start(knapsackNoUndo).RakeSearch(100);
            Console.WriteLine($"{"RakeSearch(100) non-reversible",55} {resultNoUndoRS100.BestQuality,12} {resultNoUndoRS100.VisitedNodes,6} ({(resultNoUndoRS100.VisitedNodes / resultNoUndoRS100.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParNoUndoRS100 = Maximize.Start(knapsackNoUndo).ParallelRakeSearch(100);
            Console.WriteLine($"{"Parallel RakeSearch(100) non-reversible",55} {resultParNoUndoRS100.BestQuality,12} {resultParNoUndoRS100.VisitedNodes,6} ({(resultParNoUndoRS100.VisitedNodes / resultParNoUndoRS100.Elapsed.TotalSeconds),12:F2} nodes/sec)");

            var resultNoUndoRBS1010 = Maximize.Start(knapsackNoUndo).RakeAndBeamSearch(10, 10, state => -state.Bound.Value);
            Console.WriteLine($"{"RakeAndBeamSearch(10,10) non-reversible",55} {resultNoUndoRBS1010.BestQuality,12} {resultNoUndoRBS1010.VisitedNodes,6} ({(resultNoUndoRBS1010.VisitedNodes / resultNoUndoRBS1010.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParNoUndoRBS1010 = Maximize.Start(knapsackNoUndo).ParallelRakeAndBeamSearch(10, 10, state => -state.Bound.Value);
            Console.WriteLine($"{"Parallel RakeAndBeamSearch(10,10) non-reversible",55} {resultParNoUndoRBS1010.BestQuality,12} {resultParNoUndoRBS1010.VisitedNodes,6} ({(resultParNoUndoRBS1010.VisitedNodes / resultParNoUndoRBS1010.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultNoUndoRBS100100 = Maximize.Start(knapsackNoUndo).RakeAndBeamSearch(100, 100, state => -state.Bound.Value);
            Console.WriteLine($"{"RakeAndBeamSearch(100,100) non-reversible",55} {resultNoUndoRBS100100.BestQuality,12} {resultNoUndoRBS100100.VisitedNodes,6} ({(resultNoUndoRBS100100.VisitedNodes / resultNoUndoRBS100100.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParNoUndoRBS100100 = Maximize.Start(knapsackNoUndo).ParallelRakeAndBeamSearch(100, 100, state => -state.Bound.Value);
            Console.WriteLine($"{"Parallel RakeAndBeamSearch(100,100) non-reversible",55} {resultParNoUndoRBS100100.BestQuality,12} {resultParNoUndoRBS100100.VisitedNodes,6} ({(resultParNoUndoRBS100100.VisitedNodes / resultParNoUndoRBS100100.Elapsed.TotalSeconds),12:F2} nodes/sec)");

            var resultNoUndoPM = Maximize.Start(knapsackNoUndo).PilotMethod();
            Console.WriteLine($"{"PilotMethod non-reversible",55} {resultNoUndoPM.BestQuality,12} {resultNoUndoPM.VisitedNodes,6} ({(resultNoUndoPM.VisitedNodes / resultNoUndoPM.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParNoUndoPM = Maximize.Start(knapsackNoUndo).ParallelPilotMethod();
            Console.WriteLine($"{"Parallel PilotMethod non-reversible",55} {resultParNoUndoPM.BestQuality,12} {resultParNoUndoPM.VisitedNodes,6} ({(resultParNoUndoPM.VisitedNodes / resultParNoUndoPM.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultNoUndoPMBS10 = Maximize.Start(knapsackNoUndo).PilotMethod(10, state => -state.Bound.Value, filterWidth: int.MaxValue);
            Console.WriteLine($"{"PilotMethod with BeamSearch(10) non-reversible",55} {resultNoUndoPMBS10.BestQuality,12} {resultNoUndoPMBS10.VisitedNodes,6} ({(resultNoUndoPMBS10.VisitedNodes / resultNoUndoPMBS10.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParNoUndoPMBS10 = Maximize.Start(knapsackNoUndo).ParallelPilotMethod(10, state => -state.Bound.Value, filterWidth: int.MaxValue);
            Console.WriteLine($"{"Parallel PilotMethod with BeamSearch(10) non-reversible",55} {resultParNoUndoPMBS10.BestQuality,12} {resultParNoUndoPMBS10.VisitedNodes,6} ({(resultParNoUndoPMBS10.VisitedNodes / resultParNoUndoPMBS10.Elapsed.TotalSeconds),12:F2} nodes/sec)");

            var resultNoUndoLD = Maximize.Start(knapsackNoUndo).NaiveLDSearch(3);
            Console.WriteLine($"{"NaiveLDSearch(3) non-reversible",55} {resultNoUndoLD.BestQuality,12} {resultNoUndoLD.VisitedNodes,6} ({(resultNoUndoLD.VisitedNodes / resultNoUndoLD.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultNoUndoALD = Maximize.Start(knapsackNoUndo).AnytimeLDSearch(3);
            Console.WriteLine($"{"AnytimeLDSearch(3) non-reversible",55} {resultNoUndoALD.BestQuality,12} {resultNoUndoALD.VisitedNodes,6} ({(resultNoUndoALD.VisitedNodes / resultNoUndoALD.Elapsed.TotalSeconds),12:F2} nodes/sec)");

            var resultNoUndoDFS = Maximize.Start(knapsackNoUndo).DepthFirst();
            Console.WriteLine($"{"DepthFirst non-reversible",55} {resultNoUndoDFS.BestQuality,12} {resultNoUndoDFS.VisitedNodes,6} ({(resultNoUndoDFS.VisitedNodes / resultNoUndoDFS.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParNoUndoDFS = Maximize.Start(knapsackNoUndo).ParallelDepthFirst();
            Console.WriteLine($"{"Parallel DepthFirst non-reversible",55} {resultParNoUndoDFS.BestQuality,12} {resultParNoUndoDFS.VisitedNodes,6} ({(resultParNoUndoDFS.VisitedNodes / resultParNoUndoDFS.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultNoUndoBFS = Maximize.Start(knapsackNoUndo).BreadthFirst();
            Console.WriteLine($"{"BreadthFirst non-reversible",55} {resultNoUndoBFS.BestQuality,12} {resultNoUndoBFS.VisitedNodes,6} ({(resultNoUndoBFS.VisitedNodes / resultNoUndoBFS.Elapsed.TotalSeconds),12:F2} nodes/sec)");
            var resultParNoUndoBFS = Maximize.Start(knapsackNoUndo).ParallelBreadthFirst();
            Console.WriteLine($"{"Parallel BreadthFirst non-reversible",55} {resultParNoUndoBFS.BestQuality,12} {resultParNoUndoBFS.VisitedNodes,6} ({(resultParNoUndoBFS.VisitedNodes / resultParNoUndoBFS.Elapsed.TotalSeconds),12:F2} nodes/sec)");
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

            var resultParallelDF = Minimize.Start(tsp)
                .WithRuntimeLimit(TimeSpan.FromSeconds(5))
                .ParallelDepthFirst(filterWidth: 2);
            Console.WriteLine($"ParallelDepthFirst(16) {resultParallelDF.BestQuality} {resultParallelDF.VisitedNodes} ({(resultParallelDF.VisitedNodes / resultParallelDF.Elapsed.TotalSeconds):F2} nodes/sec)");

            var resultParallelBS100 = Minimize.Start(tsp)
                .ParallelBeamSearch(100, state => state.Bound.Value, 3);
            Console.WriteLine($"ParallelBeamSearch(100,3) {resultParallelBS100.BestQuality} {resultParallelBS100.VisitedNodes} ({(resultParallelBS100.VisitedNodes / resultParallelBS100.Elapsed.TotalSeconds):F2} nodes/sec)");

            var resultParallelPilot = Minimize.Start(tsp)
                .WithRuntimeLimit(TimeSpan.FromSeconds(5))
                .ParallelPilotMethod(filterWidth: 2);
            Console.WriteLine($"ParallelPilotMethod(16) {resultParallelPilot.BestQuality} {resultParallelPilot.VisitedNodes} ({(resultParallelPilot.VisitedNodes / resultParallelPilot.Elapsed.TotalSeconds):F2} nodes/sec)");
        }

        private static void SchedulingProblem()
        {
            // generate sample data for jobs and machines
            var random = new Random(13);
            var now = DateTime.Now.Date.AddHours(7.5);
            var jobs = Enumerable.Range(0, 10).Select(x => new Job
            {
                Id = x + 1,
                Name = $"Job {x+1}",
                ReadyDate = now.AddMinutes(random.Next(0, 100)),
                Duration = TimeSpan.FromMinutes(random.Next(10, 20))
            }).ToList();
            var machines = Enumerable.Range(0, 3).Select(x => new Machine
            {
                Id = x + 1,
                Name = $"Machine {x+1}",
                Start = now
            }).ToList();

            var state = new SchedulingProblem(SampleApp.SchedulingProblem.ObjectiveType.Makespan, jobs, machines);
            var control = Minimize.Start(state).DepthFirst();
            var result = control.BestQualityState;
            Console.WriteLine("===== Makespan =====");
            Console.WriteLine($"Objective: {result.Quality}");
            Console.WriteLine($"Nodes: {control.VisitedNodes}");
            foreach (var group in result.Choices.GroupBy(c => c.Machine))
            {
                Console.WriteLine(group.Key.Name);
                foreach (var c in group.OrderBy(c => c.ScheduledDate))
                {
                    Console.WriteLine($"  {c.Job.Name} {c.Job.ReadyDate} {c.Job.Duration} {c.ScheduledDate}");
                }
            }
            
            state = new SchedulingProblem(SampleApp.SchedulingProblem.ObjectiveType.Delay, jobs, machines);
            control = Minimize.Start(state).DepthFirst();
            result = control.BestQualityState;
            Console.WriteLine("===== Job Delay =====");
            Console.WriteLine($"Objective: {result.Quality}");
            Console.WriteLine($"Nodes: {control.VisitedNodes}");
            foreach (var group in result.Choices.GroupBy(c => c.Machine))
            {
                Console.WriteLine(group.Key.Name);
                foreach (var c in group.OrderBy(c => c.ScheduledDate))
                {
                    Console.WriteLine($"  {c.Job.Name} {c.Job.ReadyDate} {c.Job.Duration} {c.ScheduledDate}");
                }
            }
            
            state = new SchedulingProblem(SampleApp.SchedulingProblem.ObjectiveType.TotalCompletionTime, jobs, machines);
            control = Minimize.Start(state).DepthFirst();
            result = control.BestQualityState;
            Console.WriteLine("===== Total Completion Time =====");
            Console.WriteLine($"Objective: {result.Quality}");
            Console.WriteLine($"Nodes: {control.VisitedNodes}");
            foreach (var group in result.Choices.GroupBy(c => c.Machine))
            {
                Console.WriteLine(group.Key.Name);
                foreach (var c in group.OrderBy(c => c.ScheduledDate))
                {
                    Console.WriteLine($"  {c.Job.Name} {c.Job.ReadyDate} {c.Job.Duration} {c.ScheduledDate}");
                }
            }
        }
    }
}
