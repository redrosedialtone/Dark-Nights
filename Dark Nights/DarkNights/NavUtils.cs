using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    public class GraphEdgeNode : INavNode
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

    public class GraphConnection
    {
        public int Depth;
        public IGraph Cast;
        private IGraph Source;

        private Dictionary<int, GraphEdgeNode> interEdgeNodes = new Dictionary<int, GraphEdgeNode>();
        private Dictionary<int, List<InterEdgeData>> interEdgeData = new Dictionary<int, List<InterEdgeData>>();

        public GraphConnection(IGraph source, IGraph cast, int depth)
        {
            this.Source = source; this.Cast = cast; this.Depth = depth;
        }

        public GraphConnection Pair(IGraph newSource)
        {
            GraphConnection pair = new GraphConnection(newSource, newSource == Cast ? Source : Cast, Depth);
            pair.Add(interEdgeData);
            pair.PreGraph();
            return pair;
        }

        public GraphConnection Pair(IGraph lowSource, IGraph highSource, int Depth)
        {
            this.Depth = Depth;
            GraphConnection pair = new GraphConnection(lowSource, lowSource == Cast ? Source : Cast, Depth);
            pair.Add(interEdgeData);
            pair.PreGraph();
            return pair;
        }

        public void Add(IGraph graph, Coordinates[] tiles)
        {
            var symmetryIndex = tiles.Length / 2;
            var data = new InterEdgeData(tiles, tiles[symmetryIndex]);
            if (interEdgeData.TryGetValue(graph.GetHashCode(), out List<InterEdgeData> existing))
            {
                existing.Add(data);
            }
            else
            {
                interEdgeData.Add(graph.GetHashCode(), new List<InterEdgeData>() { data });
            }
        }

        private void Add(Dictionary<int, List<InterEdgeData>> data)
        {
            foreach (var kV in data)
            {
                interEdgeData.Add(kV.Key, kV.Value);
            }
        }

        public void PreGraph()
        {
            interEdgeNodes = new Dictionary<int, GraphEdgeNode>();
            if(interEdgeData.Count > 0)
            {
                foreach (var edge in interEdgeData[Source.GetHashCode()])
                {
                    Coordinates Coordinates = edge.Node;
                    GraphEdgeNode node = new GraphEdgeNode(Coordinates, Depth);
                    interEdgeNodes.Add(edge.GetHashCode(), node);
                }
            }
        }

        public void BuildGraph()
        {
            if (interEdgeData.Count > 0)
            {
                foreach (var edge in interEdgeData[Cast.GetHashCode()])
                {
                    interEdgeNodes.Remove(edge.GetHashCode());
                    Coordinates Coordinates = edge.Node;
                    if (NavSys.Get.TryGetNode<GraphEdgeNode>(Coordinates, out GraphEdgeNode node, Depth))
                    {
                        interEdgeNodes.Add(edge.GetHashCode(), node);
                    }
                }
            }
        }

        public IEnumerable<InterEdgeData> EdgeData(IGraph Graph)
        {
            if (interEdgeData.TryGetValue(Graph.GetHashCode(), out List<InterEdgeData> data)) 
            {
                foreach (var edge in data)
                {
                    yield return edge;
                }
            }
        }

        public GraphEdgeNode Node(InterEdgeData data)
        {
            if (interEdgeNodes.TryGetValue(data.GetHashCode(), out GraphEdgeNode node))
            {
                return node;
            }
            return null;
        }

        public IEnumerable<GraphEdgeNode> EdgeNodes(IGraph Graph)
        {
            if (interEdgeData.TryGetValue(Graph.GetHashCode(), out List<InterEdgeData> data))
            {
                foreach (var edge in data)
                {
                    if (interEdgeNodes.TryGetValue(edge.GetHashCode(), out GraphEdgeNode node))
                    {
                        yield return node;
                    }
                }
            }
        }

        public IEnumerable<InterEdgeData> AllEdges
        {
            get
            {
                if (interEdgeData == null) yield break;
                foreach (var data in interEdgeData)
                {
                    foreach (var edge in data.Value)
                    {
                        yield return edge;
                    }

                }
            }
        }

        public struct InterEdgeData
        {
            public Coordinates[] Tiles;
            public Coordinates Node;
            public int Clearance => Tiles.Length;

            public InterEdgeData(Coordinates[] tiles, Coordinates node)
            {
                Tiles = tiles; Node = node;
            }

            public override int GetHashCode()
            {
                return Node.GetHashCode();
            }
        }
    }

    public class NavPath
    {
        public Stack<INavNode> nodePath;
        public Stack<Coordinates> tilePath;
        public Stack<Coordinates> lastPath = new Stack<Coordinates>();
        public int Count => tilePath == null ? nodePath.Count : tilePath.Count;

        public Action Completed;

        public Coordinates Next()
        {
            Coordinates next;
            if (tilePath != null)
            {
                next = tilePath.Pop();
                if (tilePath.Count == 0) Done();
            }
            else
            {
                next = nodePath.Pop().Coordinates;
                if (nodePath.Count == 0) Done();
            }
            lastPath.Push(next);
            return next;
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
                foreach (var connection in current.Connections)
                {
                    foreach (var graph in connection.Cast.GetGraph)
                    {
                        // Can we traverse back to the original graph?
                        //if (!(graph.Exits.Contains(current)))
                        //{
                        //    continue;
                        //}

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

            closedSet.Add(origin);
            if ((Nav.Impassable(origin))) return null;
            traversible.AddLast(origin);

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
            Dictionary<IGraph, List<GraphConnection>> graphExits = new Dictionary<IGraph, List<GraphConnection>>();

            foreach (var subGraph in source.GraphChildren)
            {
                foreach (var connection in subGraph.Connections)
                {
                    IGraph exit = Nav.Graph((Coordinates)connection.Cast.Origin, source.Depth);
                    if (exit == source) continue;
                    if (!(edges.Contains(exit))) continue;

                    if (graphExits.TryGetValue(exit, out List<GraphConnection> val))
                    {
                        val.Add(connection);
                    }
                    else
                    {
                        List<GraphConnection> connections = new List<GraphConnection>() { connection };
                        graphExits.Add(exit, connections);
                    }

                    //if (graphExits.Contains(exit))
                    //{
                    //    var graphEdge = edgeBuilders[exit.GetHashCode()];
                    //    foreach (var data in connection.EdgeData(subGraph))
                    //    {
                    //        graphEdge.AddTiles(source, data.Tiles);

                    //    }
                    //    foreach (var data in connection.EdgeData(connection.Cast))
                    //    {
                    //        graphEdge.AddTiles(exit, data.Tiles);
                    //    }

                    //}
                    //else
                    //{
                    //    GraphEdgeBuilder graphEdge = new GraphEdgeBuilder(new IGraph[] { source, exit });

                    //    foreach (var data in connection.EdgeData(subGraph))
                    //    {
                    //        graphEdge.AddTiles(source, data.Tiles);

                    //    }
                    //    foreach (var data in connection.EdgeData(connection.Cast))
                    //    {
                    //        graphEdge.AddTiles(exit, data.Tiles);
                    //    }

                    //    edgeBuilders.Add(exit.GetHashCode(), graphEdge);
                    //    graphExits.Add(exit);
                    //}
                }
            }

            foreach (var kv in graphExits)
            {
                foreach (var connection in kv.Value)
                {
                    source.UpdateGraphEdge(source,kv.Key, connection);
                }
                
            }

            //foreach (var builder in edgeBuilders)
            //{
            //    var graphEdges = builder.Value.Build();
            //    source.UpdateEdge(builder.Value.Graphs[1], graphEdges);
            //}
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

        public GraphConnection Build()
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

            GraphConnection connection = new GraphConnection(Graphs[0], Graphs[1], Graphs[0].Depth);

            foreach (var edge in edges)
            {
                connection.Add(Graphs[0], edge[0]);
                connection.Add(Graphs[1], edge[1]);               
            }

            return connection;
        }
    }
}
