using Microsoft.Xna.Framework;
using NLog.Fluent;
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
    public class ThetaStar
    {
        public (Coordinates min, Coordinates max)? Bounds;
        public int Clearance;

        private const int defaultLimit = 999;
        private (Coordinates min, Coordinates max) m_bounds => Bounds.Value;
        private bool bounded => Bounds != null;
        private delegate Node[] getSuccesors(Node n);
        private getSuccesors m_getSuccessors;

        public ThetaStar(int clearance, (Coordinates min, Coordinates max)? bounds)
        {
            this.Clearance = clearance;
            Bounds = bounds;
            if (bounded) m_getSuccessors = SuccessorsBounded;
            else m_getSuccessors = Successors;
        }

        public ThetaStar(int clearance)
        {
            this.Clearance = clearance;
            Bounds = null;
            m_getSuccessors = Successors;
        }

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

        public Vector2[] Path(Coordinates s, Coordinates g)
        {
            int limit;
            if (bounded) limit = (m_bounds.max.X - m_bounds.min.X) * (m_bounds.max.Y - m_bounds.min.Y);
            else limit = defaultLimit;

            List<Vector2> Result = new List<Vector2>();
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
                    next = m_getSuccessors(current);

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

                            
                            if (LineOfSight(current.p, adj))
                            {
                                adj.f = (adj.g = current.p.g + Euclidean(current.p, adj));
                                adj.p = current.p;
                            }
                            else
                            {
                                adj.f = (adj.g = current.g + Euclidean(current, adj));
                            }
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
                        Vector2 point = new Coordinates(current.x, current.y);
                        Result.Add(point);
                    }
                    while ((current = current.p) != null);

                    //Result.Reverse();
                }
            }
            while (length != 0);
            if (Result.Count == 0) return new Vector2[] { };
            else return Result.ToArray();

        }

        private Node[] Successors(Node node)
        {
            Coordinates l = new Coordinates(node.x, node.y);
            Node[] result = new Node[4];
            int i = 0;
            foreach (var edge in l.Adjacent)
            {
                int clearance = NavSys.Get.Clearance(edge);
                if (clearance < this.Clearance) continue;
                result[i++] = new Node(edge.X, edge.Y);
            }
            return result;
        }

        private Node[] SuccessorsBounded(Node node)
        {
            Coordinates l = new Coordinates(node.x, node.y);
            Node[] result = new Node[4];
            int i = 0;
            foreach (var edge in l.Adjacent)
            {
                if (edge.X < m_bounds.min.X || edge.Y < m_bounds.min.Y ||
                    edge.X >= m_bounds.max.X || edge.Y >= m_bounds.max.Y)
                {
                    continue;
                }
                int clearance = NavSys.Get.Clearance(edge);
                if (clearance < this.Clearance) continue;
                result[i++] = new Node(edge.X, edge.Y);
            }
            return result;
        }

        private float Manhattan(Node A, Node B)
        {
            return MathF.Abs(A.x - B.x) + Math.Abs(A.y - B.y);
        }

        private double Euclidean(Node start, Node end)
        {
            var x = start.x - end.x;
            var y = start.y - end.y;

            return Math.Sqrt(x * x + y * y);
        }

        private bool LineOfSight(Node node1, Node node2)
        {
            if (node1 == null) return false;
            Coordinates A = new Coordinates(node1.x, node1.y);
            Coordinates B = new Coordinates(node2.x, node2.y);
            int dx = (int)Math.Abs(B.X - A.X);
            int dy = (int)Math.Abs(B.Y - A.Y);
            int sx = A.X < B.X ? 1 : -1;
            int sy = A.Y < B.Y ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                int clearance = NavSys.Get.Traversability(A);
                if (clearance <= this.Clearance) return false;
                if (A.X == B.X && A.Y == B.Y) return true;
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    if (A.X == B.X) return true;
                    err -= dy;
                    A.X += sx;
                }
                if (e2 < dx)
                {
                    if (A.Y == B.Y) return true;
                    err += dx;
                    A.Y += sy;
                }
            }
        }
    }

    public class AStar
    {
        public (Coordinates min, Coordinates max)? Bounds;
        public int Clearance;

        private NLog.Logger log => NavSys.log;
        private const int defaultLimit = 999;
        private (Coordinates min, Coordinates max) m_bounds => Bounds.Value;
        private bool bounded => Bounds != null;
        private delegate Node[] getSuccesors(Node n);
        private getSuccesors m_getSuccessors;

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

        public AStar(int clearance, (Coordinates min, Coordinates max)? bounds)
        {
            this.Clearance = clearance;
            SetBounds(bounds);
        }

        public AStar(int clearance)
        {
            this.Clearance = clearance;
            SetBounds(null);
        }

        public void SetBounds((Coordinates min, Coordinates max)? bounds)
        {
            Bounds = bounds;
            if (bounded) m_getSuccessors = SuccessorsBounded;
            else m_getSuccessors = Successors;
        }

        public Vector2[] Path(Coordinates s, Coordinates g)
        {
            int limit;
            if (bounded) limit = (m_bounds.max.X - m_bounds.min.X + 1) * (m_bounds.max.Y - m_bounds.min.Y + 1);
            else limit = defaultLimit;
            List<Vector2> Result = new List<Vector2>();
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
                    next = m_getSuccessors(current);

                    if (length + next.Length > open.Count)
                    {
                        // Reached limit
                        log.Warn("Reached A* Limit!");
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

        public void NodeSearch(AbstractGraphNode Source, HashSet<AbstractGraphNode> Nodes,  out HashSet<AbstractGraphNode> Linked)
        {
            Linked = new HashSet<AbstractGraphNode>
            {
                Source
            };
            foreach (var node in Nodes)
            {
                if (node == Source) continue;
                if (Path(Source.Coordinates, node.Coordinates).Length != 0)
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
                int clearance = NavSys.Get.Clearance(edge);
                if (clearance < this.Clearance) continue;
                result[i++] = new Node(edge.X, edge.Y);
            }
            return result;
        }

        private Node[] SuccessorsBounded(Node node)
        {
            Coordinates l = new Coordinates(node.x, node.y);
            Node[] result = new Node[4];
            int i = 0;
            foreach (var edge in l.Adjacent)
            {
                if (edge.X < m_bounds.min.X || edge.Y < m_bounds.min.Y ||
                    edge.X >= m_bounds.max.X || edge.Y >= m_bounds.max.Y)
                {
                    continue;
                }
                int clearance = NavSys.Get.Clearance(edge);
                if (clearance < this.Clearance) continue;
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
        public int Size;
        private const int defaultLimit = 999;
        private NLog.Logger log => NavSys.log;

        private class Node
        {
            public INavNode n;
            public Node p;
            public double g;
            public double f;
            public int v;

            public Node(INavNode n)
            {
                this.n = n;
            }
        }

        public AStarHierarchal(int size)
        {
            this.Size = size;
        }     

        public INavNode[] Path(INavNode s, INavNode g)
        {
            int limit = defaultLimit;
            List<INavNode> Result = new List<INavNode>();
            Dictionary<int, int> list = new Dictionary<int, int>();
            List<Node> open = new List<Node>(new Node[limit]);

            Node node = new Node(s)
            {
                f = 0,
                g = 0,
                v = s.Coordinates.X + s.Coordinates.Y * limit
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

            Node end = new Node(g)
            {
                v = g.Coordinates.X + g.Coordinates.Y * limit
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

                bool finished = false;

                foreach (var n in g.Neighbours)
                {
                    if(n == current.n)
                    {
                        finished = true; break;
                    }
                }

                if (!finished)
                {
                    --length;
                    next = Successors(current.n);

                    if (length + next.Length > open.Count)
                    {
                        log.Warn("Reached A* Limit! (Hierarchal)");
                        // Reached limit
                        break;
                    }

                    for (i = 0, j = next.Length; i < j; ++i)
                    {
                        if (next[i] == null) continue;
                        (adj = next[i]).p = current;
                        adj.f = adj.g = 0;
                        adj.v = adj.n.Coordinates.X + adj.n.Coordinates.Y * limit;

                        if (!list.ContainsKey(adj.v))
                        {
                            adj.f = (adj.g = current.g + Euclidean(adj.n.Coordinates, current.n.Coordinates)) + Euclidean(adj.n.Coordinates, end.n.Coordinates);
                            open[length++] = adj;
                            list[adj.v] = 1;
                        }
                    }
                }
                else
                {
                    i = length = 0;
                    end.p = current;
                    current = end;
                    do
                    {
                        Result.Add(current.n);
                    }
                    while ((current = current.p) != null);

                    //Result.Reverse();
                }
            }
            while (length != 0);
            return Result.ToArray();

        }

        private Node[] Successors(INavNode node)
        {
            var neigbhours = node.Neighbours.ToArray();
            Node[] result = new Node[neigbhours.Length];
            int i = 0;
            foreach (var edge in neigbhours)
            {
                int clearance = edge.Clearance;
                if (clearance < this.Size) continue;
                result[i++] = new Node(edge);
            }
            return result;
        }

        public void NodeSearch(AbstractGraphNode Source, HashSet<AbstractGraphNode> Nodes, Cluster Origin, out HashSet<AbstractGraphNode> Linked)
        {
            Linked = new HashSet<AbstractGraphNode>();
            Linked.Add(Source);
            foreach (var node in Nodes)
            {
                if (node == Source) continue;
                if (Path(Source, node).Length != 0)
                {
                    Linked.Add(node);
                }
            }
        }

        private float Manhattan(Coordinates A, Coordinates B)
        {
            return MathF.Abs(A.X - B.X) + Math.Abs(A.Y - B.Y);
        }

        private double Euclidean(Coordinates start, Coordinates end)
        {
            var x = start.X - end.X;
            var y = start.Y - end.Y;

            return Math.Sqrt(x * x + y * y);
        }
    }
}
