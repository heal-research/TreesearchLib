using System;
using System.Collections.Generic;

namespace TreesearchLib
{
    public interface IQuality<T> : IComparable<T> where T : struct
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

        public override string ToString() => $"min( {value} )";

        public static SearchControl<TState, Minimize> Start<TState>(TState state)
            where TState : IState<TState, Minimize> {
            return SearchControl<TState, Minimize>.Start(state);
        }
        public static SearchControl<TState, TChoice, Minimize> Start<TState, TChoice>(IMutableState<TState, TChoice, Minimize> state)
            where TState : class, IMutableState<TState, TChoice, Minimize> {
            return SearchControl<TState, TChoice, Minimize>.Start((TState)state);
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

        public override string ToString() => $"max( {value} )";

        public static SearchControl<TState, Maximize> Start<TState>(TState state)
            where TState : IState<TState, Maximize> {
            return SearchControl<TState, Maximize>.Start(state);
        }
        public static SearchControl<TState, TChoice, Maximize> Start<TState, TChoice>(IMutableState<TState, TChoice, Maximize> state)
            where TState : class, IMutableState<TState, TChoice, Maximize> {
            return SearchControl<TState, TChoice, Maximize>.Start((TState)state);
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

    public interface IMutableState<TState, TChoice, TQuality> : IQualifiable<TQuality>
        where TState : class, IMutableState<TState, TChoice, TQuality>
        where TQuality : struct, IQuality<TQuality>
    {
        IEnumerable<TChoice> GetChoices();
        void Apply(TChoice choice);
        void UndoLast();
    }
}