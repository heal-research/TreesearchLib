using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TreesearchLib;

namespace SampleApp
{
    /// <summary>
    /// Provides an implementation of the {0,1}-Knapsack problem that
    /// supports undo, i.e., moves can be applied and reversed. It is
    /// less efficient to clone this state, than <seealso cref="KnapsackNoUndo"/>,
    /// because the Decision vector is always full length.
    /// </summary>
    public class Knapsack : IMutableState<Knapsack, (bool, int), Maximize>
    {
        public IReadOnlyList<int> Profits { get; set; }
        public IReadOnlyList<int> Weights { get; set; }
        public int Capacity { get; set; }

        public Stack<(bool, int)> Decision { get; private set; }

        public int TotalWeight { get; set; }
        public int TotalProfit { get; set; }

        public Knapsack(IReadOnlyList<int> profits, IReadOnlyList<int> weights, int capcity) {
            Profits = profits;
            Weights = weights;
            Capacity = capcity;
            Decision = new Stack<(bool, int)>();
        }
        public Knapsack(Knapsack other)
        {
            Profits = other.Profits;
            Weights = other.Weights;
            Capacity = other.Capacity;
            Decision = new Stack<(bool, int)>(other.Decision.Reverse());
            TotalWeight = other.TotalWeight;
            TotalProfit = other.TotalProfit;
            IsTerminal = other.IsTerminal;
        }

        public bool IsTerminal { get; set; }

        // Caching the bound improves performance a lot when e.g., using it
        // as sorting criteria in beam search
        private Maximize? cachedbound;
        public Maximize Bound {
            get {
                if (!cachedbound.HasValue)
                {
                    if (TotalWeight > Capacity)
                    {
                        cachedbound = new Maximize(Capacity - TotalWeight);
                    } else
                    {
                        // This simple bound assumes all remaining items that may
                        // fit (without considering the others) can be added
                        var profit = TotalProfit;
                        var item = Decision.Count > 0 ? Decision.Peek().Item2 : -1;
                        for (var i = item + 1; i < Profits.Count; i++)
                        {
                            if (TotalWeight + Weights[i] <= Capacity)
                            {
                                profit += Profits[i];
                            }
                        }
                        cachedbound = new Maximize(profit);
                    }
                }
                return cachedbound.Value;
            }
        }

        public Maximize? Quality {
            get
            {
                // as the constraint is checked in GetChoices(), the following condition
                // should never be true, however if GetChoices() is changed to consider a relaxed
                // formulation that seeks to minimize overweight, this function is still correct
                if (TotalWeight > Capacity) return new Maximize(Capacity - TotalWeight);
                return new Maximize(TotalProfit);
            }
        }

        public void Apply((bool, int) choice)
        {
            cachedbound = null;
            var (take, item) = choice;
            if (take)
            {
                TotalWeight += Weights[item];
                TotalProfit += Profits[item];
            }
            
            var isTerminal = true;
            for (var i = item + 1; i < Profits.Count; i++)
            {
                if (TotalWeight + Weights[i] <= Capacity)
                {
                    isTerminal = false;
                    break;
                }
            }
            IsTerminal = isTerminal;
            Decision.Push(choice);
        }

        public void UndoLast()
        {
            cachedbound = null;
            var (take, item) = Decision.Pop();
            if (take)
            {
                TotalWeight -= Weights[item];
                TotalProfit -= Profits[item];
            }
            IsTerminal = false;
        }

        public object Clone()
        {
            return new Knapsack(this);
        }

        public IEnumerable<(bool, int)> GetChoices()
        {
            if (IsTerminal) yield break;
            var item = Decision.Count > 0 ? Decision.Peek().Item2 : -1;
            for (var i = item + 1; i < Profits.Count; i++)
            {
                if (Weights[i] + TotalWeight <= Capacity)
                {
                    yield return (true, i);
                    yield return (false, i);
                    yield break;
                }
            }
        }

