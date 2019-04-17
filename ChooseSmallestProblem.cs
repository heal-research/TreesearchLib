using System;
using System.Collections.Generic;
using System.Linq;

namespace Treesearch
{
    class ChooseSmallestProblem : ISearchableReversible<int>, ICloneable
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

        public Quality LowerBound => new Quality(choicesMade.Peek() + (size - choicesMade.Count));

        public Quality? Quality => IsSolved ? new Quality(choicesMade.Peek()) : new Quality?();

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

        public ChoiceType ConstructOrChange() => ChoiceType.Construct;

        public void FillChoices(List<int> choices)
        {
            choices.Clear();
            var current = 0;
            if (this.choicesMade.Count > 0)
            {
                current = this.choicesMade.Peek();
            }
            if (this.choicesMade.Count > size)
            {
                return;
            }
            var rng = new Random(current);
            for (int i = 0; i < rng.Next(minChoices, maxChoices); i++)
            {
                choices.Add(rng.Next(current + 1, current + maxDistance));
            }

        }

        public int ChoicesMade => choicesMade.Count;

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