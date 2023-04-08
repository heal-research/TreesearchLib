using System;

namespace TreesearchLib
{
    public delegate void Lookahead<T, Q>(ISearchControl<T, Q> control, T state)
        where T : IState<T, Q>
        where Q : struct, IQuality<Q>;
    public delegate void Lookahead<T, C, Q>(ISearchControl<T, Q> control, T state)
        where T : class, IMutableState<T, C, Q>
        where Q : struct, IQuality<Q>;
    
    public static class LA
    {
        public static Lookahead<T, Q> DFSLookahead<T, Q>(int filterWidth = 1, int depthLimit = int.MaxValue, int backtrackLimit = 0)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return (control, state) =>
            {
                Algorithms.DepthSearch<T, Q>(control, state, depth: 0, backtracks: 0, filterWidth, depthLimit, backtrackLimit);
            };
        }

        public static Lookahead<T, C, Q> DFSLookahead<T, C, Q>(int filterWidth = 1, int depthLimit = int.MaxValue, int backtrackLimit = 0)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return (control, state) =>
            {
                var (depth, _) = Algorithms.DepthSearch<T, C, Q>(control, state, depth: 0, backtracks: 0, filterWidth, depthLimit, backtrackLimit);
                // restore state after the lookahead
                while (depth > 0)
                {
                    state.UndoLast();
                    depth--;
                }
            };
        }

        public static Lookahead<T, Q> BeamSearchLookahead<T, Q>(int beamWidth, Func<T, float> rank, int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return (control, state) =>
            {
                Heuristics.BeamSearch<T, Q>(control, state, beamWidth, rank, filterWidth, depthLimit);
            };
        }

        public static Lookahead<T, C, Q> BeamSearchLookahead<T, C, Q>(int beamWidth, Func<T, float> rank, int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return (control, state) =>
            {
                Heuristics.BeamSearch<T, C, Q>(control, state, 0, beamWidth, rank, filterWidth, depthLimit);
            };
        }

        public static Lookahead<T, Q> MonoBeamSearchLookahead<T, Q>(int beamWidth, Func<T, float> rank, int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return (control, state) =>
            {
                Heuristics.MonotonicBeamSearch<T, Q>(control, state, beamWidth, rank, filterWidth);
            };
        }

        public static Lookahead<T, C, Q> MonoBeamSearchLookahead<T, C, Q>(int beamWidth, Func<T, float> rank, int filterWidth = int.MaxValue, int depthLimit = int.MaxValue)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return (control, state) =>
            {
                Heuristics.MonotonicBeamSearch<T, C, Q>(control, state, beamWidth, rank, filterWidth);
            };
        }

        public static Lookahead<T, Q> RakeSearchLookahead<T, Q>(int rakeWidth, Lookahead<T, Q> innerLookahead)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return (control, state) =>
            {
                Heuristics.RakeSearch<T, Q>(control, state, rakeWidth, innerLookahead, iterations: 1);
            };
        }

        public static Lookahead<T, C, Q> RakeSearchLookahead<T, C, Q>(int rakeWidth, Lookahead<T, C, Q> innerLookahead)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return (control, state) =>
            {
                Heuristics.RakeSearch<T, C, Q>(control, state, rakeWidth, innerLookahead, iterations: 1);
            };
        }

        public static Lookahead<T, Q> LDSearchLookahead<T, Q>(int maxDiscrepancy)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            return (control, state) =>
            {
                Heuristics.AnytimeLDSearch<T, Q>(control, state, maxDiscrepancy);
            };
        }

        public static Lookahead<T, C, Q> LDSearchLookahead<T, C, Q>(int maxDiscrepancy)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            return (control, state) =>
            {
                Heuristics.AnytimeLDSearch<T, C, Q>(control, (T)state.Clone(), maxDiscrepancy);
            };
        }
    }
}