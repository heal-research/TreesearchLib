using System;
using System.Collections.Generic;

namespace TreesearchLib {

    public static class UndoStateExtension {
        /// <summary>
        /// Wraps the current state into one that does not allow to perform UndoLast() and thus requires that
        /// each state is to be cloned, before a move is to be applied.
        /// </summary>
        /// <param name="state">The state that should be wrapped</param>
        /// <typeparam name="TState">The state type</typeparam>
        /// <typeparam name="TChoice">The choice type</typeparam>
        /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
        /// <returns>The wrapped state instance</returns>
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

        public bool IsTerminal => undoState.IsTerminal;
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