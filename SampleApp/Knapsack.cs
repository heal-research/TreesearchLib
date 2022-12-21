using System;
using System.Collections.Generic;
using System.Linq;
using TreesearchLib;

namespace SampleApp
{
    public class Knapsack : IUndoState<Knapsack, bool, Maximize>
    {
        public IReadOnlyList<int> Profits { get; set; }
        public IReadOnlyList<int> Weights { get; set; }
        public int Capacity { get; set; }

        public Stack<int> Selected { get; private set; } = new();
        public Stack<bool> Decision { get; private set; } = new();

        public int TotalWeight { get; set; }
        public int TotalProfit { get; set; }

        public Knapsack() { }
        public Knapsack(Knapsack other)
        {
            Profits = other.Profits;
            Weights = other.Weights;
            Capacity = other.Capacity;
            Selected = new Stack<int>(other.Selected.Reverse());
            Decision = new Stack<bool>(other.Decision.Reverse());
            TotalWeight = other.TotalWeight;
            TotalProfit = other.TotalProfit;
        }

        public Maximize Bound {
            get {
                if (TotalWeight > Capacity) return new Maximize(Capacity - TotalWeight);
                var profit = TotalProfit;
                for (var i = Decision.Count; i < Profits.Count; i++)
                {
                    if (TotalWeight + Weights[i] <= Capacity)
                    {
                        profit += Profits[i];
                    }
                }
                return new Maximize(profit);
            }
        }

        public Maximize? Quality {
            get {
                if (Decision.Count < Profits.Count) return null;
                if (TotalWeight > Capacity) return new Maximize(Capacity - TotalWeight);
                return new Maximize(TotalProfit);
            }
        }

        public void Apply(bool choice)
        {
            if (choice)
            {
                var item = Decision.Count;
                Selected.Push(item);
                TotalWeight += Weights[item];
                TotalProfit += Profits[item];
            }
            Decision.Push(choice);
        }

        public object Clone()
        {
            return new Knapsack(this);
        }

        public IEnumerable<bool> GetChoices()
        {
            var item = Decision.Count;
            if (item == Profits.Count) yield break;
            if (Weights[item] + TotalWeight <= Capacity)
                yield return true;
            yield return false;
        }

        public void UndoLast()
        {
            var choice = Decision.Pop();
            if (choice)
            {
                var item = Decision.Count;
                if (item != Selected.Pop()) throw new InvalidOperationException("Item is unexpected");
                TotalWeight -= Weights[item];
                TotalProfit -= Profits[item];
            }
        }

        public override string ToString()
        {
            return $"Items: {string.Join(", ", Selected)}";
        }
    }
}