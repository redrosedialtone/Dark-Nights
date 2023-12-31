﻿using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DarkNights
{
    public class AStarTiles
    {
        private NLog.Logger log => NavSys.log;

        private readonly SimplePriorityQueue<Coordinates> OpenList = new SimplePriorityQueue<Coordinates>();
        private readonly List<Coordinates> ClosedList = new List<Coordinates>();
        private readonly Dictionary<Coordinates, Coordinates> Trace = new Dictionary<Coordinates, Coordinates>();

        private readonly Dictionary<Coordinates, float> Weights = new Dictionary<Coordinates, float>();
        private readonly Dictionary<Coordinates, float> HeuristicScores = new Dictionary<Coordinates, float>();

        private readonly Coordinates Start;
        private readonly Coordinates End;
        private Coordinates Current;

        public AStarTiles(Coordinates Start, Coordinates End)
        {
            this.Start = Start;
            this.End = End;
        }

        public Stack<Coordinates> Path()
        {
            log.Debug("Generating Heuristic Path...");
            OpenList.Enqueue(Start, 0);
            Weights[Start] = 0;
            HeuristicScores[Start] = HeuristicWeight((Start.X, Start.Y), (End.X, End.Y));

            bool targetReached = false;
            int nodesTraversed = 0;
            while (OpenList.Count > 0)
            {
                nodesTraversed++;
                Current = OpenList.Dequeue();
                // We did it!
                if (Current == End)
                {
                    log.Debug("Located Path Target");
                    targetReached = true;
                    break;
                }

                ClosedList.Add(Current);

                foreach (var edge in Current.Adjacent)
                {
                    if (NavSys.Get.TryGetNode(edge, out INavNode node))
                    {
                        if(node.Passability == PassabilityFlags.Impassable)
                        {
                            if (!ClosedList.Contains(edge)) ClosedList.Add(edge); continue;
                        }
                    }
                    if (ClosedList.Contains(edge)) continue;

                    float weight = Weights[Current] + 1.0f;
                    bool alreadyOpen = OpenList.Contains(edge);
                    if (alreadyOpen && weight >= Weights[edge]) continue;

                    if (Trace.ContainsKey(edge) == false) Trace.Add(edge, Current);
                    else Trace[edge] = Current;
                    if (Weights.ContainsKey(edge) == false) Weights.Add(edge, weight);
                    else Weights[edge] = weight;

                    float heuristicWeight = weight + HeuristicWeight((edge.X, edge.Y), (End.X, End.Y));
                    if (HeuristicScores.ContainsKey(edge) == false) HeuristicScores.Add(edge, heuristicWeight);
                    else HeuristicScores[edge] = heuristicWeight;

                    if (!alreadyOpen)
                    {
                        OpenList.Enqueue(edge, HeuristicScores[edge]);
                    }
                }
            }

            if (targetReached)
            {
                log.Debug($"Path Located ({nodesTraversed}) after {nodesTraversed} nodes.");
                Stack<Coordinates> Path = new Stack<Coordinates>();
                do
                {
                    Path.Push(Current);
                    Current = Trace[Current];
                } while (Current != Start);
                return Path;
            }
            log.Debug("No Path Found");
            return null;
        }

        private float HeuristicWeight((int X, int Y) Start, (int X, int Y) End)
        {
            return MathF.Sqrt(
                MathF.Pow(Start.X - End.X, 2) +
                MathF.Pow(Start.Y - End.Y, 2));
        }

        private float Distance((int X, int Y) Start, (int X, int Y) End)
        {
            float xDif = MathF.Abs(Start.X - End.X);
            float yDif = MathF.Abs(Start.Y - End.Y);
            if (xDif + yDif == 1) return 1f;
            else if (xDif == 1 && yDif == 1) return 1.414213562373f;
            else
            {
                return HeuristicWeight(Start, End);
            }
        }


    }

    public class AStarHierarchal
    {
        private NLog.Logger log => NavSys.log;
        private readonly SimplePriorityQueue<IGraph> OpenList = new SimplePriorityQueue<IGraph>();
        private readonly List<IGraph> ClosedList = new List<IGraph>();
        private readonly Dictionary<IGraph, IGraph> Trace = new Dictionary<IGraph, IGraph>();

        private readonly Dictionary<IGraph, float> Weights = new Dictionary<IGraph, float>();
        private readonly Dictionary<IGraph, float> HeuristicScores = new Dictionary<IGraph, float>();

        private readonly Coordinates Start;
        private readonly Coordinates End;
        private IGraph CurrentGraph;

        public AStarHierarchal(Coordinates Start, Coordinates End)
        {
            this.Start = Start;
            this.End = End;
        }
    }
}
