using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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

    public struct PathNode : INavNode
    {
        public PassabilityFlags Passability => PassabilityFlags.Pathing;
        public Coordinates Coordinates { get; private set; }
        public IEnumerable<INavNode> Neighbours { get; private set; }

        public PathNode(Coordinates Coordinates, IEnumerable<INavNode> Neighbours)
        {
            this.Coordinates = Coordinates; this.Neighbours = Neighbours;
            NavSys.Get.AddTemporaryNode(this);
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

    public struct GraphEdgeNode : INavNode
    {
        public PassabilityFlags Passability => PassabilityFlags.Pathing;
        public Coordinates Coordinates { get; private set; }
        public IEnumerable<INavNode> Neighbours => linkedEdges;
        private INavNode[] linkedEdges;
        public int Depth { get; private set; }

        public GraphEdgeNode(Coordinates Coordinates, int Depth)
        {
            this.Coordinates = Coordinates; 
            this.Depth = Depth;
            linkedEdges = null;
        }

        public void SetNeighbours(GraphEdgeNode[] linkedEdges)
        {
            this.linkedEdges = new INavNode[linkedEdges.Length];
            linkedEdges.CopyTo(this.linkedEdges, 0);
        }
    }

    public struct InterEdge
    {
        public Coordinates[][] Tiles;
        public Coordinates[] Linked;
        public int Clearance;
        public int Depth;

        private int sourceHash;

        public InterEdge(IGraph Source, Coordinates[][] Tiles, int Depth)
        {
            this.Tiles = Tiles; this.Depth = Depth;

            int index = Tiles[0].Length / 2;
            Linked = new Coordinates[] { Tiles[0][index], Tiles[1][index] };
            Clearance = Tiles[0].Length;

            sourceHash = Source.GetHashCode();
        }

        public IEnumerable<IGraph> Graphs()
        {
            yield return NavSys.Get.Graph(Linked[0], Depth);
            yield return NavSys.Get.Graph(Linked[1], Depth);
        }

        public bool Connected(IGraph source, out IGraph cast, out Coordinates sourceTile, out Coordinates castTile)
        {
            sourceTile = default;
            castTile = default;
            cast = null;

            bool connected = false;
            foreach (var graph in Graphs())
            {
                if (graph == source) connected = true;
                else cast = graph;
            }
            if (!connected) return false;

            if (source.GetHashCode() == sourceHash) { sourceTile = Linked[0]; castTile = Linked[1]; }
            else { sourceTile = Linked[1]; castTile = Linked[0]; }
            return true;
        }

        public bool Connected(IGraph source, out IGraph cast, out IEnumerable<Coordinates> sourceEdge, out IEnumerable<Coordinates> castEdge)
        {
            sourceEdge = null;
            castEdge = null;
            cast = null;

            bool connected = false;
            foreach (var graph in Graphs())
            {
                if (graph == source) connected = true;
                else cast = graph;
            }
            if (!connected) return false;

            if (source.GetHashCode() == sourceHash) { sourceEdge = Tiles[0]; castEdge = Tiles[1]; }
            else { sourceEdge = Tiles[1]; castEdge = Tiles[0]; }
            return true;
        }
    }

    public class NavPath
    {
        public Stack<Coordinates> TilePath = new Stack<Coordinates>();
        public Stack<Coordinates> lastPath = new Stack<Coordinates>();
        public int Count => TilePath.Count;

        public Action Completed;

        public Coordinates Next()
        {
            var ret = TilePath.Pop();
            lastPath.Push(ret);
            if (TilePath.Count == 0)
            {
                Completed?.Invoke();
            }
            return ret;
        }

        public void Done()
        {
            Completed?.Invoke();
        }
    }

    public class RegionBuilder
    {
        public Region[] Regions;
        public Coordinates Start;
        public (Coordinates min, Coordinates max) Bounds;
        public IGraph[] Previous;

        private NavSys Nav => NavSys.Get;

        public void Build(Cluster origin)
        {
            int level = origin.Depth;
            List<Region> regions = new List<Region>();
            if (level == 0)
            {
                HashSet<Coordinates> openSet = new HashSet<Coordinates>();
                foreach (var tile in origin.Tiles())
                {
                    openSet.Add(tile);
                }

                while (openSet.Count > 0)
                {
                    Coordinates next = openSet.First();
                    var region = FloodFill(next, out HashSet<Coordinates> closedSet);
                    if (closedSet.Count > 0)
                    {
                        openSet.ExceptWith(closedSet);
                    }
                    if (region != null) regions.Add(region);
                }
            }
            else
            {
                HashSet<IGraph> openSet = new HashSet<IGraph>();
                foreach (var subGraph in origin.GraphChildren)
                {
                    openSet.Add(subGraph);
                }

                while (openSet.Count > 0)
                {
                    IGraph next = openSet.First();
                    var region = FloodFill(next, out HashSet<IGraph> closedSet);
                    if (closedSet.Count > 0)
                    {
                        openSet.ExceptWith(closedSet);
                    }
                    if (region != null) regions.Add(region);
                }
            }

            Regions = regions.ToArray();
        }

        private Region FloodFill(IGraph Cast, out HashSet<IGraph> closedSet)
        {
            closedSet = new HashSet<IGraph>();
            LinkedList<IGraph> traversible = new LinkedList<IGraph>();
            List<IGraph> edges = new List<IGraph>();

            Queue<IGraph> queue = new Queue<IGraph>();
            queue.Enqueue(Cast);

            traversible.AddLast(Cast);
            closedSet.Add(Cast);

            while (queue.Count > 0)
            {
                IGraph current = queue.Dequeue();
                foreach (var exit in current.Exits)
                {
                    foreach (var graph in exit.GetGraph)
                    {
                        // Can we traverse back to the original graph?
                        if (!(graph.Exits.Contains(current)))
                        {
                            continue;
                        }

                        // Have we been checked already?
                        if (closedSet.Contains(graph)) continue;
                        closedSet.Add(graph);

                        // Are we out of bounds?
                        var tile = graph.Origin;
                        if (tile.X < Bounds.min.X || tile.Y < Bounds.min.Y ||
                            tile.X >= Bounds.max.X || tile.Y >= Bounds.max.Y)
                        {
                            var sibling = NavSys.Get.Graph(tile, 1);
                            if (!(edges.Contains(sibling))) edges.Add(sibling);
                            continue;
                        }

                        traversible.AddLast(graph);
                        queue.Enqueue(graph);
                    }
                }
            }
            if (traversible.Count > 0)
            {
                LinkedList<Coordinates> area = new LinkedList<Coordinates>();
                foreach (var contained in traversible)
                {
                    foreach (var tile in contained.Area)
                    {
                        area.AddLast(tile);
                    }
                }

                Region newRegion = new Region(area.ToArray(), traversible.ToArray(), Previous.First().Depth);
                GraphExits(newRegion, edges.ToArray());
                return newRegion;
            }
            return null;
        }

        private Region FloodFill(Coordinates origin, out HashSet<Coordinates> closedSet)
        {
            closedSet = new HashSet<Coordinates>();
            LinkedList<Coordinates> traversible = new LinkedList<Coordinates>();
            LinkedList<(Coordinates From, Coordinates To)> exits = new LinkedList<(Coordinates From, Coordinates To)>();

            Queue<Coordinates> queue = new Queue<Coordinates>();
            queue.Enqueue(origin);

            if (!(Nav.Impassable(origin)))
            {
                traversible.AddLast(origin);
            }
            closedSet.Add(origin);

            while (queue.Count > 0)
            {
                Coordinates next = queue.Dequeue();
                bool blockedEdge = Nav.Impassable(next);
                foreach (var neighbour in next.Adjacent)
                {
                    // Have we been checked already?
                    if (closedSet.Contains(neighbour)) continue;
                    closedSet.Add(neighbour);

                    // Are we out of bounds?
                    if (neighbour.X < Bounds.min.X || neighbour.Y < Bounds.min.Y ||
                        neighbour.X >= Bounds.max.X || neighbour.Y >= Bounds.max.Y) 
                    {
                        exits.AddLast((next, neighbour));
                        continue;
                    }

                    // Can we traverse this tile?
                    if (Nav.Impassable(neighbour)) continue;
                    queue.Enqueue(neighbour);
                    traversible.AddLast(neighbour);
                }
            }
            if (traversible.Count > 0)
            {
                Region newRegion = new Region(traversible.ToArray(), 0);
                GraphExits(newRegion, exits);
                return newRegion;
            }
            return null;
        }

        private void GraphExits(IGraph origin, LinkedList<(Coordinates From, Coordinates To)> edges)
        {
            List<IGraph> graphExits = new List<IGraph>();
            Dictionary<int, GraphEdgeBuilder> edgeBuilders = new Dictionary<int, GraphEdgeBuilder>();

            foreach (var edge in edges)
            {
                IGraph exit = Nav.Graph(edge.To);
                if (exit == null) continue;
                if (graphExits.Contains(exit))
                {
                    GraphEdgeBuilder graphEdge = edgeBuilders[exit.GetHashCode()];
                    graphEdge.Tiles[0].Add(edge.From);
                    graphEdge.Tiles[1].Add(edge.To);
                }
                else
                {
                    graphExits.Add(exit);
                    GraphEdgeBuilder graphEdge = new GraphEdgeBuilder(new IGraph[] { origin, exit });
                    graphEdge.Tiles[0].Add(edge.From);
                    graphEdge.Tiles[1].Add(edge.To);
                    edgeBuilders.Add(exit.GetHashCode(), graphEdge);
                }
            }

            foreach (var builder in edgeBuilders)
            {
                var graphEdges = builder.Value.Build();
                origin.UpdateEdge(builder.Value.Graphs[1], graphEdges);
            }
        }

        private void GraphExits(IGraph source, IGraph[] edges)
        {
            List<IGraph> graphExits = new List<IGraph>();
            Dictionary<int, GraphEdgeBuilder> edgeBuilders = new Dictionary<int, GraphEdgeBuilder>();

            foreach (var subGraph in source.GraphChildren)
            {
                foreach (var subExit in subGraph.Exits)
                {
                    IGraph exit = Nav.Graph(subExit.Origin, source.Depth);
                    if (exit == source) continue;
                    if (!(edges.Contains(exit))) continue;

                    if (graphExits.Contains(exit))
                    {
                        var graphEdge = edgeBuilders[exit.GetHashCode()];
                        foreach (var edge in subGraph.GetEdges(subExit))
                        {
                            if (edge.Connected(subGraph, out IGraph cast, out IEnumerable<Coordinates> sourceEdge, out IEnumerable<Coordinates> castEdge))
                            {
                                graphEdge.AddTiles(source, sourceEdge);
                                graphEdge.AddTiles(exit, castEdge);
                            }
                        }
                    }
                    else
                    {
                        GraphEdgeBuilder graphEdge = new GraphEdgeBuilder(new IGraph[] { source, exit });

                        foreach (var edge in subGraph.GetEdges(subExit))
                        {
                            if (edge.Connected(subGraph, out IGraph cast, out IEnumerable<Coordinates> sourceEdge, out IEnumerable<Coordinates> castEdge))
                            {
                                graphEdge.AddTiles(source, sourceEdge);
                                graphEdge.AddTiles(exit, castEdge);
                            }
                        }

                        edgeBuilders.Add(exit.GetHashCode(), graphEdge);
                        graphExits.Add(exit);
                    }
                }
            }

            foreach (var builder in edgeBuilders)
            {
                var graphEdges = builder.Value.Build();
                source.UpdateEdge(builder.Value.Graphs[1], graphEdges);
            }
        }
    }

    public class GraphEdgeBuilder
    {
        public IGraph[] Graphs;
        public List<Coordinates>[] Tiles;

        public GraphEdgeBuilder(IGraph[] Graphs)
        {
            this.Graphs = Graphs;
            Tiles = new List<Coordinates>[] { new List<Coordinates>(), new List<Coordinates>() };
        }

        public void AddTiles(IGraph exit, IEnumerable<Coordinates> tiles)
        {
            int index = -1;
            for (int i = 0; i < Graphs.Length; i++)
            {
                if (Graphs[i] == exit) { index = i; break; }
            }
            if (index == -1) return;
            Tiles[index].AddRange(tiles);
        }

        public void AddTiles(IGraph[] exits, Coordinates[][] tiles)
        {
            int indexA = -1;
            int indexB = -1;
            for (int i = 0; i < exits.Length; i++)
            {
                if (Graphs[0] == exits[i]) indexA = i;
                if (Graphs[1] == exits[i]) indexB = i;
            }
            if (indexA == -1 || indexB == -1) return;
            Tiles[0].AddRange(tiles[indexA]);
            Tiles[1].AddRange(tiles[indexB]);
        }

        public IEnumerable<InterEdge> Build()
        {
            Coordinates[] exitsA = Tiles[0].OrderBy(a => a.X).ThenBy(a => a.Y).ToArray();
            Coordinates[] exitsB = Tiles[1].OrderBy(a => a.X).ThenBy(a => a.Y).ToArray();
            List<Coordinates[][]> edges = new List<Coordinates[][]>();

            (Coordinates a, Coordinates b) previous = (exitsA[0], exitsB[0]);

            int prevIndex = 0;
            int curIndex = 0;
            for (;curIndex < exitsA.Length; curIndex++)
            {
                Coordinates a = exitsA[curIndex];
                Coordinates b = exitsB[curIndex];
                bool adjacent = a.AdjacentTo(previous.a) || b.AdjacentTo(previous.b);
                bool blocked = NavSys.Get.Impassable(a) || NavSys.Get.Impassable(b);
                if (blocked)
                {
                    if (curIndex > prevIndex)
                    {
                        edges.Add(new Coordinates[][] { exitsA[prevIndex..curIndex], exitsB[prevIndex..curIndex] });
                    }
                        
                    prevIndex = curIndex + 1;
                }
                else if (!adjacent)
                {
                    if (curIndex > prevIndex)
                    {
                        edges.Add(new Coordinates[][] { exitsA[prevIndex..curIndex], exitsB[prevIndex..curIndex] });
                    }
                    prevIndex = curIndex;
                }
                previous = (a, b);
            }

            if (curIndex > prevIndex)
            {
                edges.Add(new Coordinates[][] { exitsA[prevIndex..curIndex], exitsB[prevIndex..curIndex] });
            }

            foreach (var edge in edges)
            {
                InterEdge graphEdge = new InterEdge(Graphs[0], edge, Graphs[0].Depth);
                yield return graphEdge;
            }
        }
    }
}
