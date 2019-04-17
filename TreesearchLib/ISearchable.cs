using System;
using System.Collections.Generic;

namespace TreesearchLib
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

    public interface ISearchable<Choice>
    {
        Quality LowerBound { get; }
        Quality? Quality { get; }

        IEnumerable<Choice> GetChoices();
        void Apply(Choice choice);
    }

    public interface ISearchableReversible<Choice> : ISearchable<Choice>
    {
        int ChoicesMade { get; }
        void UndoLast();
    }
}