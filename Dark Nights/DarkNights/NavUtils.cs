using Microsoft.Xna.Framework;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Microsoft.Xna.Framework.Graphics.SpriteFont;

namespace DarkNights
{
    [Flags]
    public enum PassabilityFlags
    {
        Nil = 0,
        Destination = 1,
        Edge = 2,
        Pathing = 4,
        Impassable = 8,
    }

    public interface INavNode
    {
        int Clearance { get; }
        PassabilityFlags Passability { get; }
        Coordinates Coordinates { get; }
        IEnumerable<INavNode> Neighbours { get; }
    }

    public class PathNode : INavNode
    {
        public int Clearance { get; private set; }
        public PassabilityFlags Passability => PassabilityFlags.Pathing;
        public Coordinates Coordinates { get; private set; }
        public IEnumerable<INavNode> Neighbours { get; private set; }

        public PathNode(Coordinates Coordinates, int clearance)
        {
            this.Coordinates = Coordinates; //this.Neighbours = Neighbours;
            this.Clearance = clearance;
            NavSys.Get.AddTemporaryNode(this);
        }

        public void ConnectToGraph(IEnumerable<INavNode> Neighbours)
        {
            this.Neighbours = Neighbours;
        }

        public void Clear()
        {
            NavSys.Get.ClearTemporaryNode(this);
        }
    }

    public struct ImpassableNode : INavNode
    {
        public int Clearance => 0;
        public PassabilityFlags Passability => PassabilityFlags.Impassable;
        public Coordinates Coordinates { get; private set; }
        public IEnumerable<INavNode> Neighbours => null;

        public ImpassableNode(Coordinates Coordinates)
        {
            this.Coordinates = Coordinates;
            NavSys.Get.AddNavNode(this);
        }
    }

    public class AbstractGraphNode : INavNode
    {
        public int Clearance { get; private set; }
        public PassabilityFlags Passability => PassabilityFlags.Pathing;
        public Coordinates Coordinates { get; private set; }

        public INavNode interEdge;
        private INavNode[] intraEdges;
        public int Depth { get; private set; }

        public AbstractGraphNode(Coordinates Coordinates, int Depth, int Clearance)
        {
            this.Coordinates = Coordinates; 
            this.Depth = Depth;
            this.Clearance = Clearance;
            intraEdges = null;
        }

        public void IntraEdges(AbstractGraphNode[] linkedEdges)
        {
            this.intraEdges = new INavNode[linkedEdges.Length];
            linkedEdges.CopyTo(this.intraEdges, 0);
        }

        public IEnumerable<INavNode> Neighbours
        {
            get
            {
                yield return interEdge;
                if (intraEdges == null) yield break;
                foreach (var item in intraEdges)
                {
                    yield return item;
                }
            }
        }
    }


    public class NavPath
    {
        public Stack<INavNode> abstractPath;
        public Stack<Vector2> tilePath = new Stack<Vector2>();
        public Stack<Vector2> lastPath = new Stack<Vector2>();
        public Action OnCompleted;
        public int Clearance = 1;
        public bool Completed = false;
        public bool invalid => (tilePath == null || tilePath.Count == 0) && (abstractPath == null || abstractPath.Count == 0);
        private NLog.Logger log => NavSys.log;

        private Coordinates previous;

        public NavPath(int Clearance, Stack<INavNode> abstractPath)
        {
            this.Clearance = Clearance;
            this.abstractPath = abstractPath;
        }

        public NavPath(int Clearance, Stack<Vector2> tilePath)
        {
            this.Clearance = Clearance;
            this.tilePath = tilePath;
        }

        public Vector2 Next(Vector2 current)
        {
            Vector2 next = previous;
            if(tilePath.Count == 0)
            {
                if (invalid) Finish();
                else
                {
                    var goal = abstractPath.Pop();
                    var clusterA = NavSys.Get.GetCluster(goal.Coordinates);
                    var clusterB = NavSys.Get.GetCluster(current);
                    NavPath newPath;
                    if (clusterA == clusterB) newPath = NavSys.Get.CreateTilePath(current, goal.Coordinates, Clearance, clusterA);
                    else newPath = NavSys.Get.CreateTilePath(current, goal.Coordinates, Clearance);

                    if(newPath == null || newPath.tilePath.Count == 0)
                    {
                        log.Error("NavPath is invalid!");
                        Finish();
                        return next;
                    }

                    tilePath = newPath.tilePath;
                }
                //if (tilePath == null) Finish();
            }
            if(!Completed)
            {
                next = tilePath.Pop();
               // if (finishedMovement) Finish();
            }
            lastPath.Push(next);
            previous = next;
            return next;
        }

        public void Finish()
        {
            Completed = true;
            OnCompleted?.Invoke();
        }
    }

    public struct InterEdge
    {
        public Coordinates[] Tiles;
        public (Coordinates t, Coordinates sym) Transition;

        public InterEdge(Coordinates[] tiles, (Coordinates, Coordinates) transition)
        {
            Tiles = tiles; Transition = transition;
        }
    }

    public class GraphHelper
    {
        public Cluster Source;
        public Cluster Cast;

