using System.Collections.Generic;

namespace TreesearchLib
{
    public interface IQuality<T>
    {
        bool IsBetter(T other);
    }

    public struct Minimize : IQuality<Minimize>
    {
        int value;
        public Minimize(int value)
        {
            this.value = value;
        }

        public bool IsBetter(Minimize other) => value < other.value;        

        public override string ToString() => $"Quality( {value} )";
    }

    public struct Maximize : IQuality<Maximize>
    {
        int value;
        public Maximize(int value)
        {
            this.value = value;
        }

        public bool IsBetter(Maximize other) => value > other.value;

        public override string ToString() => $"Quality( {value} )";
    }

    public interface ISearchable<TChoice, TQuality> where TQuality : struct, IQuality<TQuality>
    {
        TQuality LowerBound { get; }
        TQuality? Quality { get; }

        IEnumerable<TChoice> GetChoices();
        void Apply(TChoice choice);
    }

    public interface ISearchableWithUndo<Choice, TQuality> : ISearchable<Choice, TQuality> where TQuality : struct, IQuality<TQuality>
    {
        int ChoicesMade { get; }
        void UndoLast();
    }

    public interface IMinimizable<TChoice> : ISearchable<TChoice, Minimize>
    {

    }

    public interface IMinimizableWithUndo<TChoice> : ISearchableWithUndo<TChoice, Minimize>
    {

    }

    public interface IMaximizable<TChoice> : ISearchable<TChoice, Maximize>
    {

    }

    public interface IMaximizableWithUndo<TChoice> : ISearchableWithUndo<TChoice, Maximize>
    {

    }
}