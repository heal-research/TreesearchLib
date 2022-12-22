using System;
using System.Diagnostics;
using System.Threading;

namespace TreesearchLib
{
    public delegate void QualityCallback<TState, TQuality>(ISearchControl<TState, TQuality> control, TState state, TQuality quality)
        where TState : IQualifiable<TQuality>
        where TQuality : struct, IQuality<TQuality>;

    public interface ISearchControl<TState, TQuality>
        where TState : IQualifiable<TQuality> 
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

    /// <summary>
    /// Search control class for mutable (reversible) states with a separate choice type
    /// </summary>
    /// <typeparam name="TState">The type that represents the state</typeparam>
    /// <typeparam name="TChoice">The type that represents the choice (each choice leads to a branch)</typeparam>
    /// <typeparam name="TQuality">The type that represents the quality</typeparam>
    public class SearchControl<TState, TChoice, TQuality> : ISearchControl<TState, TQuality>
        where TState : class, IMutableState<TState, TChoice, TQuality>
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

        public SearchControl<TState, TChoice, TQuality> Finish()
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

            var quality = state.Quality;
            if (quality.HasValue)
            {
                if (quality.Value.IsBetter(BestQuality))
                {
                    BestQuality = quality;
                    BestQualityState = (TState)state.Clone();
                    ImprovementCallback?.Invoke(this, state, quality.Value);
                }
            }
        }

        public static SearchControl<TState, TChoice, TQuality> Start(IMutableState<TState, TChoice, TQuality> state)
        {
            return new SearchControl<TState, TChoice, TQuality>((TState)state);
        }
    }

    /// <summary>
    /// Search control class for irreversible states
    /// </summary>
    /// <typeparam name="TState">The type that represents the state</typeparam>
    /// <typeparam name="TQuality">The type that represents the quality</typeparam>
    public class SearchControl<TState, TQuality> : ISearchControl<TState, TQuality>
        where TState : IState<TState, TQuality>
        where TQuality : struct, IQuality<TQuality>
    {
        private SearchControl(TState state)
        {
            stopwatch = Stopwatch.StartNew();
            InitialState = state;
            BestQuality = null;
            BestQualityState = default(TState);
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

            var quality = state.Quality;
            if (quality.HasValue)
            {
                if (quality.Value.IsBetter(BestQuality))
                {
                    BestQuality = quality;
                    BestQualityState = (TState)state.Clone();
                    ImprovementCallback?.Invoke(this, state, quality.Value);
                }
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
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.ImprovementCallback = callback;
            return control;
        }
        public static SearchControl<TState, TChoice, TQuality> WithImprovementCallback<TState, TChoice, TQuality>(this SearchControl<TState, TChoice, TQuality> control, QualityCallback<TState, TQuality> callback)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.ImprovementCallback = callback;
            return control;
        }

        public static SearchControl<TState, TQuality> WithCancellationToken<TState, TQuality>(this SearchControl<TState, TQuality> control, CancellationToken token)
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.Cancellation = token;
            return control;
        }

        public static SearchControl<TState, TChoice, TQuality> WithCancellationToken<TState, TChoice, TQuality>(this SearchControl<TState, TChoice, TQuality> control, CancellationToken token)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.Cancellation = token;
            return control;
        }

        public static SearchControl<TState, TQuality> WithUpperBound<TState, TQuality>(this SearchControl<TState, TQuality> control, TQuality upperBound)
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.BestQuality = upperBound;
            return control;
        }

        public static SearchControl<TState, TChoice, TQuality> WithUpperBound<TState, TChoice, TQuality>(this SearchControl<TState, TChoice, TQuality> control, TQuality upperBound)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.BestQuality = upperBound;
            return control;
        }

        public static SearchControl<TState, TQuality> WithRuntimeLimit<TState, TQuality>(this SearchControl<TState, TQuality> control, TimeSpan runtime)
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.Runtime = runtime;
            return control;
        }

        public static SearchControl<TState, TChoice, TQuality> WithRuntimeLimit<TState, TChoice, TQuality>(this SearchControl<TState, TChoice, TQuality> control, TimeSpan runtime)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.Runtime = runtime;
            return control;
        }

        public static SearchControl<TState, TQuality> WithNodeLimit<TState, TQuality>(this SearchControl<TState, TQuality> control, long nodelimit)
            where TState : IState<TState, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.NodeLimit = nodelimit;
            return control;
        }

        public static SearchControl<TState, TChoice, TQuality> WithNodeLimit<TState, TChoice, TQuality>(this SearchControl<TState, TChoice, TQuality> control, long nodelimit)
            where TState : class, IMutableState<TState, TChoice, TQuality>
            where TQuality : struct, IQuality<TQuality>
        {
            control.NodeLimit = nodelimit;
            return control;
        }
    }
}