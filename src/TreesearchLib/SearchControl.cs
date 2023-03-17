using System;
using System.Diagnostics;
using System.Threading;

namespace TreesearchLib
{
    public enum VisitResult { Ok = 0, Discard = 1 }
    public delegate void QualityCallback<TState, TQuality>(ISearchControl<TState, TQuality> control, TState state, TQuality quality)
        where TState : IQualifiable<TQuality>
        where TQuality : struct, IQuality<TQuality>;

    public interface ISearchControl<TState, TQuality>
        where TState : IQualifiable<TQuality> 
        where TQuality : struct, IQuality<TQuality> {
        /// <summary>
        /// The state from which the search should start (if not determined otherwise)
        /// </summary>
        /// <value></value>
        TState InitialState { get; }
        /// <summary>
        /// The best-found quality (or none, if no solution found)
        /// </summary>
        /// <value></value>
        TQuality? BestQuality { get; }
        /// <summary>
        /// The best-found state (or none, if no solution found)
        /// </summary>
        /// <value></value>
        TState BestQualityState { get; }
        /// <summary>
        /// The elapsed time so far
        /// </summary>
        /// <value></value>
        TimeSpan Elapsed { get; }
        /// <summary>
        /// Limits the elapsed time, <see cref="ShouldStop"/> returns true when Elapsed > Runtime.
        /// </summary>
        /// <value></value>
        TimeSpan Runtime { get; }
        /// <summary>
        /// Allows to cancel the search operation
        /// </summary>
        /// <value></value>
        CancellationToken Cancellation { get; }
        /// <summary>
        /// Limits the number of nodes that should be visited
        /// </summary>
        /// <value></value>
        long NodeLimit { get; }
        /// <summary>
        /// Tracks the number of visited nodes.
        /// </summary>
        /// <value></value>
        long VisitedNodes { get; }
        /// <summary>
        /// Whether the tracker has finished
        /// </summary>
        /// <value></value>
        bool IsFinished { get; }

        /// <summary>
        /// Function to check if a termination condition has been reached.
        /// </summary>
        /// <returns>True if the search should terminate</returns>
        bool ShouldStop();
        /// <summary>
        /// Performs the tracking of best quality and respective state.
        /// This function should be called for every visited node.
        /// </summary>
        /// <param name="state">The state that is visited</param>
        /// <returns>Whether the node is ok, or whether it should be discarded, because the lower bound is already worse than the best upper bound</returns>
        VisitResult VisitNode(TState state);
    }

    /// <summary>
    /// Search control class for mutable (reversible) states with a separate choice type
    /// </summary>
    /// <typeparam name="TState">The type that represents the state</typeparam>
    /// <typeparam name="TChoice">The type that represents the choice (each choice leads to a branch)</typeparam>
    /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
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

        public VisitResult VisitNode(TState state)
        {
            VisitedNodes++;

            var result = BestQuality.HasValue && !state.Bound.IsBetter(BestQuality.Value) ? VisitResult.Discard : VisitResult.Ok;

            var quality = state.Quality;
            if (quality.HasValue)
            {
                if (!BestQuality.HasValue || quality.Value.IsBetter(BestQuality.Value))
                {
                    BestQuality = quality;
                    BestQualityState = (TState)state.Clone();
                    ImprovementCallback?.Invoke(this, state, quality.Value);
                }
            }
            return result;
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
    /// <typeparam name="TQuality">The type of quality (Minimize, Maximize)</typeparam>
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

        public VisitResult VisitNode(TState state)
        {
            VisitedNodes++;

            var result = BestQuality.HasValue && !state.Bound.IsBetter(BestQuality.Value) ? VisitResult.Discard : VisitResult.Ok;

            var quality = state.Quality;
            if (quality.HasValue)
            {
                if (!BestQuality.HasValue || quality.Value.IsBetter(BestQuality.Value))
                {
                    BestQuality = quality;
                    BestQualityState = (TState)state.Clone();
                    ImprovementCallback?.Invoke(this, state, quality.Value);
                }
            }
            return result;
        }

        public static SearchControl<TState, TQuality> Start(TState state)
        {
            return new SearchControl<TState, TQuality>(state);
        }
    }

