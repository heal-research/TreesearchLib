using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TreesearchLib
{
    public static class MCTSStateExtensions
    {
        public static Task<TState> MCTSAsync<TState>(this IState<TState, Maximize> state,
                double confidence = 1.414213562373095, bool adaptiveConfidence = true,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, Maximize> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, Maximize>
        {
            return Task.Run(() => MCTS(state, confidence, adaptiveConfidence, seed, runtime, nodelimit, callback, token));
        }

        public static TState MCTS<TState>(this IState<TState, Maximize> state,
                double confidence = 1.414213562373095, bool adaptiveConfidence = true,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, Maximize> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, Maximize>
        {
            if (!runtime.HasValue && !nodelimit.HasValue && (token == default(CancellationToken) || token == CancellationToken.None))
                throw new ArgumentException("No termination condition provided for MCTS");
            var control = SearchControl<TState, Maximize>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            Action<MCTSNode<TState, Maximize>, TState> updateNodeScore = (node, s) => node.Score += s.Quality.Value.Value;
            return MonteCarloTreeSearch<TState, Maximize>.Search(control, updateNodeScore, confidence, adaptiveConfidence, seed).State;
        }

        public static Task<TState> MCTSAsync<TState>(this IState<TState, Minimize> state,
                double confidence = 1.414213562373095, bool adaptiveConfidence = true,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, Minimize> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, Minimize>
        {
            return Task.Run(() => MCTS(state, confidence, adaptiveConfidence, seed, runtime, nodelimit, callback, token));
        }

        public static TState MCTS<TState>(this IState<TState, Minimize> state,
                double confidence = 1.414213562373095, bool adaptiveConfidence = true,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, Minimize> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : IState<TState, Minimize>
        {
            if (!runtime.HasValue && !nodelimit.HasValue && (token == default(CancellationToken) || token == CancellationToken.None))
                throw new ArgumentException("No termination condition provided for MCTS");
            var control = SearchControl<TState, Minimize>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            Action<MCTSNode<TState, Minimize>, TState> updateNodeScore = (node, s) => node.Score -= s.Quality.Value.Value;
            return MonteCarloTreeSearch<TState, Minimize>.Search(control, updateNodeScore, confidence, adaptiveConfidence, seed).State;
        }

        public static Task<TState> MCTSAsync<TState, TChoice>(this IMutableState<TState, TChoice, Maximize> state,
                double confidence = 1.414213562373095, bool adaptiveConfidence = true, 
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, Maximize> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, Maximize>
        {
            return Task.Run(() => MCTS(state, confidence, adaptiveConfidence, seed, runtime, nodelimit, callback, token));
        }

        public static TState MCTS<TState, TChoice>(this IMutableState<TState, TChoice, Maximize> state,
                double confidence = 1.414213562373095, bool adaptiveConfidence = true,
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, Maximize> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, Maximize>
        {
            if (!runtime.HasValue && !nodelimit.HasValue && (token == default(CancellationToken) || token == CancellationToken.None))
                throw new ArgumentException("No termination condition provided for MCTS");
            var control = SearchControl<TState, TChoice, Maximize>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            Action<MCTSNode<TState, TChoice, Maximize>, TState> updateNodeScore = (node, s) => node.Score += s.Quality.Value.Value;
            return MonteCarloTreeSearch<TState, TChoice, Maximize>.Search(control, updateNodeScore, confidence, adaptiveConfidence, seed).State;
        }

        public static Task<TState> MCTSAsync<TState, TChoice>(this IMutableState<TState, TChoice, Minimize> state,
                double confidence = 1.414213562373095, bool adaptiveConfidence = true, 
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, Minimize> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, Minimize>
        {
            return Task.Run(() => MCTS(state, confidence, adaptiveConfidence, seed, runtime, nodelimit, callback, token));
        }

        public static TState MCTS<TState, TChoice>(this IMutableState<TState, TChoice, Minimize> state,
                double confidence = 1.414213562373095, bool adaptiveConfidence = true, 
                int? seed = null, TimeSpan? runtime = null, long? nodelimit = null,
                QualityCallback<TState, Minimize> callback = null,
                CancellationToken token = default(CancellationToken))
            where TState : class, IMutableState<TState, TChoice, Minimize>
        {
            if (!runtime.HasValue && !nodelimit.HasValue && (token == default(CancellationToken) || token == CancellationToken.None))
                throw new ArgumentException("No termination condition provided for MCTS");
            var control = SearchControl<TState, TChoice, Minimize>.Start((TState)state).WithCancellationToken(token);
            if (runtime.HasValue) control = control.WithRuntimeLimit(runtime.Value);
            if (nodelimit.HasValue) control = control.WithNodeLimit(nodelimit.Value);
            if (callback != null) control = control.WithImprovementCallback(callback);
            Action<MCTSNode<TState, TChoice, Minimize>, TState> updateNodeScore = (node, s) => node.Score -= s.Quality.Value.Value;
            return MonteCarloTreeSearch<TState, TChoice, Minimize>.Search(control, updateNodeScore, confidence, adaptiveConfidence, seed).State;
        }
    }
    
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

    public class MonteCarloTreeSearch<TState, TQuality>
        where TState : IState<TState, TQuality>
        where TQuality : struct, IQuality<TQuality>
    {
        // Perform one iteration of Monte Carlo tree search
        public static MCTSNode<TState, TQuality> Search(SearchControl<TState, TQuality> control, Action<MCTSNode<TState, TQuality>, TState> updateNodeScore, double confidence = 1.414213562373095, bool adaptiveConfidence = true, int? seed = null)
        {
            var rng = seed.HasValue ? new Random(seed.Value) : new Random();
            
            var root = new MCTSNode<TState, TQuality>(control.InitialState, null);
            while (!control.ShouldStop())
            {
                // Selection
                var node = root;
                while (!node.State.IsTerminal && node.Children.Count > 0)
                {
                    node = SelectChild(node, confidence);
                }

                // Expansion
                if (!node.State.IsTerminal)
                {
                    node = ExpandNode(control, node, rng);

                    // Simulation
                    var result = Simulate(control, node.State, rng);

                    // Backpropagation
                    while (node != null)
                    {
                        node.Visits++;
                        updateNodeScore(node, result);
                        node = node.Parent;
                    }
                    if (adaptiveConfidence) confidence *= 0.903602;
                } else if (adaptiveConfidence) confidence *= 1.5;
            }

            // Return the child with the highest win rate
            return GetBestChild(root);
        }

        // Select the child with the highest Upper Confidence Bound (UCB) score
        private static MCTSNode<TState, TQuality> SelectChild(MCTSNode<TState, TQuality> node, double confidence)
        {
            if (node.Children.Count == 1) return node.Children[0];

            MCTSNode<TState, TQuality> selected = null;
            var bestScore = double.MinValue;

            var parentlog = Math.Log(node.Visits);
            foreach (var child in node.Children)
            {
                if (child.Visits == 0) return child;

                var score = child.Score / (double)child.Visits +
                    confidence * Math.Sqrt(parentlog / child.Visits);
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
    // A node in the Monte Carlo tree
    public class MCTSNode<TState, TChoice, TQuality>
        where TState : class, IMutableState<TState, TChoice, TQuality>
        where TQuality : struct, IQuality<TQuality>
    {
        public TState State { get; set; }
        public MCTSNode<TState, TChoice, TQuality> Parent { get; set; }
        public List<MCTSNode<TState, TChoice, TQuality>> Children { get; set; }
        public int Visits { get; set; }
        public int Score { get; set; }

        public MCTSNode(TState state, MCTSNode<TState, TChoice, TQuality> parent)
        {
            State = state;
            Parent = parent;
            Children = new List<MCTSNode<TState, TChoice, TQuality>>();
            Visits = 0;
            Score = 0;
        }
    }

    public class MonteCarloTreeSearch<TState, TChoice, TQuality>
        where TState : class, IMutableState<TState, TChoice, TQuality>
        where TQuality : struct, IQuality<TQuality>
    {
        // Perform one iteration of Monte Carlo tree search
        public static MCTSNode<TState, TChoice, TQuality> Search(SearchControl<TState, TChoice, TQuality> control, Action<MCTSNode<TState, TChoice, TQuality>, TState> updateNodeScore, double confidence = 1.414213562373095, bool adaptiveConfidence = true, int? seed = null)
        {
            var rng = seed.HasValue ? new Random(seed.Value) : new Random();

            var root = new MCTSNode<TState, TChoice, TQuality>(control.InitialState, null);
            if (root.State.IsTerminal) return root;

            while (!control.ShouldStop())
            {
                // Selection
                var node = root;
                while (!node.State.IsTerminal && node.Children.Count > 0)
                {
                    node = SelectChild(node, confidence);
                }

                if (!node.State.IsTerminal)
                {
                    // Expansion
                    node = ExpandNode(control, node, rng);

                    // Simulation
                    var result = Simulate(control, node.State, rng);

                    // Backpropagation
                    while (node != null)
                    {
                        node.Visits++;
                        updateNodeScore(node, result);
                        node = node.Parent;
                    }
                    if (adaptiveConfidence) confidence *= 0.903602;
                } else if(adaptiveConfidence) confidence *= 1.5;
            }

            // Return the child with the highest win rate
            return GetBestChild(root);
        }

        // Select the child with the highest Upper Confidence Bound (UCB) score
        private static MCTSNode<TState, TChoice, TQuality> SelectChild(MCTSNode<TState, TChoice, TQuality> node, double confidence)
        {
            if (node.Children.Count == 1) return node.Children[0];

            MCTSNode<TState, TChoice, TQuality> selected = null;
            var bestScore = double.MinValue;

            var parentlog = Math.Log(node.Visits);
            foreach (var child in node.Children)
            {
                if (child.Visits == 0) return child;

                var score = child.Score / (double)child.Visits +
                    confidence * Math.Sqrt(parentlog / child.Visits);
                if (score > bestScore)
                {
                    selected = child;
                    bestScore = score;
                }
            }

            return selected;
        }

        // Expand the node by adding one of its untried children
        private static MCTSNode<TState, TChoice, TQuality> ExpandNode(SearchControl<TState, TChoice, TQuality> control, MCTSNode<TState, TChoice, TQuality> node, Random rng)
        {
            // Add a child node for each untried child state
            foreach (var choice in node.State.GetChoices())
            {
                var next = (TState)node.State.Clone();
                next.Apply(choice);
                control.VisitNode(next);
                var child = new MCTSNode<TState, TChoice, TQuality>(next, node);
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
        private static TState Simulate(SearchControl<TState, TChoice, TQuality> control, TState state, Random rng)
        {
            state = (TState)state.Clone(); // since state is mutated, make a clone first
            while (!state.IsTerminal)
            {
                MakeRandomMove(state, rng);
                control.VisitNode(state);
            }
            return state;
        }

        // Make a random move in the given state
        private static void MakeRandomMove(TState state, Random rng)
        {
            TChoice sel = default(TChoice);
            int total = 0;
            foreach (var next in state.GetChoices())
            {
                total++;
                if (rng.NextDouble() * total < 1.0)
                {
                    sel = next;
                }
            }
            state.Apply(sel);
        }

        // Get the child with the highest win rate
        private static MCTSNode<TState, TChoice, TQuality> GetBestChild(MCTSNode<TState, TChoice, TQuality> node)
        {
            MCTSNode<TState, TChoice, TQuality> best = null;
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

