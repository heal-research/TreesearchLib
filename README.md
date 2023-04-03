## TreesearchLib

TreesearchLib is a C# framework for modeling optimization problems as search trees and a collection of algorithms to identify good solutions for those problems. It includes exhaustive algorithms, as well as heuristics.

Modeling optimization problems is performed by implementing a problem state class. This class maintains the decisions that have been taken, as well as the next choices, i.e., branches in the search tree. It is possible to compute bounds, which algorithms may use to discard parts of the tree.

```csharp
class MyProblem : IState<MyProblem, Minimize> {
    bool IsTerminal { get; }
    TQuality Bound { get; }
    TQuality? Quality { get; }
    IEnumerable<TState> GetBranches();
}
```

You can use a default bound, e.g. a low enough value for `Minimize`, or a high enough value for `Maximize`, if you don't have a specific bound. Of course stronger bounds make the application of exhaustive algorithms more efficient. You should return a quality value, at least for a terminal state, but also if a quality can be estimated for non-terminal states. Finally, `GetBranches()` returns all descendet states, sorted in a way that the first branch returned is likely the best one. Depth-first search has a bias to descend into the first branch first, also limited discrepancy search assumes the first branch is the one that incurs no cost to follow, while the second and third branch already cost 1 or 2 "discrepancies". Beam search and monotonic beam search allow to define a separate rank function.

When you implement a `Bound` and use it for sorting (e.g. in beam search), make sure the calculation is cached in the state class. Otherwise, the performance of beam search will be unnecessarily bad.

### Examples and Usage

You can invoke the algorithms in several different ways:
    
```csharp
var problem = new MyProblem();
// By using the ISearchControl extension methods
var control = Minimize.Start(problem)
    .WithRuntimeLimit(TimeSpan.FromSeconds(10))
    .PilotMethod();
// By using the IState extension methods
var result = problem.PilotMethod(runtime: TimeSpan.FromSeconds(10));
```

Check out the SampleApp to see implementations of the following problems:

 * [ChooseSmallestProblem](src/SampleApp/ChooseSmallestProblem.cs) - a fun problem which searches small values in the sequence of random number seeds
 * [Knapsack](src/SampleApp/Knapsack.cs) - the famous {0, 1}-Knapsack, implemented using reversible search (allowing to undo moves), as well as non-reversible
 * [TSP](src/SampleApp/TSP.cs) - the Berlin52 instance of the TSPLIB
 * [SchedulingProblem](src/SampleApp/SchedulingProblem.cs) - a very simple scheduling problem with three objective functions
 * [Tower of Hanoi](src/SampleApp/TowerOfHanoi.cs) - the classic Tower of Hanoi problem

These samples should give you an idea on how to use the framework for problem modeling.

### Validation

You should use the state's extension method `Test` to check whether your implementation is correct. Not all errors can be detected, but several subtle problems can be discovered, e.g. undo operations that result in a state which outputs a different set of choices than before. The Program.cs in the SampleApp calls this method for all problems. For instance

```csharp
var hanoi = new TowerOfHanoi(3, 3);
var testResult = hanoi.Test<TowerOfHanoi, (int, int), Minimize>(EqualityComparer<(int, int)>.Default);
Console.WriteLine($"Is TowerOfHanoi implemented correctly: {testResult}");
```

If the result is `TestResult.Ok` the implementation is likely correct. Otherwise, the enum provides hints on potential problems. If you have a more complex choice type, you need to provide an equality comparer for it.

### Algorithms

The algorithms that are included are:

 * Depth-first search / branch and bound, sequential and parallel
 * Breadth-first search, sequential and parallel
 * Limited discrepancy search (naive and anytime variant), sequential only
 * Beam Search, sequental and parallel
 * Monotonic Beam Search, sequential only
 * Rake Search, sequential and parallel
 * Pilot method, sequential and parallel
 * Monte Carlo tree search, sequential only

Rake search essentially combines a breadth-first search with a depth-first search.
New hybrid algorithms can be implemented, also by making use of the existing algorithms.

#### Lookahead

Additionally, the *rake search* and *pilot method* use a lookahead delegate to complete the solution from the current state. There are several options for lookahead which can be used within these two methods:

 1. Depth-first search - e.g. with filterWidth = 1 or with filterWidth > 1, but with a backtrackLimit
 2. Beam search
 3. Monotonic beam search
 4. Rake search (can be a lookahead itself)
 5. Limited discrepancy search

The static class `LA` has several methods to create parameterized lookahead delegates. For instance, the following code creates a lookahead that uses a depth-first search with a filter width of 2 and a backtrack limit of 100:

```csharp
LA.DFSLookahead<MyProblem, Minimize>(filterWidth: 2, backtrackLimit: 100);
```

This means you consider the first two branches at each depth for expansion, but stop after a total of 100 backtracking operations have been performed. Beware, that if you don't use backtrackLimit, your lookahead may take a very long time, as the number of states is $2^n$ (for filterWidth = 2) with $n$ being the depth.

#### Algorithm parameters

An overview of selveral parameters that the algorithms support:

 * filterWidth - when set considers only the first filterWidth branches from a state for expansion
 * depthLimit - search up to a specific depth only, useful, e.g., when states would be able to expand infinitely
 * backtrackLimit - limits the total number of backtrack operations, a backtracks occurs whenever the search has to return to a node closer to the root than the current one
 * beamWidth - a beam is a set of states that are expanded in parallel, the beamWidth defines the maximum size of that set
 * rakeWidth - the rakeWidth defines the number of states to reach at a certain level, before switching to depth-first search