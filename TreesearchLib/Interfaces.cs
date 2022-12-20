using System;
using System.Collections.Generic;

namespace TreesearchLib
{
    public interface IQuality<T> where T : struct
    {
        bool IsBetter(T? other);
    }

    public struct Minimize : IQuality<Minimize>, IComparable<Minimize>
    {
        int value;
        public Minimize(int value)
        {
            this.value = value;
        }

        public bool IsBetter(Minimize? other) => !other.HasValue || value < other.Value.value;

        public override string ToString() => $"Minimize( {value} )";

        public static SearchControl<TState, Minimize> Start<TState>(TState state)
            where TState : class, IState<TState, Minimize> {
            return SearchControl<TState, Minimize>.Start(state);
        }
        public static SearchControlUndo<TState, TChoice, Minimize> Start<TState, TChoice>(IUndoState<TState, TChoice, Minimize> state)
            where TState : class, IUndoState<TState, TChoice, Minimize> {
            return SearchControlUndo<TState, TChoice, Minimize>.Start((TState)state);
        }

        public int CompareTo(Minimize other)
        {
            return value.CompareTo(other.value);
        }
    }

    public struct Maximize : IQuality<Maximize>, IComparable<Maximize>
    {
        int value;
        public Maximize(int value)
        {
            this.value = value;
        }

        public bool IsBetter(Maximize? other) => !other.HasValue || value > other.Value.value;

        public override string ToString() => $"Maximize( {value} )";

        public static SearchControl<TState, Maximize> Start<TState>(TState state)
            where TState : class, IState<TState, Maximize> {
            return SearchControl<TState, Maximize>.Start(state);
        }
        public static SearchControlUndo<TState, TChoice, Maximize> Start<TState, TChoice>(IUndoState<TState, TChoice, Maximize> state)
            where TState : class, IUndoState<TState, TChoice, Maximize> {
            return SearchControlUndo<TState, TChoice, Maximize>.Start((TState)state);
        }

        public int CompareTo(Maximize other)
        {
            return other.value.CompareTo(value);
        }
    }

    public interface IQualifiable<TQuality> : ICloneable
        where TQuality : struct, IQuality<TQuality>
    {
        TQuality Bound { get; }
        TQuality? Quality { get; }
    }

    public interface IState<TState, TQuality> : IQualifiable<TQuality>
        where TState : IState<TState, TQuality>
        where TQuality : struct, IQuality<TQuality>
    {
        IEnumerable<TState> GetBranches();
    }

    public interface IUndoState<TState, TChoice, TQuality> : IQualifiable<TQuality>
        where TState : class, IUndoState<TState, TChoice, TQuality>
        where TQuality : struct, IQuality<TQuality>
    {
        IEnumerable<TChoice> GetChoices();
        void Apply(TChoice choice);
        void UndoLast();
    }
}