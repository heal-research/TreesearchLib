using System;
using System.Collections.Generic;
using System.Linq;

namespace TreesearchLib
{
    /// <summary>
    /// The flags indicate the type of error that is likely to have occurred.
    /// In all error cases, the problem could also be in GetChoices(). This is impossible to detect separtely.
    /// 
    /// Ok: The test succeeded, so the state is probably correct.
    /// 
    /// Inconclusive: The test was inconclusive, the correctness cannot be determined.
    /// 
    /// CloningProblem: The test failed because the state likely does not implement cloning correctly.
    /// 
    /// UndoProblem: The test failed because the state likely does not implement apply/undo correctly.
    /// 
    /// ComparerProblem: The test failed because the comparer could be incorrect.
    /// 
    /// SequenceProblem: The test failed because the sequence of GetChoices() or GetBranches() is different.
    /// </summary>
    [Flags]
    public enum TestResult {
        Ok = 0,
        Inconclusive = 1,
        CloningProblem = 2,
        UndoProblem = 4,
        ComparerProblem = 8,
        SequenceProblem = 16
    };
    public static class Validator
    {
        /// <summary>
        /// This test checks whether the two functionalities: cloning and apply/undo are implemented
        /// correctly.
        /// 
        /// The first part of the test performs the same randomly drawn moves on the state as well as
        /// a clone of the state. Each time it checks whether the obtained choices are the same.
        /// In the second part, all moves are undone. After each undo again it is checked that the
        /// choices obtained from GetChoices <seealso cref="IMutableState.GetChoices()"/> are exactly
        /// as they had been before the apply.
        /// 
        /// If the test succeeds, it is not guaranteed that the implementation is correct, but a lot of
        /// potential errors can be detected.
        /// </summary>
        /// <remarks>
        /// The test assumes that at least there is at least one move that can be made,
        /// so <paramref name="state"/> should not be in a terminal state.
        /// 
        /// Also, it assumes that the state and choice generation is deterministic, which is a basic
        /// assumption of many algorithms. The GetChoices method needs to return the same choices
        /// in the same order every time two states are supposed to be equal.
        /// </remarks>
        /// <param name="state">The state (not a terminal one) that should be checked</param>
        /// <param name="comparer">The comparer that checks whether two choices are equal</param>
        /// <returns>Whether the cloning is correct according to the equality comparer provided</returns>
        public static TestResult Test<T, C, Q>(this T state, IEqualityComparer<C> comparer)
            where T : class, IMutableState<T, C, Q>
            where Q : struct, IQuality<Q>
        {
            var depth = 0;
            try
            {
                var expectedChoices = new Stack<List<C>>();
                var random = new System.Random(13); // just to avoid to take always the first decision
                var clone = (T)state.Clone(); // cloning at the initial level

                if (!comparer.Equals(state.GetChoices().First(), clone.GetChoices().First()))
                {
                    return TestResult.CloningProblem | TestResult.ComparerProblem;
                }

                while (depth < 1000)
                {
                    var choices = state.GetChoices().ToList();
                    var clonedChoices = clone.GetChoices().ToList();

                    if (choices.Count == 0 || clonedChoices.Count == 0 || state.IsTerminal || clone.IsTerminal)
                    {
                        if (choices.Count != clonedChoices.Count || state.IsTerminal != clone.IsTerminal)
                        {
                            return TestResult.CloningProblem;
                        }
                        break;
                    }
                    if (!choices.SequenceEqual(clonedChoices, comparer))
                    {
                        var result = TestResult.CloningProblem;
                        if (new HashSet<C>(choices, comparer).SetEquals(clonedChoices))
                        {
                            result |= TestResult.SequenceProblem;
                        }
                        return result;
                    }

                    expectedChoices.Push(choices);

                    var index = random.Next(choices.Count);
                    state.Apply(choices[index]);
                    clone.Apply(clonedChoices[index]);
                    depth++;
                }
                if (depth == 0)
                {
                    return TestResult.Inconclusive; // a terminal state was provided
                }
                clone = (T)state.Clone(); // cloning at a terminal level
                while (depth > 0)
                {
                    state.UndoLast();
                    clone.UndoLast();
                    depth--;
                    var choices = state.GetChoices().ToList();
                    var clonedChoices = clone.GetChoices().ToList();
                    var expected = expectedChoices.Pop();
                    if (!choices.SequenceEqual(expected, comparer))
                    {
                        var result = TestResult.UndoProblem;
                        if (new HashSet<C>(choices, comparer).SetEquals(expected))
                        {
                            result |= TestResult.SequenceProblem;
                        }
                        return result;
                    }
                    if (!clonedChoices.SequenceEqual(expected, comparer))
                    {
                        var result = TestResult.CloningProblem;
                        if (new HashSet<C>(clonedChoices, comparer).SetEquals(expected))
                        {
                            result |= TestResult.SequenceProblem;
                        }
                        return result;
                    }
                }
                return TestResult.Ok;
            }
            finally
            {
                while (depth > 0)
                {
                    state.UndoLast();
                    depth--;
                }
            }
        }
        /// <summary>
        /// This test checks whether the branches obtained from the given instance are identical
        /// to the branches obtained from a clone of that instance.
        /// </summary>
        /// <remarks>
        /// The test assumes that at least there is at least one move that can be made,
        /// so <paramref name="state"/> should not be in a terminal state.
        /// </remarks>
        /// <param name="state">The state (not a terminal one) that should be checked</param>
        /// <param name="comparer">The comparer that checks whether two states are equal</param>
        /// <returns>Whether the cloning is correct according to the equality comparer provided</returns>
        public static TestResult Test<T, Q>(this T state, IEqualityComparer<T> comparer)
            where T : IState<T, Q>
            where Q : struct, IQuality<Q>
        {
            var depth = 0;
            var random = new System.Random(13); // just to avoid to take always the first decision
            var clone = (T)state.Clone();
            if (!comparer.Equals(state, clone))
            {
                return TestResult.CloningProblem | TestResult.ComparerProblem;
            }

            while (depth < 1000)
            {
                var choices = state.GetBranches().ToList();
                var clonedChoices = clone.GetBranches().ToList();

                if (choices.Count == 0 || clonedChoices.Count == 0 || state.IsTerminal || clone.IsTerminal)
                {
                    if (choices.Count != clonedChoices.Count || state.IsTerminal != clone.IsTerminal)
                    {
                        return TestResult.CloningProblem;
                    }
                    break;
                }
                if (!choices.SequenceEqual(clonedChoices, comparer))
                {
                    var result = TestResult.CloningProblem;
                    if (new HashSet<T>(choices, comparer).SetEquals(clonedChoices))
                    {
                        result |= TestResult.SequenceProblem;
                    }
                    return result;
                }

                var index = random.Next(choices.Count);
                state = choices[index];
                clone = clonedChoices[index];
                depth++;
            }
            if (depth == 0)
            {
                return TestResult.Inconclusive;
            }
            return TestResult.Ok;
        }
    }
}