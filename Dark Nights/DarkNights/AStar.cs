using Priority_Queue;
using System;
using System.Collections;
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

        public Stack<IGraph> Path(IGraph Start, IGraph End)
        {
            log.Debug("Generating Hierarchal Heuristic Path...");
            PriorityQueue<IGraph, float> OpenList = new PriorityQueue<IGraph, float>();
            List<IGraph> ClosedList = new List<IGraph>();
            Dictionary<IGraph, IGraph> Trace = new Dictionary<IGraph, IGraph>();
            Dictionary<IGraph, float> Weights = new Dictionary<IGraph, float>();
            Weights[Start] = 0;
            IGraph current = Start;
            bool targetReached = false;
            int nodesTraversed = 0;

            OpenList.Enqueue(Start, HeuristicWeight(Start, End));
            while (OpenList.Count > 0)
            {
                current = OpenList.Dequeue();
                ClosedList.Add(current);
                nodesTraversed++;

                // We did it!
                if (current == End)
                {
                    log.Debug("Located Path Target");
                    targetReached = true;
                    break;
                }

                foreach (var edge in current.Connections)
                {
                    var neighbour = edge.Cast;
                    if (ClosedList.Contains(neighbour)) continue;
                    float weight = Weights[current] + HeuristicWeight(neighbour, current);
                    bool alreadyOpen = false;
                    foreach (var oLNode in OpenList.UnorderedItems)
                    {
                        if (oLNode.Element == neighbour) alreadyOpen = true;
                    }
                    if (alreadyOpen && weight >= Weights[current]) continue;

                    if (Trace.ContainsKey(neighbour) == false) Trace.Add(neighbour, current);
                    else Trace[neighbour] = current;
                    if (Weights.ContainsKey(neighbour) == false) Weights.Add(neighbour, weight);
                    else Weights[neighbour] = weight;

                    float heuristicWeight = weight + HeuristicWeight(neighbour, End);

                    if (!alreadyOpen)
                    {
                        OpenList.Enqueue(neighbour, heuristicWeight);
                    }
                }
            }

            if (targetReached)
            {
                log.Debug($"Path Located after {nodesTraversed} nodes.");
                Stack<IGraph> Path = new Stack<IGraph>();
                do
                {
                    Path.Push(current);
                    current = Trace[current];
                } while (current != Start);
                return Path;
            }
            return null;
        }

            public Stack<Cluster> Path(Cluster Start, Cluster End)
        {
            log.Debug("Generating Hierarchal Heuristic Path...");
            PriorityQueue<Cluster, float> OpenList = new PriorityQueue<Cluster, float>();
            List<Cluster> ClosedList = new List<Cluster>();
            Dictionary<Cluster, Cluster> Trace = new Dictionary<Cluster, Cluster>();
            Dictionary<Cluster, float> Weights = new Dictionary<Cluster, float>();
            Weights[Start] = 0;
            Cluster current = Start;
            bool targetReached = false;
            int nodesTraversed = 0;

            OpenList.Enqueue(Start, HeuristicWeight(Start, End));

            while (OpenList.Count > 0)
            {
                current = OpenList.Dequeue();
                ClosedList.Add(current);
                nodesTraversed++;

                // We did it!
                if (current == End)
                {
                    log.Debug("Located Path Target");
                    targetReached = true;
                    break;
                }

                foreach (var neighbour in NavSys.Get.ClusterNeighbours(current))
                {
                    if (ClosedList.Contains(neighbour)) continue;
                    float weight = Weights[current] + HeuristicWeight(neighbour, current);
                    bool alreadyOpen = false;
                    foreach (var oLNode in OpenList.UnorderedItems)
                    {
                        if (oLNode.Element == neighbour) alreadyOpen = true;
                    }
                    if (alreadyOpen && weight >= Weights[current]) continue;

                    if (Trace.ContainsKey(neighbour) == false) Trace.Add(neighbour, current);
                    else Trace[neighbour] = current;
                    if (Weights.ContainsKey(neighbour) == false) Weights.Add(neighbour, weight);
                    else Weights[neighbour] = weight;

                    float heuristicWeight = weight + HeuristicWeight(neighbour, End);

                    if (!alreadyOpen)
                    {
                        OpenList.Enqueue(neighbour, heuristicWeight);
                    }
                }
            }

            if (targetReached)
            {
                log.Debug($"Path Located after {nodesTraversed} nodes.");
                Stack<Cluster> Path = new Stack<Cluster>();
                do
                {
                    Path.Push(current);
                    current = Trace[current];
                } while (current != Start);
                return Path;
            }
            return null;
        }

        public Stack<INavNode> Path(INavNode Start, INavNode End)
        {
            log.Debug("Generating Hierarchal Heuristic Path...");

            PriorityQueue<INavNode, float> OpenList = new PriorityQueue<INavNode, float>();
            List<INavNode> ClosedList = new List<INavNode>();
            Dictionary<INavNode, INavNode> Trace = new Dictionary<INavNode, INavNode>();
            Dictionary<INavNode, float> Weights = new Dictionary<INavNode, float>();
            Weights[Start] = 0;
            INavNode current = Start;
            bool targetReached = false;
            int nodesTraversed = 0;

            OpenList.Enqueue(Start,HeuristicWeight(Start, End));

            while (OpenList.Count > 0)
            {
                current = OpenList.Dequeue();
                ClosedList.Add(current);
                nodesTraversed++;

                // We did it!
                if (End.Neighbours.Contains(current))
                {
                    log.Debug("Located Path Target");
                    targetReached = true;
                    break;
                }

                foreach (var node in current.Neighbours)
                {
                    if (ClosedList.Contains(node)) continue;
                    float weight = Weights[current] + HeuristicWeight(node, current);
                    bool alreadyOpen = false;
                    foreach (var oLNode in OpenList.UnorderedItems)
                    {
                        if (oLNode.Element == node) alreadyOpen = true;
                    }
                    if (alreadyOpen && weight >= Weights[current]) continue;

                    if (Trace.ContainsKey(node) == false) Trace.Add(node, current);
                    else Trace[node] = current;
                    if (Weights.ContainsKey(node) == false) Weights.Add(node, weight);
                    else Weights[node] = weight;

                    float heuristicWeight = weight + HeuristicWeight(node, End);

                    if (!alreadyOpen)
                    {
                        OpenList.Enqueue(node, heuristicWeight);
                    }
                }
            }

            if (targetReached)
            {
                log.Debug($"Path Located after {nodesTraversed} nodes.");
                Stack<INavNode> Path = new Stack<INavNode>();
                Path.Push(End);
                do
                {
                    Path.Push(current);
                    current = Trace[current];
                } while (current != Start);
                return Path;
            }
            return null;
        }

        private float HeuristicWeight(INavNode Start, INavNode End)
        {
            return MathF.Sqrt(
                MathF.Pow(Start.Coordinates.X - End.Coordinates.X, 2) +
                MathF.Pow(Start.Coordinates.Y - End.Coordinates.Y, 2));
        }

        private float HeuristicWeight(IGraph Start, IGraph End)
        {
            return MathF.Sqrt(
                MathF.Pow(Start.Origin.X - End.Origin.X, 2) +
                MathF.Pow(Start.Origin.Y - End.Origin.Y, 2));
        }

        private float HeuristicWeight(Cluster Start, Cluster End)
        {
            return MathF.Sqrt(
                MathF.Pow(Start.Origin.X - End.Origin.X, 2) +
                MathF.Pow(Start.Origin.Y - End.Origin.Y, 2));
        }
    }
}
