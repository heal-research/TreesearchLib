using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace TreesearchLib
{
    public delegate void QualityCallback<TState, TQuality>(ISearchControl<TState, TQuality> control, TState state, TQuality quality)
        where TState : class, IQualifiable<TQuality>
        where TQuality : struct, IQuality<TQuality>;

    public interface ISearchControl<TState, TQuality>
        where TState : class, IQualifiable<TQuality> 
        where TQuality : struct, IQuality<TQuality> {
        TState InitialState { get; }
        TQuality? BestQuality { get; }
        TState BestQualityState { get; }
        TimeSpan Elapsed { get; }
        TimeSpan Runtime { get; }
        CancellationToken Cancellation { get; }
        long NodeLimit { get; }
        long VisitedNodes { get; }
        bool IsFinished { get; }

        bool ShouldStop();
        void VisitNode(TState state);
    }

    public class SearchControlUndo<TState, TChoice, TQuality> : ISearchControl<TState, TQuality>
        where TState : class, IUndoState<TState, TChoice, TQuality>
        where TQuality : struct, IQuality<TQuality> 
    {
        private SearchControlUndo(TState state)
        {
            stopwatch = Stopwatch.StartNew();
            InitialState = state;
            BestQuality = null;
            BestQualityState = null;
            Cancellation = CancellationToken.None;
            Runtime = TimeSpan.MaxValue;
            NodeLimit = long.MaxValue;
            VisitedNodes = 0;
        }

        private Stopwatch stopwatch;
        
        public QualityCallback<TState, TQuality> ImprovementCallback { get; set; }

        public TState InitialState { get; set; }
        public TQuality? BestQuality { get; set; }
        public TState BestQualityState { get; set; }

        public TimeSpan Elapsed => stopwatch.Elapsed;
        public TimeSpan Runtime { get; set; }
        public CancellationToken Cancellation { get; set; }
        public long NodeLimit { get; set; }
        public long VisitedNodes { get; private set; }

        public bool IsFinished => !stopwatch.IsRunning;

        public SearchControlUndo<TState, TChoice, TQuality> Finish()
        {
            stopwatch.Stop();
            return this;
        }

        public bool ShouldStop()
        {
            if (IsFinished || Cancellation.IsCancellationRequested || stopwatch.Elapsed > Runtime
                || VisitedNodes >= NodeLimit)
            {
                return true;
            }

            return false;
        }

        public void VisitNode(TState state)
        {
            VisitedNodes++;

            if (state.Quality.HasValue)
            {
                FoundSolution(state);
            }
        }

        private void FoundSolution(TState state)
        {
            if (!state.Quality.HasValue) throw new ArgumentException("state is not a full solution, quality is null");
            var quality = state.Quality.Value;
            if (quality.IsBetter(BestQuality))
            {
                BestQuality = quality;
                BestQualityState = (TState)state.Clone();
                ImprovementCallback?.Invoke(this, state, quality);
            }
        }

        public static SearchControlUndo<TState, TChoice, TQuality> Start(IUndoState<TState, TChoice, TQuality> state)
        {
            return new SearchControlUndo<TState, TChoice, TQuality>((TState)state);
        }
    }

    public class SearchControl<TState, TQuality> : ISearchControl<TState, TQuality>
        where TState : class, IState<TState, TQuality>
        where TQuality : struct, IQuality<TQuality>
    {
        private SearchControl(TState state)
        {
            stopwatch = Stopwatch.StartNew();
            InitialState = state;
            BestQuality = null;
            BestQualityState = null;
            Cancellation = CancellationToken.None;
            Runtime = TimeSpan.MaxValue;
            VisitedNodes = 0;
        }

        private Stopwatch stopwatch;
        
        public QualityCallback<TState, TQuality> ImprovementCallback { get; set; }

        public TState InitialState { get; set; }
        public TQuality? BestQuality { get; set; }
        public TState BestQualityState { get; set; }

        public TimeSpan Elapsed => stopwatch.Elapsed;
        public TimeSpan Runtime { get; set; }
        public CancellationToken Cancellation { get; set; }
        public long NodeLimit { get; set; }
        public long VisitedNodes { get; private set; }

        public bool IsFinished => !stopwatch.IsRunning;

        public SearchControl<TState, TQuality> Finish()
        {
            stopwatch.Stop();
            return this;
        }

        public bool ShouldStop()
        {
            if (IsFinished || Cancellation.IsCancellationRequested || stopwatch.Elapsed > Runtime
                || VisitedNodes >= NodeLimit)
            {
                return true;
            }

            return false;
        }

        public void VisitNode(TState state)
        {
            VisitedNodes++;

            if (state.Quality.HasValue)
            {
                FoundSolution(state);
            }
        }

        private void FoundSolution(TState state)
        {
            if (!state.Quality.HasValue) throw new ArgumentException("state is not a full solution, quality is null");
            var quality = state.Quality.Value;
            if (quality.IsBetter(BestQuality))
            {
                BestQuality = quality;
                BestQualityState = (TState)state.Clone();
                ImprovementCallback?.Invoke(this, state, quality);
            }
        }

        public static SearchControl<TState, TQuality> Start(TState state)
        {
            return new SearchControl<TState, TQuality>(state);
        }
    }

    public static class SearchControlExtensions
    {
        public static SearchControl<TState, TQuality> WithImprovementCallback<TState, TQuality>(this SearchControl<TState, TQuality> control, QualityCallback<TState, TQuality> callback)
            where TState : class, IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.ImprovementCallback = callback;
            return control;
        }
        public static SearchControlUndo<TState, TChoice, TQuality> WithImprovementCallback<TState, TChoice, TQuality>(this SearchControlUndo<TState, TChoice, TQuality> control, QualityCallback<TState, TQuality> callback)
            where TState : class, IUndoState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.ImprovementCallback = callback;
            return control;
        }

        public static SearchControl<TState, TQuality> WithCancellationToken<TState, TQuality>(this SearchControl<TState, TQuality> control, CancellationToken token)
            where TState : class, IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.Cancellation = token;
            return control;
        }

        public static SearchControlUndo<TState, TChoice, TQuality> WithCancellationToken<TState, TChoice, TQuality>(this SearchControlUndo<TState, TChoice, TQuality> control, CancellationToken token)
            where TState : class, IUndoState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.Cancellation = token;
            return control;
        }

        public static SearchControl<TState, TQuality> WithUpperBound<TState, TQuality>(this SearchControl<TState, TQuality> control, TQuality upperBound)
            where TState : class, IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.BestQuality = upperBound;
            return control;
        }

        public static SearchControlUndo<TState, TChoice, TQuality> WithUpperBound<TState, TChoice, TQuality>(this SearchControlUndo<TState, TChoice, TQuality> control, TQuality upperBound)
            where TState : class, IUndoState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.BestQuality = upperBound;
            return control;
        }

        public static SearchControl<TState, TQuality> WithRuntimeLimit<TState, TQuality>(this SearchControl<TState, TQuality> control, TimeSpan runtime)
            where TState : class, IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.Runtime = runtime;
            return control;
        }

        public static SearchControlUndo<TState, TChoice, TQuality> WithRuntimeLimit<TState, TChoice, TQuality>(this SearchControlUndo<TState, TChoice, TQuality> control, TimeSpan runtime)
            where TState : class, IUndoState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.Runtime = runtime;
            return control;
        }
    }
}