### TreesearchLib

TreesearchLib is a C# framework for modeling optimization problems as search trees and a collection of algorithms to identify good solutions for those problems. It includes exhaustive algorithms, as well as heuristics.

Modeling optimization problems is performed by implementing a problem state class. This class maintains the decisions that have been taken, as well as the next choices, i.e., branches in the search tree. It is possible to compute bounds, which algorithms may use to discard parts of the tree.

#### Examples

Check out the SampleApp to see implementations of the following problems:

 * [ChooseSmallestProblem](src/SampleApp/ChooseSmallestProblem.cs) - a fun problem which searches small values in the sequence of random number seeds
 * [Knapsack](src/SampleApp/Knapsack.cs) - the famous {0, 1}-Knapsack, implemented using reversible search (allowing to undo moves), as well as non-reversible
 * [TSP](src/SampleApp/TSP.cs) - the Berlin52 instance of the TSPLIB
 * [SchedulingProblem](src/SampleApp/SchedulingProblem.cs) - a very simple scheduling problem
 * [Tower of Hanoi](src/SampleApp/TowerOfHanoi.cs) - the classic Tower of Hanoi problem

These samples should give you an idea on how to use the framework for problem modeling.

#### Validation

You should use the state's extension method `Test` to check whether your implementation is correct. Not all errors can be detected, but several subtle problems can be discovered, e.g. undo operations that result in a state which outputs a different set of choices than before. The Program.cs in the SampleApp calls this method for all problems. For instance

```csharp
var hanoi = new TowerOfHanoi(3, 3);
var testResult = hanoi.Test<TowerOfHanoi, (int, int), Minimize>(EqualityComparer<(int, int)>.Default);
Console.WriteLine($"Is TowerOfHanoi implemented correctly: {testResult}");
```

If the result is `TestResult.Ok` the implementation is likely correct. Otherwise, the enum provides hints on potential problems.

#### Algorithms

The algorithms that are included are:

 * Depth-first search / branch and bound, sequential and parallel
 * Breadth-first search, sequential and parallel
 * Limited Discrepancy Search, sequential only
 * Beam Search, sequental and parallel
 * Monotonic Beam Search, sequential only
 * Rake Search (and a Rake+Beam combination), sequential and parallel
 * Pilot Method, sequential and parallel
 * Monte Carlo Tree Search, sequential only

New hybrid algorithms can be implemented, also by making use of the existing algorithms.