using System;
using System.Collections.Generic;

namespace TreesearchLib {

    public static class UndoStateExtension {
        public static UndoWrapper<TUndoState, TChoice, TQuality> Wrap<TUndoState, TChoice, TQuality>(this IUndoState<TUndoState, TChoice, TQuality> undoState)
        where TUndoState : class, IUndoState<TUndoState, TChoice, TQuality>
        where TQuality : struct, IQuality<TQuality> {
            return new UndoWrapper<TUndoState, TChoice, TQuality>((TUndoState)undoState);
        }
    }

    public class UndoWrapper<TUndoState, TChoice, TQuality> : IState<UndoWrapper<TUndoState, TChoice, TQuality>, TQuality>
        where TUndoState : class, IUndoState<TUndoState, TChoice, TQuality>
        where TQuality : struct, IQuality<TQuality> {

        private TUndoState undoState;

        internal UndoWrapper(TUndoState undoState) {
            if (undoState == null) throw new ArgumentNullException(nameof(undoState));
            this.undoState = undoState;
        }
        
        public TQuality Bound => undoState.Bound;
        public TQuality? Quality => undoState.Quality;

        public IEnumerable<UndoWrapper<TUndoState, TChoice, TQuality>> GetBranches()
        {
            foreach (var choice in undoState.GetChoices()) {
                var clone = (TUndoState)undoState.Clone();
                clone.Apply(choice);
                yield return new UndoWrapper<TUndoState, TChoice, TQuality>(clone);
            }
        }

        public object Clone()
        {
            return new UndoWrapper<TUndoState, TChoice, TQuality>((TUndoState)undoState.Clone());
        }
    }
}