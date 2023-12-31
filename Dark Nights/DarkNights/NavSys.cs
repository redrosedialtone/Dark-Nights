using DarkNights;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula;
using Nebula.Main;
using Nebula.Runtime;
using Nebula.Systems;
using NLog.Fluent;
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Microsoft.Xna.Framework.Graphics.SpriteFont;
using static Nebula.Runtime.DrawUtils;

namespace DarkNights
{
    public class NavSys : Manager
    {
        #region Static
        private static NavSys instance;
        public static NavSys Get => instance;

        public static readonly NLog.Logger log = NLog.LogManager.GetLogger("NAVIGATION");
        #endregion

        public Action<INavNode> NavNodeAdded = delegate { };
        public Action<INavNode> NavNodeRemoved = delegate { };
        public int ClusterSize => (int)(Defs.ChunkSize);
        public int ClusterStep(int level) => ClusterSize * (int)MathF.Pow(3, level);
        public int ClusterFromStep(int size) => (int)(MathF.Log(size / ClusterSize) / MathF.Log(3));

        public Dictionary<int, Cluster>[] clusters = new Dictionary<int, Cluster>[2];

        public List<PathNode> temporaryNodes = new List<PathNode>();

        public override void Init()
        {
            log.Info("> ..");
            instance = this;

            GenerateClusters();

            ApplicationController.Get.Initiate(this);
        }

        public override void Tick()
        {
            base.Tick();
        }

        public override void OnInitialized()
        {
            base.OnInitialized();

            NavigationGizmo navGizmo = new NavigationGizmo();
            navGizmo.Enabled = true;
            navGizmo.DrawNodes = true;
            navGizmo.DrawPaths = true;
            navGizmo.DrawClusters = true;
            navGizmo.DrawRegionEdges = true;
            //navGizmo.DrawRegionBounds = true;
            navGizmo.DrawRegions = true;
        }

        private void GenerateClusters()
        {
            Coordinates minimum = WorldSystem.Get.World.MinimumTile;
            Coordinates maximum = WorldSystem.Get.World.MaximumTile;

            // Generate Clusters
            for (int level = 0; level < clusters.Length; level++)
            {
                clusters[level] = new Dictionary<int, Cluster>();
                int step = ClusterStep(level);

                (int X, int Y) min = ((int)MathF.Floor((float)minimum.X / step), (int)MathF.Floor((float)minimum.Y / step));
                (int X, int Y) max = ((int)MathF.Ceiling((float)maximum.X / step), (int)MathF.Ceiling((float)maximum.Y / step));

                for (int x = min.X; x < max.X; x++)
                {
                    for (int y = min.Y; y < max.Y; y++)
                    {
                        Cluster newCluster = new Cluster(new Coordinates(x * step, y * step), level, step);
                        clusters[level].Add(new Coordinates(x, y).GetHashCode(), newCluster);

                        Region newRegion = new Region(newCluster.Tiles(), level);
                        newCluster.SetGraphs(new Region[] { newRegion });

                        log.Trace($"New Cluster({level}) @ {newCluster.Origin}");
                    }
                }
            }

            for (int depth = 0; depth < clusters.Length; depth++)
            {
                foreach (var kV in clusters[depth])
                {
                    var cluster = kV.Value;
                    Region region = (Region)cluster.GetGraphs.First();

                    List<InterEdge> edges = new List<InterEdge>();
                    foreach (var neighbour in ClusterNeighbours(cluster))
                    {
                        if (neighbour == null) continue;
                        Region exit = (Region)neighbour.GetGraphs.First();

                        GraphEdgeBuilder edgeBuilder = new GraphEdgeBuilder(new IGraph[] { region, exit });
                        edgeBuilder.Tiles = ClusterEdge(cluster, neighbour);

                        var built = edgeBuilder.Build();
                        region.UpdateEdge(exit, built);
                    }                 
                    cluster.RebuildNodes();
                }
            }
        }


        public static NavPath Path(Vector2 A, Vector2 B)
        {
            //instance.instance_PathHierarchal(A, B);
            return instance.instance_BasicAStar(A, B);
        }

