using System;
using System.Collections.Generic;
using System.Linq;
using TreesearchLib;

namespace SampleApp
{
    class ChooseSmallestProblem : IUndoState<ChooseSmallestProblem, int, Minimize>
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

        public bool IsSolved => choicesMade.Count == size;

        public Minimize Bound => new Minimize(choicesMade.Peek() + (size - choicesMade.Count));

        public Minimize? Quality => IsSolved ? new Minimize(choicesMade.Peek()) : new Minimize?();

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
            for (int i = 0; i < rng.Next(minChoices, maxChoices); i++)
            {
                yield return rng.Next(current + 1, current + maxDistance);
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
    }
}