    public class WrappedSearchControl<TState, TQuality> : ISearchControl<TState, TQuality>
        where TState : IState<TState, TQuality>
        where TQuality : struct, IQuality<TQuality>
    {
        private readonly ISearchControl<TState, TQuality> control;

        public WrappedSearchControl(ISearchControl<TState, TQuality> control)
        {
            this.control = control;
        }

        public TState InitialState => control.InitialState;

        public TQuality? BestQuality { get; set; }

        public TState BestQualityState { get; set; }

        public TimeSpan Elapsed => control.Elapsed;

        public TimeSpan Runtime => control.Runtime;

        public CancellationToken Cancellation => control.Cancellation;

        public long NodeLimit => control.NodeLimit;

        public long VisitedNodes => control.VisitedNodes;

        public bool IsFinished => control.IsFinished;

        public bool ShouldStop()
        {
            return control.ShouldStop();
        }

        public VisitResult VisitNode(TState state)
        {
            var quality = state.Quality;
            if (quality.HasValue)
            {
                if (!BestQuality.HasValue || quality.Value.IsBetter(BestQuality.Value))
                {
                    BestQuality = quality;
                    BestQualityState = (TState)state.Clone();
                }
            }
            return control.VisitNode(state);
        }
    }

    public class WrappedThreadSafeSearchControl<TState, TQuality> : ISearchControl<TState, TQuality>
        where TState : IState<TState, TQuality>
        where TQuality : struct, IQuality<TQuality>
    {
        private static readonly object locker = new object();
        private readonly ISearchControl<TState, TQuality> control;

        public WrappedThreadSafeSearchControl(ISearchControl<TState, TQuality> control)
        {
            this.control = control;
        }

        public TState InitialState => control.InitialState;

        public TQuality? BestQuality { get; set; }

        public TState BestQualityState { get; set; }

        public TimeSpan Elapsed => control.Elapsed;

        public TimeSpan Runtime => control.Runtime;

        public CancellationToken Cancellation => control.Cancellation;

        public long NodeLimit => control.NodeLimit;

        public long VisitedNodes => control.VisitedNodes;

        public bool IsFinished => control.IsFinished;

        public bool ShouldStop()
        {
            return control.ShouldStop();
        }

        public VisitResult VisitNode(TState state)
        {
            lock (locker)
            {
                var quality = state.Quality;
                if (quality.HasValue)
                {
                    if (!BestQuality.HasValue || quality.Value.IsBetter(BestQuality.Value))
                    {
                        BestQuality = quality;
                        BestQualityState = (TState)state.Clone();
                    }
                }
                return control.VisitNode(state);
            }
        }
    }

    public class WrappedSearchControl<TState, TChoice, TQuality> : ISearchControl<TState, TQuality>
        where TState : class, IMutableState<TState, TChoice, TQuality>
        where TQuality : struct, IQuality<TQuality>
    {
        private readonly ISearchControl<TState, TQuality> control;

        public WrappedSearchControl(ISearchControl<TState, TQuality> control)
        {
            this.control = control;
        }

        public TState InitialState => control.InitialState;

        public TQuality? BestQuality { get; set; }

        public TState BestQualityState { get; set; }

        public TimeSpan Elapsed => control.Elapsed;

        public TimeSpan Runtime => control.Runtime;

        public CancellationToken Cancellation => control.Cancellation;

        public long NodeLimit => control.NodeLimit;

        public long VisitedNodes => control.VisitedNodes;

        public bool IsFinished => control.IsFinished;

        public bool ShouldStop()
        {
            return control.ShouldStop();
        }

        public VisitResult VisitNode(TState state)
        {
            var quality = state.Quality;
            if (quality.HasValue)
            {
                if (!BestQuality.HasValue || quality.Value.IsBetter(BestQuality.Value))
                {
                    BestQuality = quality;
                    BestQualityState = (TState)state.Clone();
                }
            }
            return control.VisitNode(state);
        }
    }

    public class WrappedThreadSafeSearchControl<TState, TChoice, TQuality> : ISearchControl<TState, TQuality>
        where TState : class, IMutableState<TState, TChoice, TQuality>
        where TQuality : struct, IQuality<TQuality>
    {
        private static readonly object locker = new object();
        private readonly ISearchControl<TState, TQuality> control;

        public WrappedThreadSafeSearchControl(ISearchControl<TState, TQuality> control)
        {
            this.control = control;
        }

        public TState InitialState => control.InitialState;

        public TQuality? BestQuality { get; set; }

        public TState BestQualityState { get; set; }

        public TimeSpan Elapsed => control.Elapsed;

        public TimeSpan Runtime => control.Runtime;

        public CancellationToken Cancellation => control.Cancellation;

        public long NodeLimit => control.NodeLimit;

        public long VisitedNodes => control.VisitedNodes;

        public bool IsFinished => control.IsFinished;

        public bool ShouldStop()
        {
            return control.ShouldStop();
        }

        public VisitResult VisitNode(TState state)
        {
            lock(locker)
            {
                var quality = state.Quality;
                if (quality.HasValue)
                {
                    if (!BestQuality.HasValue || quality.Value.IsBetter(BestQuality.Value))
                    {
                        BestQuality = quality;
                        BestQualityState = (TState)state.Clone();
                    }
                }
                return control.VisitNode(state);
            }
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