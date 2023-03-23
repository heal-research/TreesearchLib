### TreesearchLib

TreesearchLib is a C# framework for modeling optimization problems as search trees and a collection of algorithms to identify good solutions for those problems. It includes exhaustive algorithms, as well as heuristics.

Modeling optimization problems is performed by implementing a problem state class. This class maintains the decisions that have been taken, as well as the next choices, i.e., branches in the search tree. It is possible to compute bounds, which algorithms may use to discard parts of the tree.

Check out the SampleApp to see implementations of the following problems:

 * [ChooseSmallestProblem](src/SampleApp/ChooseSmallestProblem.cs) - a fun problem which searches small values in the sequence of random number seeds
 * [Knapsack](src/SampleApp/Knapsack.cs) - the famous {0, 1}-Knapsack, implemented using reversible search (allowing to undo moves), as well as non-reversible
 * [TSP](src/SampleApp/TSP.cs) - the Berlin52 instance of the TSPLIB
 * [SchedulingProblem](src/SampleApp/SchedulingProblem.cs) - a very simple scheduling problem

These samples should give you an idea on how to use the framework for problem modeling.

The algorithms that are included are:

 * Branch and bound (depth-first search), sequential and parallel
 * Breadth-first search, sequential and parallel
 * Limited Discrepancy Search, sequential only
 * Beam Search, sequental and parallel
 * Monotonic Beam Search, sequential only
 * Rake Search (and a Rake+Beam combination), sequential and parallel
 * Pilot Method, sequential and parallel
 * Monte Carlo Tree Search, sequential only

New hybrid algorithms can be implemented, also by making use of the existing algorithms.