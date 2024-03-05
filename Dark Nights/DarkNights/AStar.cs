using Priority_Queue;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

        private readonly Dictionary<Coordinates, float> gWeights = new Dictionary<Coordinates, float>();
        private readonly Dictionary<Coordinates, float> hWeights = new Dictionary<Coordinates, float>();

        private readonly Coordinates Start;
        private readonly Coordinates End;
        private Coordinates Current;
        private int limit = 999;

        public AStarTiles(Coordinates Start, Coordinates End)
        {
            this.Start = Start;
            this.End = End;
        }

        public Stack<Coordinates> Path()
        {
            //log.Debug("Generating Heuristic Path...");
            OpenList.Enqueue(Start, 0);
            gWeights[Start] = 0;
            hWeights[Start] = HeuristicWeight((Start.X, Start.Y), (End.X, End.Y));

            bool targetReached = false;
            int nodesTraversed = 0;
            while (OpenList.Count > 0)
            {
                nodesTraversed++;
                Current = OpenList.Dequeue();
                // We did it!
                if (Current == End)
                {
                    //log.Debug("Located Path Target");
                    targetReached = true;
                    break;
                }

                ClosedList.Add(Current);

                foreach (var successor in Current.Adjacent)
                {
                    if (ClosedList.Contains(successor)) continue;
                    if (NavSys.Get.TryGetNode(successor, out INavNode node))
                    {
                        if (node.Passability == PassabilityFlags.Impassable)
                        {
                            ClosedList.Add(successor); continue;
                        }
                    }

                    float g = gWeights[Current] + 1.0f;
                    bool alreadyOpen = OpenList.Contains(successor);
                    if (alreadyOpen && g >= gWeights[successor]) continue;

                    if (Trace.ContainsKey(successor) == false) Trace.Add(successor, Current);
                    else Trace[successor] = Current;
                    if (gWeights.ContainsKey(successor) == false) gWeights.Add(successor, g);
                    else gWeights[successor] = g;

                    float heuristicWeight = g + Euclidean(successor,End);
                    if (hWeights.ContainsKey(successor) == false) hWeights.Add(successor, heuristicWeight);
                    else hWeights[successor] = heuristicWeight;

                    if (!alreadyOpen)
                    {
                        OpenList.Enqueue(successor, hWeights[successor]);
                    }
                }
            }

            if (targetReached)
            {
                //log.Debug($"Path Located ({nodesTraversed}) after {nodesTraversed} nodes.");
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

        private float Manhattan(Coordinates A, Coordinates B)
        {
            return MathF.Abs(A.X - B.X) + Math.Abs(A.Y - B.Y);
        }

        private float Euclidean(Coordinates A, Coordinates B)
        {
            var x = A.X - B.X;
            var y = A.Y - B.Y;

            return MathF.Sqrt(x * x + y * y);
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

    public class AStarManhattan
    {

        private class Node
        {
            public int x;
            public int y;
            public Node p;
            public double g;
            public double f;
            public int v;

            public Node(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        public Coordinates[] Path(Coordinates s, Coordinates g, int limit = 6250)
        {
            List<Coordinates> Result = new List<Coordinates>();
            Dictionary<int, int> list = new Dictionary<int, int>();
            List<Node> open = new List<Node>(new Node[limit]);

            Node node = new Node(s.X, s.Y)
            {
                f = 0,
                g = 0,
                v = s.X + s.Y * limit
            };

            open.Insert(0, node);

            int length = 1;
            Node adj;

            int i;
            int j;
            double max;
            int min;

            Node current;
            Node[] next;

            Node end = new Node(g.X, g.Y)
            {
                v = g.X + g.Y * limit
            };

            do
            {
                max = limit;
                min = 0;

                for (i = 0; i < length; i++)
                {
                    double f = open[i].f;

                    if (f < max)
                    {
                        max = f;
                        min = i;
                    }
                }

                current = open[min];
                open.RemoveRange(min, 1);

                if (current.v != end.v)
                {
                    --length;
                    next = Successors(current);

                    if (length + next.Length > open.Count)
                    {
                        // Reached limit
                        break;
                    }

                    for (i = 0, j = next.Length; i < j; ++i)
                    {
                        if (next[i] == null) continue;
                        (adj = next[i]).p = current;
                        adj.f = adj.g = 0;
                        adj.v = adj.x + adj.y * limit;

                        if (!list.ContainsKey(adj.v))
                        {
                            adj.f = (adj.g = current.g + Euclidean(adj, current)) + Euclidean(adj, end);
                            open[length++] = adj;
                            list[adj.v] = 1;
                        }
                    }
                }
                else
                {
                    i = length = 0;

                    do
                    {
                        Coordinates point = new Coordinates(current.x, current.y);
                        Result.Add(point);
                    }
                    while ((current = current.p) != null);

                    //Result.Reverse();
                }
            }
            while (length != 0);
            return Result.ToArray();

        }

        public Coordinates[] PathBounded(Coordinates s, Coordinates g, (Coordinates min, Coordinates max) bounds)
        {
            int limit = (bounds.max.X - bounds.min.X) * (bounds.max.Y - bounds.min.Y);
            List<Coordinates> Result = new List<Coordinates>();
            Dictionary<int, int> list = new Dictionary<int, int>();
            List<Node> open = new List<Node>(new Node[limit]);

            Node node = new Node(s.X, s.Y)
            {
                f = 0,
                g = 0,
                v = s.X + s.Y * limit
            };

            open.Insert(0, node);

            int length = 1;
            Node adj;

            int i;
            int j;
            double max;
            int min;

            Node current;
            Node[] next;

            Node end = new Node(g.X, g.Y)
            {
                v = g.X + g.Y * limit
            };

            do
            {
                max = limit;
                min = 0;

                for (i = 0; i < length; i++)
                {
                    double f = open[i].f;

                    if (f < max)
                    {
                        max = f;
                        min = i;
                    }
                }

                current = open[min];
                open.RemoveRange(min, 1);

                if (current.v != end.v)
                {
                    --length;
                    next = SuccessorsBounded(current,bounds);

                    if (length + next.Length > open.Count)
                    {
                        // Reached limit
                        break;
                    }

                    for (i = 0, j = next.Length; i < j; ++i)
                    {
                        if (next[i] == null) continue;

                        (adj = next[i]).p = current;
                        adj.f = adj.g = 0;
                        adj.v = adj.x + adj.y * limit;

                        if (!list.ContainsKey(adj.v))
                        {
                            adj.f = (adj.g = current.g + Euclidean(adj, current)) + Euclidean(adj, end);
                            open[length++] = adj;
                            list[adj.v] = 1;
                        }
                    }
                }
                else
                {
                    i = length = 0;

                    do
                    {
                        Coordinates point = new Coordinates(current.x, current.y);
                        Result.Add(point);
                    }
                    while ((current = current.p) != null);

                    //Result.Reverse();
                }
            }
            while (length != 0);
            if (Result.Count == 0) return null;
            else return Result.ToArray();

        }

        public void NodeSearch(AbstractGraphNode Source, HashSet<AbstractGraphNode> Nodes, Cluster Origin, out HashSet<AbstractGraphNode> Linked)
        {
            Linked = new HashSet<AbstractGraphNode>();
            Linked.Add(Source);
            foreach (var node in Nodes)
            {
                if (node == Source) continue;
                if (PathBounded(Source.Coordinates, node.Coordinates, (Origin.Minimum, Origin.Maximum)) != null)
                {
                    Linked.Add(node);
                }
            }
        }

        private Node[] Successors(Node node)
        {
            Coordinates l = new Coordinates(node.x, node.y);
            Node[] result = new Node[4];
            int i = 0;
            foreach (var edge in l.Adjacent)
            {
                if (NavSys.Get.TryGetNode(edge, out INavNode n))
                {
                    if (n.Passability == PassabilityFlags.Impassable)
                    {
                        continue;
                    }
                }
                result[i++] = new Node(edge.X, edge.Y);
            }
            return result;
        }

        private Node[] SuccessorsBounded(Node node, (Coordinates min, Coordinates max) bounds)
        {
            Coordinates l = new Coordinates(node.x, node.y);
            Node[] result = new Node[4];
            int i = 0;
            foreach (var edge in l.Adjacent)
            {
                if (edge.X < bounds.min.X || edge.Y < bounds.min.Y ||
                    edge.X >= bounds.max.X || edge.Y >= bounds.max.Y)
                {
                    continue;
                }
                if (NavSys.Get.TryGetNode(edge, out INavNode n))
                {
                    if (n.Passability == PassabilityFlags.Impassable)
                    {
                        continue;
                    }
                }
                result[i++] = new Node(edge.X, edge.Y);
            }
            return result;
        }

        private float Manhattan(Coordinates A, Coordinates B)
        {
            return MathF.Abs(A.X - B.X) + Math.Abs(A.Y - B.Y);
        }

        private double Euclidean(Node start, Node end)
        {
            var x = start.x - end.x;
            var y = start.y - end.y;

            return Math.Sqrt(x * x + y * y);
        }
    }

    public class AStarHierarchal
    {
        private NLog.Logger log => NavSys.log;

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

        public void NodeSearch(AbstractGraphNode Source, HashSet<AbstractGraphNode> Nodes, Cluster Origin, out HashSet<AbstractGraphNode> Linked)
        {
            Linked = new HashSet<AbstractGraphNode>();
            Linked.Add(Source);
            foreach (var node in Nodes)
            {
                if (node == Source) continue;
                if(NodePath(Source, node, Origin))
                {
                    Linked.Add(node);
                }
            }
        }

        public bool NodePath(INavNode A, INavNode B, Cluster Origin)
        {
            SimplePriorityQueue<Coordinates> OpenList = new SimplePriorityQueue<Coordinates>();
            List<Coordinates> ClosedList = new List<Coordinates>();
            Dictionary<Coordinates, Coordinates> Trace = new Dictionary<Coordinates, Coordinates>();

            Dictionary<Coordinates, float> Weights = new Dictionary<Coordinates, float>();
            Dictionary<Coordinates, float> HeuristicScores = new Dictionary<Coordinates, float>();

            Coordinates Start = A.Coordinates;
            Coordinates End = B.Coordinates;

            OpenList.Enqueue(Start, 0);
            Weights[Start] = 0;
            HeuristicScores[Start] = HeuristicWeight((Start.X, Start.Y), (End.X, End.Y));

            Coordinates Current;
            bool targetReached = false;
            int nodesTraversed = 0;
            while (OpenList.Count > 0)
            {
                nodesTraversed++;
                Current = OpenList.Dequeue();
                // We did it!
                if (Current == End)
                {
                    targetReached = true;
                    break;
                }

                ClosedList.Add(Current);

                foreach (var edge in Current.Adjacent)
                {
                    if (edge.X < Origin.Minimum.X || edge.Y < Origin.Minimum.Y ||
                        edge.X >= Origin.Maximum.X || edge.Y >= Origin.Maximum.Y)
                    {
                        if (!ClosedList.Contains(edge)) ClosedList.Add(edge); continue;
                    }
                    if (NavSys.Get.TryGetNode(edge, out INavNode node))
                    {
                        if (node.Passability == PassabilityFlags.Impassable)
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

            return targetReached;
        }

        private float HeuristicWeight(INavNode Start, INavNode End)
        {
            return MathF.Sqrt(
                MathF.Pow(Start.Coordinates.X - End.Coordinates.X, 2) +
                MathF.Pow(Start.Coordinates.Y - End.Coordinates.Y, 2));
        }

        private float HeuristicWeight(Cluster Start, Cluster End)
        {
            return MathF.Sqrt(
                MathF.Pow(Start.Origin.X - End.Origin.X, 2) +
                MathF.Pow(Start.Origin.Y - End.Origin.Y, 2));
        }

        private float HeuristicWeight((int X, int Y) Start, (int X, int Y) End)
        {
            return MathF.Sqrt(
                MathF.Pow(Start.X - End.X, 2) +
                MathF.Pow(Start.Y - End.Y, 2));
        }
    }
}
