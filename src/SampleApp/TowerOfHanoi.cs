using System.Collections.Generic;
using TreesearchLib;
using System.Linq;

namespace SampleApp
{
    public class TowerOfHanoi : IMutableState<TowerOfHanoi, (int, int), Minimize>
    {
        private readonly int _numberOfTowers = 3;
        private readonly int _numberOfDisks = 3;

        private Stack<int>[] _towers;
        private Stack<(int, int)> _moves;
        public IEnumerable<(int, int)> Moves => _moves.Reverse();
        public IEnumerable<IEnumerable<int>> Towers => _towers.Select(x => x.Reverse());

        public bool IsTerminal => _towers[_numberOfTowers - 1].Count == _numberOfDisks;

        public Minimize Bound => new Minimize(_moves.Count);

        public Minimize? Quality => IsTerminal ? Bound : null;

        public void Apply((int, int) choice)
        {
            _moves.Push(choice);
            _towers[choice.Item2].Push(_towers[choice.Item1].Pop());
        }

        public void UndoLast()
        {
            var choice = _moves.Pop();
            _towers[choice.Item1].Push(_towers[choice.Item2].Pop());
        }

        public TowerOfHanoi(int towers, int disks)
        {
            _numberOfTowers = towers;
            _numberOfDisks = disks;
            _towers = new Stack<int>[_numberOfTowers];
            for (int i = 0; i < _numberOfTowers; i++)
            {
                _towers[i] = new Stack<int>();
            }
            for (int i = _numberOfDisks; i > 0; i--)
            {
                _towers[0].Push(i);
            }
            _moves = new Stack<(int, int)>();
        }
        public TowerOfHanoi(TowerOfHanoi other)
        {
            _towers = new Stack<int>[_numberOfTowers];
            for (int i = 0; i < _numberOfTowers; i++)
            {
                _towers[i] = new Stack<int>(other._towers[i].Reverse());
            }
            _moves = new Stack<(int, int)>(other._moves.Reverse());
        }

        public object Clone()
        {
            return new TowerOfHanoi(this);
        }

        public IEnumerable<(int, int)> GetChoices()
        {
            for (int i = 0; i < _numberOfTowers; i++)
            {
                for (int j = 0; j < _numberOfTowers; j++)
                {
                    if (i != j && _towers[i].Count > 0 && (_towers[j].Count == 0 || _towers[i].Peek() < _towers[j].Peek()))
                    {
                        if (_moves.Count > 0 && _moves.Peek().Item1 == j && _moves.Peek().Item2 == i)
                        {
                            continue;
                        }
                        yield return (i, j);
                    }
                }
            }
        }
    }
}