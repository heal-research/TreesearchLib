using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TreesearchLib;

namespace SampleApp
{
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
            if (Weights[Item] + TotalWeight <= Capacity)
                yield return true;
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
            cachedbound = null;
        }
        public KnapsackNoUndo(KnapsackNoUndo other)
        {
            Profits = other.Profits;
            Weights = other.Weights;
            Capacity = other.Capacity;
            Decision = other.Decision; // is considered immutable
            TotalWeight = other.TotalWeight;
            TotalProfit = other.TotalProfit;
            cachedbound = other.cachedbound;
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
            cachedbound = null;
        }

        public bool IsTerminal => Decision.Length == Profits.Count;

        private Maximize? cachedbound;
        public Maximize Bound
        {
            get
            {
                if (!cachedbound.HasValue)
                {
                    if (TotalWeight > Capacity)
                    {
                        cachedbound = new Maximize(Capacity - TotalWeight);
                    } else
                    {
                        var profit = TotalProfit;
                        for (var i = Decision.Length; i < Profits.Count; i++)
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

        public Maximize? Quality
        {
            get
            {
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
            if (Weights[item] + TotalWeight <= Capacity)
            {
                yield return new KnapsackNoUndo(this, true);
            }
            yield return new KnapsackNoUndo(this, false);
        }

        public override string ToString()
        {
            return $"Items: {string.Join(", ", Decision.Select((v, i) => (i, v)).Where(x => x.v).Select(x => x.i))}";
        }
    }
}