        private NavPath instance_BasicAStar(Vector2 A, Vector2 B)
        {
            Coordinates destination = (Coordinates)B;
            if (TryGetNode(B, out INavNode node))
            {
                if (node.Passability == PassabilityFlags.Impassable)
                {
                    log.Warn($"Impassable Node @ {destination}");
                    return null;
                }
            }

            NavPath path = new NavPath();
            PathNode startNode = new PathNode(A, null);
            PathNode endNode = new PathNode(B, null);
            path.Completed += startNode.Clear;
            path.Completed += endNode.Clear;

            var heuristic = new AStarTiles(A, B);
            //path.Completed += () => { ClearPathNode(startNode); ClearPathNode(endNode); };
            path.TilePath = heuristic.Path();
            return path;
        }

        private void instance_PathHierarchal(Vector2 A, Vector2 B)
        {
            Coordinates start = A;
            Coordinates goal = B;

            int commonParent = -1;

            for (int level = clusters.Length-1; level >= 0; level--)
            {
                IGraph layerStart = Graph(start, level);
                IGraph layerGoal = Graph(goal, level);

                if (layerStart == layerGoal)
                {
                    commonParent = level;
                }
            }

            if (commonParent != -1)
            {
                log.Info($"Common parent @ level {commonParent}");
            }
            else
            {

            }

            
        }

