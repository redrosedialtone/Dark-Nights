using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        PassabilityFlags Passability { get; }
        Coordinates Coordinates { get; }
        IEnumerable<INavNode> Neighbours { get; }
    }

    public class PathNode : INavNode
    {
        public PassabilityFlags Passability => PassabilityFlags.Pathing;
        public Coordinates Coordinates { get; private set; }
        public IEnumerable<INavNode> Neighbours { get; private set; }

        public PathNode(Coordinates Coordinates)
        {
            this.Coordinates = Coordinates; //this.Neighbours = Neighbours;
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
        public PassabilityFlags Passability => PassabilityFlags.Pathing;
        public Coordinates Coordinates { get; private set; }

        public INavNode interEdge;
        private INavNode[] intraEdges;
        public int Depth { get; private set; }

        public AbstractGraphNode(Coordinates Coordinates, int Depth)
        {
            this.Coordinates = Coordinates; 
            this.Depth = Depth;
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
        public Stack<Coordinates> tilePath = new Stack<Coordinates>();
        public Stack<Coordinates> lastPath = new Stack<Coordinates>();
        public Action OnCompleted;
        public bool Completed = false;

        private bool finishedMovement => (tilePath == null || tilePath.Count == 0) && (abstractPath == null || abstractPath.Count == 0);
        private Coordinates previous;

        public Coordinates Next(Coordinates current)
        {
            Coordinates next = previous;
            if(tilePath.Count == 0)
            {
                if (finishedMovement) Finish();
                else
                {
                    var goal = abstractPath.Pop();
                    tilePath = NavSys.Get.TilePath(current, goal.Coordinates);
                }
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
                bool obstacle = NavSys.Get.Impassable(t) || NavSys.Get.Impassable(sym);
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

            InterEdge[] ret = new InterEdge[lines.Count];

            int i = 0;
            foreach (var line in lines)
            {
                int symInd = line[0].Length / 2;
                InterEdge e = new InterEdge()
                {
                    Tiles = line[0],
                    Transition = (line[0][symInd], line[1][symInd])
                };
                ret[i++] = e;
            }
            return ret;
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
