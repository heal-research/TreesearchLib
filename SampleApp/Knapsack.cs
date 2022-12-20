using System;
using System.Collections.Generic;
using System.Linq;
using TreesearchLib;

namespace SampleApp
{
    public class Knapsack : IUndoState<Knapsack, bool, Maximize>
    {
        public int[] Profits { get; set; }
        public int[] Weights { get; set; }
        public int Capacity { get; set; }

        public Stack<int> Selected { get; private set; } = new();
        public Stack<bool> Decision { get; private set; } = new();

        public Knapsack() { }
        public Knapsack(Knapsack other)
        {
            Profits = other.Profits;
            Weights = other.Weights;
            Capacity = other.Capacity;
            Selected = new Stack<int>(other.Selected.Reverse());
            Decision = new Stack<bool>(other.Decision.Reverse());
        }

        public Maximize Bound {
            get {
                int totalWeight = 0, totalProfit = 0;
                foreach (var item in Selected)
                {
                    totalWeight += Weights[item];
                    totalProfit += Profits[item];
                }
                if (totalWeight > Capacity) return new Maximize(Capacity - totalWeight);
                for (var i = Decision.Count; i < Profits.Length; i++)
                {
                    if (totalWeight + Weights[i] <= Capacity)
                    {
                        totalProfit += Profits[i];
                    }
                }
                return new Maximize(totalProfit);
            }
        }

        public Maximize? Quality {
            get {
                if (Decision.Count < Profits.Length) return null;
                int totalWeight = 0, totalProfit = 0;
                foreach (var item in Selected)
                {
                    totalWeight += Weights[item];
                    totalProfit += Profits[item];
                }
                if (totalWeight > Capacity) return new Maximize(Capacity - totalWeight);
                return new Maximize(totalProfit);
            }
        }

        public void Apply(bool choice)
        {
            if (choice)
            {
                Selected.Push(Decision.Count);
            }
            Decision.Push(choice);
        }

        public object Clone()
        {
            return new Knapsack(this);
        }

        public IEnumerable<bool> GetChoices()
        {
            if (Decision.Count == Profits.Length) yield break;
            yield return true;
            yield return false;
        }

        public void UndoLast()
        {
            var choice = Decision.Pop();
            if (choice)
            {
                Selected.Pop();
            }
        }

        public override string ToString()
        {
            return $"Items: {string.Join(", ", Selected)}";
        }
    }
}