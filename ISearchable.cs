using System;
using System.Collections.Generic;
using System.Linq;

namespace Treesearch
{
    public struct Quality : IComparable<Quality>
    {
        int value;
        public Quality(int value)
        {
            this.value = value;
        }

        public int CompareTo(Quality other) => -value.CompareTo(other);

        public bool IsBetter(Quality other) => value < other.value;

        public bool IsWorseOrEqual(Quality other) => value >= other.value;

        public override string ToString() => $"Quality( {value} )";
    }
    public enum ChoiceType
    {
        Construct, Change
    }

    public interface ISearchable<Choice>
    {
        bool IsSolved { get; }
        Quality LowerBound { get; }
        Quality? Quality { get; }

        void FillChoices(List<Choice> choices);
        void Apply(Choice choice);
        ChoiceType ConstructOrChange();
    }
    
    public interface ISearchableReversible<Choice> : ISearchable<Choice>
    {
        int ChoicesMade { get; }
        void UndoLast();
    }
}