        public override string ToString()
        {
            return $"Items: {string.Join(", ", Decision.Where(x => x.Item1).Select(x => x.Item2))}";
        }
    }

    /// <summary>
    /// Provides an implementatino of the {0,1}-Knapsack problem that
    /// doesn't support undo and is optimized for more efficient cloning.
    /// </summary>
    public class KnapsackNoUndo : IState<KnapsackNoUndo, Maximize>
    {
        public IReadOnlyList<int> Profits { get; private set; }
        public IReadOnlyList<int> Weights { get; private set; }
        public int Capacity { get; private set; }

        public bool[] Decision { get; private set; }

        public int TotalWeight { get; private set; }
        public int TotalProfit { get; private set; }

        public KnapsackNoUndo(IReadOnlyList<int> profits, IReadOnlyList<int> weights, int capacity)
        {
            Profits = profits;
            Weights = weights;
            Capacity = capacity;
            Decision = new bool[0];
            TotalWeight = 0;
            TotalProfit = 0;
        }
        public KnapsackNoUndo(KnapsackNoUndo other)
        {
            Profits = other.Profits;
            Weights = other.Weights;
            Capacity = other.Capacity;
            Decision = other.Decision; // is considered immutable
            TotalWeight = other.TotalWeight;
            TotalProfit = other.TotalProfit;
        }
        public KnapsackNoUndo(KnapsackNoUndo other, bool choice, int item) : this(other)
        {
            if (choice)
            {
                TotalWeight += Weights[item];
                TotalProfit += Profits[item];
            }
            var decision = new bool[item + 1];
            Array.Copy(other.Decision, decision, other.Decision.Length);
            decision[item] = choice;
            Decision = decision;
        }

        public bool IsTerminal => Decision.Length == Profits.Count;

        public Maximize Bound
        {
            get
            {
                if (TotalWeight > Capacity)
                {
                    return new Maximize(Capacity - TotalWeight);
                } else
                {
                    // This simple bound assumes all remaining items that may
                    // fit (without considering the others) can be added
                    var profit = TotalProfit;
                    for (var i = Decision.Length; i < Profits.Count; i++)
                    {
                        if (TotalWeight + Weights[i] <= Capacity)
                        {
                            profit += Profits[i];
                        }
                    }
                    return new Maximize(profit);
                }
            }
        }

        public Maximize? Quality
        {
            get
            {
                // as the constraint is checked in GetBranches(), the following condition
                // should never be true, however if GetBranches() is changed to consider a relaxed
                // formulation that seeks to minimize overweight, this function is still correct
                if (TotalWeight > Capacity) return new Maximize(Capacity - TotalWeight);
                return new Maximize(TotalProfit);
            }
        }

        public object Clone()
        {
            return new KnapsackNoUndo(this);
        }

        public IEnumerable<KnapsackNoUndo> GetBranches()
        {
            if (IsTerminal) yield break;
            for (var i = Decision.Length; i < Profits.Count; i++)
            {
                if (Weights[i] + TotalWeight <= Capacity)
                {
                    yield return new KnapsackNoUndo(this, true, i);
                    yield return new KnapsackNoUndo(this, false, i);
                    yield break;
                }
            }
        }

        public override string ToString()
        {
            return $"Items: {string.Join(", ", Decision.Select((v, i) => (i, v)).Where(x => x.v).Select(x => x.i))}";
        }

        public override bool Equals(object obj)
        {
            if (obj is KnapsackNoUndo other)
            {
                if (!Profits.SequenceEqual(other.Profits)) return false;
                if (!Weights.SequenceEqual(other.Weights)) return false;
                if (Capacity != other.Capacity) return false;
                if (TotalWeight != other.TotalWeight) return false;
                if (TotalProfit != other.TotalProfit) return false;
                if (Decision.Length != other.Decision.Length) return false;
                for (var i = 0; i < Decision.Length; i++)
                {
                    if (Decision[i] != other.Decision[i]) return false;
                }
                return true;
            }
            return false;
        }
    }
}