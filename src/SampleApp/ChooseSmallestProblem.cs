using System;
using System.Collections.Generic;
using System.Linq;
using TreesearchLib;

namespace SampleApp
{
    class ChooseSmallestProblem : IMutableState<ChooseSmallestProblem, int, Minimize>
    {
        public const int minChoices = 2;
        public const int maxChoices = 10;
        public const int maxDistance = 50;
        private int size;
        private Stack<int> choicesMade;

        public ChooseSmallestProblem(int size)
        {
            this.size = size;
            choicesMade = new Stack<int>();
        }

        public bool IsTerminal => choicesMade.Count == size;

        public Minimize Bound => new Minimize(choicesMade.Peek() + (size - choicesMade.Count));

        public Minimize? Quality => IsTerminal ? new Minimize(choicesMade.Peek()) : null;

        public void Apply(int choice)
        {
            choicesMade.Push(choice);
        }


        public object Clone()
        {
            var clone = new ChooseSmallestProblem(size);
            clone.choicesMade = new Stack<int>(choicesMade.Reverse());
            return clone;
        }

        public IEnumerable<int> GetChoices()
        {
            if (choicesMade.Count >= size)
            {
                yield break;
            }
            var current = 0;
            if (choicesMade.Count > 0)
            {
                current = choicesMade.Peek();
            }
            var rng = new Random(current);
            var chosen = new HashSet<int>();
            for (int i = 0; i < rng.Next(minChoices, maxChoices); i++)
            {
                var choice = rng.Next(current + 1, current + maxDistance);
                if (chosen.Add(choice))
                {
                    yield return choice;
                }
            }

        }

        public void UndoLast()
        {
            choicesMade.Pop();
        }

        public override string ToString()
        {
            return $"ChooseSmallestProblem [{string.Join(", ", this.choicesMade.Reverse())}]";
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ChooseSmallestProblem other))
            {
                return false;
            }
            return this.size == other.size && this.choicesMade.SequenceEqual(other.choicesMade);
        }
    }
}