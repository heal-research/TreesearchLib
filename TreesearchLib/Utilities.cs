using System;
using System.Collections.Generic;

namespace TreesearchLib {

    public static class UndoStateExtension {
        public static UndoWrapper<TState, TChoice, TQuality> NoUndo<TState, TChoice, TQuality>(this IMutableState<TState, TChoice, TQuality> state)
        where TState : class, IMutableState<TState, TChoice, TQuality>
        where TQuality : struct, IQuality<TQuality> {
            return new UndoWrapper<TState, TChoice, TQuality>((TState)state);
        }
    }

    public class UndoWrapper<TState, TChoice, TQuality> : IState<UndoWrapper<TState, TChoice, TQuality>, TQuality>
        where TState : class, IMutableState<TState, TChoice, TQuality>
        where TQuality : struct, IQuality<TQuality> {

        private TState undoState;

        internal UndoWrapper(TState undoState) {
            if (undoState == null) throw new ArgumentNullException(nameof(undoState));
            this.undoState = undoState;
        }
        
        public TQuality Bound => undoState.Bound;
        public TQuality? Quality => undoState.Quality;

        public IEnumerable<UndoWrapper<TState, TChoice, TQuality>> GetBranches()
        {
            foreach (var choice in undoState.GetChoices()) {
                var clone = (TState)undoState.Clone();
                clone.Apply(choice);
                yield return new UndoWrapper<TState, TChoice, TQuality>(clone);
            }
        }

        public object Clone()
        {
            return new UndoWrapper<TState, TChoice, TQuality>((TState)undoState.Clone());
        }

        public override string ToString() => undoState.ToString();
        public override int GetHashCode() => undoState.GetHashCode();
        public override bool Equals(object obj) => undoState.Equals(obj);
    }
}