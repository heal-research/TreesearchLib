using System;
using System.Collections.Generic;

namespace TreesearchLib
{
    // A node in the Monte Carlo tree
    public class MCTSNode<TState, TQuality>
        where TState : IState<TState, TQuality>
        where TQuality : struct, IQuality<TQuality>
    {
        public TState State { get; set; }
        public MCTSNode<TState, TQuality> Parent { get; set; }
        public List<MCTSNode<TState, TQuality>> Children { get; set; }
        public int Visits { get; set; }
        public int Score { get; set; }

        public MCTSNode(TState state, MCTSNode<TState, TQuality> parent)
        {
            State = state;
            Parent = parent;
            Children = new List<MCTSNode<TState, TQuality>>();
            Visits = 0;
            Score = 0;
        }
    }

    public class MCTS<TState, TQuality>
        where TState : IState<TState, TQuality>
        where TQuality : struct, IQuality<TQuality>
    {
        // Perform one iteration of Monte Carlo tree search
        public static MCTSNode<TState, TQuality> Search(SearchControl<TState, TQuality> control, Action<MCTSNode<TState, TQuality>, TState> updateNodeScore, int? seed = null)
        {
            var rng = seed.HasValue ? new Random(seed.Value) : new Random();
            
            var root = new MCTSNode<TState, TQuality>(control.InitialState, null);
            while (!control.ShouldStop())
            {
                // Selection
                var node = root;
                while (!node.State.IsTerminal && node.Children.Count > 0)
                {
                    node = SelectChild(node);
                }

                // Expansion
                if (!node.State.IsTerminal)
                {
                    node = ExpandNode(control, node, rng);
                }

                // Simulation
                var result = Simulate(control, node.State, rng);

                // Backpropagation
                while (node != null)
                {
                    node.Visits++;
                    updateNodeScore(node, result);
                    node = node.Parent;
                }
            }

            // Return the child with the highest win rate
            return GetBestChild(root);
        }

        // Select the child with the highest Upper Confidence Bound (UCB) score
        private static MCTSNode<TState, TQuality> SelectChild(MCTSNode<TState, TQuality> node)
        {
            MCTSNode<TState, TQuality> selected = null;
            var bestScore = double.MinValue;

            foreach (var child in node.Children)
            {
                if (child.Visits == 0) return child;

                var score = child.Score / (double)child.Visits +
                    Math.Sqrt(2 * Math.Log(node.Visits) / (double)child.Visits);
                if (score > bestScore)
                {
                    selected = child;
                    bestScore = score;
                }
            }

            return selected;
        }

        // Expand the node by adding one of its untried children
        private static MCTSNode<TState, TQuality> ExpandNode(SearchControl<TState, TQuality> control, MCTSNode<TState, TQuality> node, Random rng)
        {
            // Add a child node for each untried child state
            foreach (var next in node.State.GetBranches())
            {
                control.VisitNode(next);
                var child = new MCTSNode<TState, TQuality>(next, node);
                node.Children.Add(child);
                if (next.IsTerminal)
                {
                    return child;
                }
            }

            // sanity check, when IsTerminal would be false, but still no new node
            if (node.Children.Count > 0)
            {
                // Return a randomly selected child
                return node.Children[rng.Next(node.Children.Count)];
            } else
            {
                // reset the score to 0 as this node is awkward (should be terminal, but isn't)
                return node;
            }
        }

        // Simulate the outcome of a game by randomly selecting moves until the game is over
        private static TState Simulate(SearchControl<TState, TQuality> control, TState state, Random rng)
        {
            while (!state.IsTerminal)
            {
                state = MakeRandomMove(state, rng);
                control.VisitNode(state);
            }
            return state;
        }

        // Make a random move in the given state
        private static TState MakeRandomMove(TState state, Random rng)
        {
            TState sel = default(TState);
            int total = 0;
            foreach (var next in state.GetBranches())
            {
                total++;
                if (rng.NextDouble() * total < 1.0)
                {
                    sel = next;
                }
            }
            return sel;
        }

        // Get the child with the highest win rate
        private static MCTSNode<TState, TQuality> GetBestChild(MCTSNode<TState, TQuality> node)
        {
            MCTSNode<TState, TQuality> best = null;
            var bestScore = double.MinValue;
            foreach (var child in node.Children)
            {
                if (child.Visits == 0) continue;
                var score = child.Score / (double)child.Visits;
                if (score > bestScore)
                {
                    best = child;
                    bestScore = score;
                }
            }
            return best ?? node;
        }
    }
}