        private IEnumerable<Coordinates> DrawLine(Coordinates A, Coordinates B)
        {
            int dx = (int)Math.Abs(B.X - A.X);
            int dy = (int)Math.Abs(B.Y - A.Y);
            int sx = A.X < B.X ? 1 : -1;
            int sy = A.Y < B.Y ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                yield return new Coordinates((int)A.X, (int)A.Y);
                if (A.X == B.X && A.Y == B.Y)
                {
                    yield break;
                }
                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    A.X += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    A.Y += sy;
                }
            }
        }

        public void AddNavNode(INavNode Node)
        {
            INavNode existing = instance_Node(Node.Coordinates, out Cluster cluster);
            if (cluster != null)
            {
                if (cluster.AddNode(Node))
                {
                    NavNodeAdded(Node);
                    log.Debug($"Node added to cluster @ {cluster.ClusterCoordinates}");
                }

            }
        }

        public void RemoveNode(INavNode Node)
        {
            Cluster cluster = Cluster(Node.Coordinates);
            if (cluster != null)
            {
                if (cluster.RemoveNode(Node))
                {
                    NavNodeRemoved(Node);
                    log.Debug($"Node removed from cluster @ {cluster.ClusterCoordinates}");
                }
            }
        }

        public void AddTemporaryNode(PathNode Node)
        {
            temporaryNodes.Add(Node);
        }

        public void ClearTemporaryNode(PathNode node)
        {
            temporaryNodes.Remove(node);
        }

        public void RebuildGraphTree(Coordinates coordinates)
        {
            List<Cluster> rebuilt = new List<Cluster>();
            for (int depth = 0; depth < clusters.Length; depth++)
            {
                Cluster cluster = Cluster(coordinates, depth);
                cluster.BuildRegions();
                rebuilt.Add(cluster);
            }
            foreach (var cluster in rebuilt)
            {
                cluster.RebuildNodes();
                foreach (var neighbour in ClusterNeighbours(cluster))
                {
                    neighbour.RebuildNodes();
                }
            }
        }

        public bool Impassable(Coordinates Coordinates)
        {
            if (TryGetNode(Coordinates, out INavNode Node))
            {
                if (Node.Passability == PassabilityFlags.Impassable)
                {
                    return true;
                }
            }
            return false;
        }

        public bool TryGetNode(Coordinates Coordinates, out INavNode Node)
        {
            Node = NavSys.Node(Coordinates);
            return Node != null;
        }

        public static INavNode Node(Coordinates Coordinates)
        {
            return instance.instance_Node(Coordinates);
        }

        public INavNode instance_Node (Coordinates Coordinates)
        {
            Cluster cluster = Cluster(Coordinates, 0);
            if (cluster != null)
            {
                return cluster.Node(Coordinates);
            }
            return null;
        }

        public INavNode instance_Node(Coordinates Coordinates, out Cluster cluster)
        {
            cluster = Cluster(Coordinates, 0);
            if (cluster != null)
            {
                return cluster.Node(Coordinates);
            }
            return null;
        }

        public Cluster Cluster(Coordinates Coordinates, int level = 0)
        {
            if (level >= clusters.Length) return null;
            int step = ClusterStep(level);
            int X = (int)MathF.Floor((float)Coordinates.X / step);
            int Y = (int)MathF.Floor((float)Coordinates.Y / step);
            Coordinates origin = new Coordinates(X, Y);
            if (clusters[level].TryGetValue(origin.GetHashCode(), out Cluster val))
            {
                return val;
            }
            //log.Trace($"No Cluster @ {origin}");
            return default;
        }

        public IEnumerable<Cluster> ClusterTree(Coordinates Coordinates)
        {
            for (int depth = 0; depth < clusters.Length; depth++)
            {
                yield return Cluster(Coordinates, depth);
            }
        }

        public IGraph Graph(Coordinates Coordinates, int depth = 0)
        {
            int step = ClusterStep(depth);
            int X = (int)MathF.Floor((float)Coordinates.X / step);
            int Y = (int)MathF.Floor((float)Coordinates.Y / step);
            Coordinates origin = new Coordinates(X, Y);
            if (clusters[depth].TryGetValue(origin.GetHashCode(), out Cluster val))
            {
                foreach (var graph in val.GetGraphs)
                {
                    foreach (var tile in graph.Area)
                    {
                        if (tile == Coordinates) return graph;
                    }
                }
            }
            //log.Trace($"No Cluster @ {origin}");
            return null;
        }

        public IEnumerable<Cluster> ClustersInRadius(Vector2 position, float tiles, int depth = 0)
        {
            //tiles *= Defs.UnitPixelSize;
            Vector2 min = new Vector2(position.X - tiles, position.Y - tiles);
            Vector2 max = new Vector2(position.X + tiles, position.Y + tiles);
            Coordinates tileMin = new Coordinates(min);
            Coordinates tileMax = new Coordinates(max);

            Cluster clusterMin = Cluster(tileMin, depth);
            Cluster clusterMax = Cluster(tileMax, depth);

            if (clusterMin == null || clusterMax == null) yield break;

            for (int x = tileMin.X; x <= tileMax.X; x+=clusterMin.Size)
            {
                for (int y = tileMin.Y; y <= tileMax.Y; y+=clusterMin.Size)
                {
                    yield return Cluster(new Coordinates(x, y), depth);
                }
            }
        }

        public IEnumerable<Cluster> ClustersInRadius(Vector2 position, float tiles)
        {
            //tiles *= Defs.UnitPixelSize;
            Vector2 min = new Vector2(position.X - tiles, position.Y - tiles);
            Vector2 max = new Vector2(position.X + tiles, position.Y + tiles);
            Coordinates tileMin = new Coordinates(min);
            Coordinates tileMax = new Coordinates(max);

            Cluster clusterMin = Cluster(tileMin, 0);
            Cluster clusterMax = Cluster(tileMax, 0);

            if (clusterMin == null || clusterMax == null) yield break;

            for (int x = tileMin.X; x <= tileMax.X; x += clusterMin.Size)
            {
                for (int y = tileMin.Y; y <= tileMax.Y; y += clusterMin.Size)
                {
                    for (int depth = 0; depth < clusters.Length; depth++)
                    {
                        yield return Cluster(new Coordinates(x, y), depth);
                    }
                }
            }
        }

        public Cluster ClusterNeighbour(Cluster centre, Coordinates dir)
        {
            Coordinates neighbouringEdge = centre.Origin + new Coordinates(dir.X * centre.Size, dir.Y * centre.Size);
            return Cluster(neighbouringEdge, centre.Depth);
        }

        public IEnumerable<Cluster> ClusterNeighbours(Cluster centre)
        {
            //N
            yield return ClusterNeighbour(centre, Coordinates.North);
            //E
            yield return ClusterNeighbour(centre, Coordinates.East);
            //S
            yield return ClusterNeighbour(centre, Coordinates.South);
            //W
            yield return ClusterNeighbour(centre, Coordinates.West);
        }


        public IEnumerable<Cluster> ClusterChildren(Cluster parent, int level)
        {
            level--;
            if (level < 0) yield break;
            int step = ClusterStep(level);
            (Coordinates min, Coordinates max) bounds = (parent.Minimum, parent.Maximum);
            for (int x = bounds.min.X; x < bounds.max.X; x+=step)
            {
                for (int y = bounds.min.Y; y < bounds.max.Y; y+=step)
                {
                    yield return Cluster(new Coordinates(x,y), level);
                }
            }
        }

        public Cluster ClusterParent(Cluster child)
        {
            int level = child.Depth + 1;
            if (level > clusters.Length) return null;
            return Cluster(child.Origin, level);
        }

        public IEnumerable<(Coordinates From, Coordinates To)> ClusterEdges(Cluster from, Cluster to)
        {
            Vector2 delta = from.Origin - to.Origin;
            bool east = delta.X < 0;
            bool north = delta.Y < 0;
            bool horizontal = delta.Y == 0;
            bool vertical = delta.X == 0;

            if (horizontal)
            {
                int x = 0;
                int xDir = 0;
                if (east) { x = from.Maximum.X; xDir++; }
                else { x = from.Minimum.X - 1; xDir--; }
                for (int y = from.Minimum.Y; y < from.Maximum.Y; y++)
                {
                    yield return (new Coordinates(x - xDir, y), new Coordinates(x, y));
                }
            }
            else if (vertical)
            {
                int y = 0;
                int yDir = 0;
                if (north) { y = from.Maximum.Y; yDir++; }
                else { y = from.Minimum.Y - 1; yDir--; }

                for (int x = from.Minimum.X; x < from.Maximum.X; x++)
                {
                    yield return (new Coordinates(x, y - yDir), new Coordinates(x, y));
                }
            }
        }

        public List<Coordinates>[] ClusterEdge(Cluster from, Cluster to)
        {
            Vector2 delta = from.Origin - to.Origin;
            bool east = delta.X < 0;
            bool north = delta.Y < 0;
            bool horizontal = delta.Y == 0;
            bool vertical = delta.X == 0;

            List<Coordinates>[] edges = new List<Coordinates>[] { new List<Coordinates>(), new List<Coordinates>()};

            if (horizontal)
            {
                int x = 0;
                int xDir = 0;
                if (east) { x = from.Maximum.X; xDir++; }
                else { x = from.Minimum.X - 1; xDir--; }
                for (int y = from.Minimum.Y; y < from.Maximum.Y; y++)
                {
                    edges[0].Add(new Coordinates(x - xDir, y));
                    edges[1].Add(new Coordinates(x, y));
                }
            }
            else if (vertical)
            {
                int y = 0;
                int yDir = 0;
                if (north) { y = from.Maximum.Y; yDir++; }
                else { y = from.Minimum.Y - 1; yDir--; }

                for (int x = from.Minimum.X; x < from.Maximum.X; x++)
                {
                    edges[0].Add(new Coordinates(x, y - yDir));
                    edges[1].Add(new Coordinates(x, y));
                }
            }

            return edges;
        }
    }

    public interface IGraph
    {
        // Graphs this exit out to.
        Coordinates Origin { get; }
        IEnumerable<IGraph> Exits { get; }
        InterEdge[] Edges { get; }
        int Depth { get; }

        IEnumerable<Coordinates> Area { get; }
        IEnumerable<IGraph> GetGraph { get; }
        IEnumerable<IGraph> GraphTree { get; }
        IEnumerable<IGraph> GraphChildren { get; }

        void UpdateEdge(IGraph Graph, IEnumerable<InterEdge> newEdges);
        IEnumerable<InterEdge> GetEdges(IGraph Graph);
    }

    public class Region : IGraph
    {
        public Coordinates Origin { get; private set; }
        public InterEdge[] Edges => GetEdges().ToArray();
        public int Depth { get; private set; }


        public IEnumerable<Coordinates> Area => area;

        private HashSet<Coordinates> area;
        private Dictionary<IGraph, InterEdge[]> edges;
        private IGraph[] subGraphs;
        private IGraph[] exits;

        public Region(IEnumerable<Coordinates> Area,IGraph[] SubGraphs, int depth)
        {
            this.subGraphs = SubGraphs;
            this.area = Area.ToHashSet();
            Origin = Area.First();
            Depth = depth;
        }

        public Region(IEnumerable<Coordinates> Area, int depth)
        {
            this.area = Area.ToHashSet();
            Origin = area.First();
            Depth = depth;
        }

        public IEnumerable<IGraph> GetGraph 
        { 
            get
            {
                yield return this;
            } 
        }

        public IEnumerable<IGraph> GraphChildren
        {
            get
            {
                if (subGraphs == null || subGraphs.Length == 0) yield break;

                foreach (var region in subGraphs)
                {
                    foreach (var graph in region.GetGraph)
                    {
                        yield return graph;
                    }
                }
            }
        }

        public IEnumerable<IGraph> GraphTree
        {
            get
            {
                yield return this;
                if (subGraphs == null || subGraphs.Length == 0) yield break;

                Queue<IGraph> nodes = new Queue<IGraph>();
                foreach (var child in subGraphs)
                {
                    nodes.Enqueue(child);
                }

                while (nodes.Count > 0)
                {
                    IGraph graph = nodes.Dequeue();

                    foreach (var child in graph.GraphChildren)
                    {
                        yield return child;
                        nodes.Enqueue(child);
                    }
                }
            }
        }

        public IEnumerable<IGraph> Exits
        {
            get
            {
                if (exits == null) yield break;
                foreach (var exit in exits)
                {
                    yield return exit;
                }
            }
        }

        public IEnumerable<InterEdge> GetEdges()
        {
            if (edges == null) yield break;
            foreach (var edgeKV in edges)
            {
                foreach (var edge in edgeKV.Value)
                {
                    yield return edge;
                }
            }
        }

        public IEnumerable<InterEdge> GetEdges(IGraph Graph)
        {
            if (edges == null || edges.Count == 0) yield break;
            if (edges.ContainsKey(Graph))
            {
                foreach (var edge in edges[Graph])
                {
                    yield return edge;
                }
            }
            yield break;
        }

        public void UpdateEdge(IGraph Graph, IEnumerable<InterEdge> newEdges)
        {
            if(newEdges == null)
            {
                if(edges != null && edges.ContainsKey(Graph))
                {
                    edges.Remove(Graph);
                }
                else
                {
                    edges = new Dictionary<IGraph, InterEdge[]>();
                }
            }
            else if(edges == null || edges.Count == 0)
            {
                edges = new Dictionary<IGraph, InterEdge[]>
                {
                    { Graph, newEdges.ToArray() }
                };
            }
            else
            {
                if (edges.ContainsKey(Graph))
                {
                    edges[Graph] = newEdges.ToArray();
                }
                else
                {
                    edges.Add(Graph,newEdges.ToArray());
                }
            }

            UpdateExits();
        }

        private void UpdateExits()
        {
            List<IGraph> exitList = new List<IGraph>();
            foreach (var kV in edges)
            {
                if (!(exitList.Contains(kV.Key))) exitList.Add(kV.Key);
            }
            exits = exitList.ToArray();
        }

        public override int GetHashCode()
        {
            return Origin.GetHashCode() * 331;
        }
    }

    /// <summary>
    /// Clusters are responsibly for updating & storing pathfinding data.
    /// </summary>
    public class Cluster
    {
        //public Cluster Parent;
        public Coordinates Origin { get; private set; }
        public int Depth { get; private set; }
        public int Size { get; private set; }

        public List<INavNode> Nodes { get; private set; } = new List<INavNode>();
        public IGraph[] SubGraphs { get; private set; }

        public Coordinates Centre => new Coordinates(Origin.X + Size / 2, Origin.Y + Size / 2);
        public Coordinates Minimum => this.Origin;
        public Coordinates Maximum => new Coordinates(Origin.X + Size, Origin.Y + Size);

        private GraphEdgeNode[] edgeNodes;

        public Coordinates ClusterCoordinates
        {
            get
            {
                int chunkX = (int)MathF.Floor((float)Origin.X / Size);
                int chunkY = (int)MathF.Floor((float)Origin.Y / Size);
                return new Coordinates(chunkX, chunkY);
            }
        }

        public Cluster(Coordinates Origin, int Depth, int Size)
        {
            this.Origin = Origin; this.Depth = Depth; this.Size = Size;
        }

        public INavNode Node(Coordinates Coordinates)
        {
            INavNode ret = null;
            foreach (var node in Nodes)
            {
                if (node.Coordinates == Coordinates)
                {
                    ret = node;
                }
            }
            return ret;
        }

        public bool AddNode(INavNode node)
        {
            Nodes.Add(node);
            if ((node.Passability & PassabilityFlags.Impassable) != 0) RebuildGraphs(node.Coordinates);
            return true;
        }

        public bool RemoveNode(INavNode node)
        {
            bool removed = Nodes.Remove(node);
            if (removed)
            {
                if ((node.Passability & PassabilityFlags.Impassable) != 0) RebuildGraphs(node.Coordinates);
            }
            return removed;
        }

        public void SetGraphs(IGraph[] graphs)
        {
            this.SubGraphs = graphs;
        }

        private void RebuildGraphs(Coordinates source)
        {
            NavSys.Get.RebuildGraphTree(source);
        }

        public void BuildRegions()
        {
            RegionBuilder builder = new RegionBuilder();
            builder.Start = Origin;
            builder.Bounds = (Minimum, Maximum);
            builder.Previous = GetGraphs.ToArray();
            builder.Build(this);

            SetGraphs(builder.Regions);

            // Remove existing edges
            foreach (var invalid in builder.Previous)
            {
                foreach (var exit in invalid.Exits)
                {
                    exit.UpdateEdge(invalid, null);
                }
            }

            // Add the new ones
            foreach (var graph in GetGraphs)
            {
                if (graph.Exits == null) continue;
                foreach (var exit in graph.Exits)
                {
                    exit.UpdateEdge(graph, graph.GetEdges(exit));
                }
            }
        }

        public void RebuildNodes()
        {
            Dictionary<int, List<GraphEdgeNode>> built = new Dictionary<int,List<GraphEdgeNode>>();
            // Generate all the nodes
            foreach (var graph in GetGraphs)
            {
                foreach (var edge in graph.Edges)
                {
                    if (edge.Connected(graph, out IGraph cast, out Coordinates sourceTile, out Coordinates castTile))
                    {
                        GraphEdgeNode node = new GraphEdgeNode(sourceTile, edge.Depth);
                        int hash = graph.GetHashCode();

                        if (built.ContainsKey(hash)) built[hash].Add(node);
                        else built.Add(graph.GetHashCode(), new List<GraphEdgeNode>() { node });
                    }
                }
            }

            // Now connect all of them
            List<GraphEdgeNode> allNodes = new List<GraphEdgeNode>();
            foreach (var kV in built)
            {
                foreach (var node in kV.Value)
                {
                    var clusterNodes = kV.Value.Where(x => x.Coordinates != node.Coordinates).ToArray();
                    var neighbours = new GraphEdgeNode[clusterNodes.Length+1];
                    clusterNodes.CopyTo(neighbours, 0);
                    node.SetNeighbours(clusterNodes);
                    allNodes.Add(node);
                }
            }
            this.edgeNodes = allNodes.ToArray();
        }

        public IEnumerable<IGraph> GetGraphs
        {
            get
            {
                if (SubGraphs == null) yield break;
                foreach (var graph in SubGraphs)
                {
                    yield return graph;
                }
            }
        }

        public IEnumerable<Cluster> Children
        {
            get
            {
                if (this.Depth == 0) yield break;
                foreach (var child in NavSys.Get.ClusterChildren(this, this.Depth))
                {
                    if(child != null) yield return child;
                }
            }
        }

        public IEnumerable<IGraph> GraphChildren
        {
            get
            {
                if (SubGraphs == null || SubGraphs.Length == 0) yield break;

                Queue<Cluster> nodes = new Queue<Cluster>();
                foreach (var child in Children)
                {
                    nodes.Enqueue(child);
                }

                while (nodes.Count > 0)
                {
                    Cluster cluster = nodes.Dequeue();

                    //TO-DO: Recursive
                    foreach (var graph in cluster.GraphTree)
                    {
                        yield return graph;
                    }
                    foreach (var child in cluster.Children)
                    {
                        nodes.Enqueue(child);
                    }
                }
            }
        }

        public IEnumerable<IGraph> GraphTree
        {
            get
            {
                Queue<Cluster> nodes = new Queue<Cluster>();
                foreach (var child in Children)
                {
                    nodes.Enqueue(child);
                }
                foreach (var graph in GetGraphs)
                {
                    yield return graph;
                }

                while (nodes.Count > 0)
                {
                    Cluster cluster = nodes.Dequeue();

                    foreach (var graph in cluster.GraphTree)
                    {
                        yield return graph;
                    }
                    foreach (var child in cluster.Children)
                    {
                        nodes.Enqueue(child);
                    }
                }
            }
        }

        public IEnumerable<Coordinates> Tiles()
        {
            for (int x = Minimum.X; x < Maximum.X; x++)
            {
                for (int y = Minimum.Y; y < Maximum.Y; y++)
                {
                    yield return new Coordinates(x, y);
                }
            }
        }

        public bool Contains(Coordinates Coordinates)
        {
            if (Coordinates.X >= Minimum.X && Coordinates.X < Maximum.X && Coordinates.Y >= Minimum.Y && Coordinates.Y < Maximum.Y) return true;
            return false;
        }

        public IEnumerable<INavNode> AllNodes
        {
            get
            {
                if (Nodes != null)
                {
                    foreach (var node in Nodes)
                    {
                        yield return node;
                    }

                }
                if(edgeNodes != null)
                {
                    foreach (var edge in edgeNodes)
                    {
                        yield return edge;
                    }
                }

            }
        }

        public override int GetHashCode()
        {
            return Origin.GetHashCode() ^ Size.GetHashCode();
        }
    }
}
