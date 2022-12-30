using System;
using System.Collections.Generic;
using System.Linq;
using TreesearchLib;

namespace SampleApp
{
    public class TSP : IMutableState<TSP, int, Minimize>
    {
        public int N { get; private set; }
        public int[,] Distances { get; private set; }
        public int TourLength { get; private set; } = 0;

        public HashSet<int> Remaining { get; private set; }
        public int[] Tour { get; private set; }
        public int Index => N - Remaining.Count;

        public bool IsTerminal => Index == N;

        private Minimize? cachedBound;
        public Minimize Bound
        {
            get
            {
                if (!cachedBound.HasValue)
                {
                    cachedBound = new Minimize(TourLength + (Remaining.Count > 0 ? Remaining.Min(v => Distances[Tour[Index-1], v]) : 0));
                }
                return cachedBound.Value;
            }
        }

        public Minimize? Quality => IsTerminal ? Bound : (Minimize?)null;

        public TSP(int[,] distances)
        {
            N = distances.GetLength(0);
            Distances = distances;
            Remaining = new HashSet<int>(Enumerable.Range(1, N - 1));
            Tour = new int[N];
        }
        private TSP(TSP other) {
            N = other.N;
            Distances = other.Distances;
            TourLength = other.TourLength;
            Remaining = new HashSet<int>(other.Remaining);
            Tour = new int[N];
            Array.Copy(other.Tour, Tour, N);
        }

        public void Apply(int choice)
        {
            cachedBound = null;
            Tour[Index] = choice;
            TourLength += Distances[Tour[Index-1], Tour[Index]];
            Remaining.Remove(choice);
            if (IsTerminal)
            {
                TourLength += Distances[Tour[Index-1], Tour[0]];
            }
        }

        public void UndoLast()
        {
            cachedBound = null;
            var choice = Tour[Index - 1];
            if (IsTerminal)
            {
                TourLength -= Distances[Tour[Index-1], Tour[0]];
            }
            Remaining.Add(choice);
            TourLength -= Distances[Tour[Index-1], Tour[Index]];
            Tour[Index] = 0;
        }

        public object Clone()
        {
            return new TSP(this);
        }

        public IEnumerable<int> GetChoices()
        {
            return Remaining.Select(v => (City: v, Distance: Distances[Tour[Index-1], v])).OrderBy(x => x.Distance).Select(x => x.City);
        }
    }

    public static class Berlin52
    {
        public static int N => 52;
        public static int[,] Coords => new int[52, 2]
        {
           {  565,  575 },
           {   25,  185 },
           {  345,  750 },
           {  945,  685 },
           {  845,  655 },
           {  880,  660 },
           {   25,  230 },
           {  525, 1000 },
           {  580, 1175 },
           {  650, 1130 },
           { 1605,  620 }, 
           { 1220,  580 },
           { 1465,  200 },
           { 1530,    5 },
           {  845,  680 },
           {  725,  370 },
           {  145,  665 },
           {  415,  635 },
           {  510,  875 },  
           {  560,  365 },
           {  300,  465 },
           {  520,  585 },
           {  480,  415 },
           {  835,  625 },
           {  975,  580 },
           { 1215,  245 },
           { 1320,  315 },
           { 1250,  400 },
           {  660,  180 },
           {  410,  250 },
           {  420,  555 },
           {  575,  665 },
           { 1150, 1160 },
           {  700,  580 },
           {  685,  595 },
           {  685,  610 },
           {  770,  610 },
           {  795,  645 },
           {  720,  635 },
           {  760,  650 },
           {  475,  960 },
           {   95,  260 },
           {  875,  920 },
           {  700,  500 },
           {  555,  815 },
           {  830,  485 },
           { 1170,   65 },
           {  830,  610 },
           {  605,  625 },
           {  595,  360 },
           { 1340,  725 },
           { 1740,  245 },
        };

        public static int[,] GetDistances()
        {
            var distances = new int[N, N];
            for (var i = 0; i < N; i++)
                for (var j = 0; j < N; j++)
                {
                    if (i == j) continue;
                    distances[i, j] = (int)Math.Round(Math.Sqrt((Coords[i, 0] - Coords[j, 0]) * (Coords[i, 0] - Coords[j, 0])
                        + (Coords[i, 1] - Coords[j, 1]) * (Coords[i, 1] - Coords[j, 1])));
                }
            return distances;
        }
    }
}