        private int maxLength = 6;

        public GraphHelper(Cluster source, Cluster cast)
        {
            Source = source;
            Cast = cast;
        }

        public InterEdge[] Entrances()
        {
            if (Source == null || Cast == null) return null;
            Coordinates[][] borders = Borders();
            LinkedList<Coordinates[][]> lines = new LinkedList<Coordinates[][]>();


            (Coordinates t, Coordinates sym) previous = (borders[0][0], borders[1][0]);
            int pIndex = 0;
            int cIndex = 0;
            for (;cIndex < Source.Size; cIndex++)
            {
                Coordinates t = borders[0][cIndex];
                Coordinates sym = borders[1][cIndex];
                bool adjacent = t.AdjacentTo(previous.t) || sym.AdjacentTo(previous.sym);
                bool obstacle = NavSys.Get.Passability(t, PassabilityFlags.Impassable) || NavSys.Get.Passability(sym, PassabilityFlags.Impassable);
                if (obstacle)
                {
                    if(cIndex > pIndex) { lines.AddFirst(new Coordinates[][] { borders[0][pIndex..cIndex], borders[1][pIndex..cIndex] }); }
                    pIndex = cIndex + 1;
                }
                else if (!adjacent)
                {
                    if(cIndex > pIndex) { lines.AddFirst(new Coordinates[][] { borders[0][pIndex..cIndex], borders[1][pIndex..cIndex] }); }
                    pIndex = cIndex;
                }
                previous = (t,sym);
            }

            if (cIndex > pIndex) { lines.AddFirst(new Coordinates[][] { borders[0][pIndex..cIndex], borders[1][pIndex..cIndex] }); }

            List<InterEdge> ret = new List<InterEdge>();

            int i = 0;
            foreach (var line in lines)
            {
                // Place one node.
                if (line[0].Length <= 2)
                {
                    InterEdge e = new InterEdge()
                    {
                        Tiles = line[0],
                        Transition = (line[0][0], line[1][0])
                    };
                    ret.Add(e);
                }
                // One node on each side
                else if (line[0].Length < maxLength)
                {
                    InterEdge e1 = new InterEdge()
                    {
                        Tiles = line[0],
                        Transition = (line[0][0], line[1][0])
                    };
                    ret.Add(e1);
                    InterEdge e2 = new InterEdge()
                    {
                        Tiles = line[0],
                        Transition = (line[0][line[0].Length-1], line[1][0])
                    };
                    ret.Add(e2);
                }
                // One on each side, plus one every length.
                else
                {
                    InterEdge e1 = new InterEdge()
                    {
                        Tiles = line[0],
                        Transition = (line[0][0], line[1][0])
                    };
                    ret.Add(e1);
                    InterEdge e2 = new InterEdge()
                    {
                        Tiles = line[0],
                        Transition = (line[0][line[0].Length-1], line[1][line[0].Length-1])
                    };
                    ret.Add(e2);

                    int num = (line[0].Length-2) / maxLength;
                    if (num == 1)
                    {
                        int index = line[0].Length / 2;
                        InterEdge e3 = new InterEdge()
                        {
                            Tiles = line[0],
                            Transition = (line[0][index], line[1][index])
                        };
                        ret.Add(e3);
                    }
                    else if (num > 1)
                    {
                        int spacing = (line[0].Length - 2) / num;
                        for (; num > 0; num--)
                        {
                            int index = num * spacing;
                            InterEdge e3 = new InterEdge()
                            {
                                Tiles = line[0],
                                Transition = (line[0][index], line[1][index])
                            };
                            ret.Add(e3);
                        }
                    }

                }

            }
            return ret.ToArray();
        }

        public Coordinates[][] Borders()
        {
            Vector2 delta = Source.Origin - Cast.Origin;
            bool east = delta.X < 0;
            bool north = delta.Y < 0;
            bool horizontal = delta.Y == 0;
            bool vertical = delta.X == 0;

            Coordinates[][] edges = new Coordinates[Source.Size][];
            edges[0] = new Coordinates[Source.Size];
            edges[1] = new Coordinates[Source.Size];

            if (horizontal)
            {
                int x;
                int xDir = 0;
                if (east) { x = Source.Maximum.X; xDir++; }
                else { x = Source.Minimum.X - 1; xDir--; }
                for (int y = Source.Minimum.Y; y < Source.Maximum.Y; y++)
                {
                    edges[0][y-Source.Minimum.Y] = new Coordinates(x - xDir, y);
                    edges[1][y - Source.Minimum.Y] = new Coordinates(x, y);
                }
            }
            else if (vertical)
            {
                int y;
                int yDir = 0;
                if (north) { y = Source.Maximum.Y; yDir++; }
                else { y = Source.Minimum.Y - 1; yDir--; }

                for (int x = Source.Minimum.X; x < Source.Maximum.X; x++)
                {
                    edges[0][x - Source.Minimum.X] = new Coordinates(x, y - yDir);
                    edges[1][x - Source.Minimum.X] = new Coordinates(x, y);
                }
            }
            return edges;
        }
    }
}
