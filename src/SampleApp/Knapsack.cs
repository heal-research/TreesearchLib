using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TreesearchLib;

namespace SampleApp
{
    /// <summary>
    /// Provides an implementatino of the {0,1}-Knapsack problem that
    /// supports undo, i.e., moves can be applied and reversed. It is
    /// less efficient to clone this state, than <seealso cref="KnapsackNoUndo"/>,
    /// because the Decision vector is always full length.
    /// </summary>
    public class Knapsack : IMutableState<Knapsack, bool, Maximize>
    {
        public IReadOnlyList<int> Profits { get; set; }
        public IReadOnlyList<int> Weights { get; set; }
        public int Capacity { get; set; }

        public int Item = 0;

        public bool[] Decision { get; private set; }

        public int TotalWeight { get; set; }
        public int TotalProfit { get; set; }

        public Knapsack(IReadOnlyList<int> profits, IReadOnlyList<int> weights, int capcity) {
            Profits = profits;
            Weights = weights;
            Capacity = capcity;
            Decision = new bool[Profits.Count];
        }
        public Knapsack(Knapsack other)
        {
            Profits = other.Profits;
            Weights = other.Weights;
            Capacity = other.Capacity;
            Item = other.Item;
            Decision = new bool[other.Decision.Length];
            Array.Copy(other.Decision, Decision, other.Decision.Length);
            TotalWeight = other.TotalWeight;
            TotalProfit = other.TotalProfit;
        }

        public bool IsTerminal => Item == Profits.Count;

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
                        for (var i = Item; i < Profits.Count; i++)
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

        public void Apply(bool choice)
        {
            cachedbound = null;
            if (choice)
            {
                TotalWeight += Weights[Item];
                TotalProfit += Profits[Item];
            }
            Decision[Item] = choice;
            Item++;
        }

        public object Clone()
        {
            return new Knapsack(this);
        }

        public IEnumerable<bool> GetChoices()
        {
            if (IsTerminal) yield break;
            if (Weights[Item] + TotalWeight <= Capacity) // Capacity constraint
            {
                yield return true;
            }
            yield return false;
        }

        public void UndoLast()
        {
            cachedbound = null;
            Item--;
            var choice = Decision[Item];
            if (choice)
            {
                TotalWeight -= Weights[Item];
                TotalProfit -= Profits[Item];
            }
        }

        public override string ToString()
        {
            return $"Items: {string.Join(", ", Decision.Select((v, i) => (i, v)).Where(x => x.v).Select(x => x.i))}";
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
        public KnapsackNoUndo(KnapsackNoUndo other, bool choice) : this(other)
        {
            if (choice)
            {
                var item = Decision.Length;
                TotalWeight += Weights[item];
                TotalProfit += Profits[item];
            }
            var decision = new bool[other.Decision.Length + 1];
            Array.Copy(other.Decision, decision, other.Decision.Length);
            decision[other.Decision.Length] = choice;
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
            var item = Decision.Length;
            if (Weights[item] + TotalWeight <= Capacity) // Capacity constraint
            {
                yield return new KnapsackNoUndo(this, true);
            }
            yield return new KnapsackNoUndo(this, false);
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