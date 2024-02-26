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
using System.Text.RegularExpressions;
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

            // Generate Graphs
            for (int depth = 0; depth < clusters.Length; depth++)
            {
                foreach (var kV in clusters[depth])
                {
                    var cluster = kV.Value;
                    Region region = (Region)cluster.GetGraph.First();

                    List<GraphConnection> edges = new List<GraphConnection>();
                    foreach (var neighbour in ClusterNeighbours(cluster))
                    {
                        if (neighbour == null) continue;
                        Region exit = (Region)neighbour.GetGraph.First();

                        GraphEdgeBuilder edgeBuilder = new GraphEdgeBuilder(new IGraph[] { region, exit });
                        edgeBuilder.Tiles = ClusterEdge(cluster, neighbour);

                        var built = edgeBuilder.Build();
                        region.UpdateEdge(exit, built);
                    }
                    cluster.PreGraph();
                }
            }

            // 
            for (int depth = 0; depth < clusters.Length; depth++)
            {
                foreach (var kV in clusters[depth])
                {
                    var cluster = kV.Value;
                    cluster.BuildGraphs();
                }
            }
        }


        public static NavPath Path(Vector2 A, Vector2 B)
        {
            return instance.instance_PathHierarchal(A, B);
            //return instance.instance_BasicAStar(A, B);
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
            path.tilePath = heuristic.Path();
            return path;
        }

        private NavPath instance_PathHierarchal(Vector2 A, Vector2 B)
        {
            Coordinates start = A;
            Coordinates goal = B;

            log.Debug($"Generating Hierchal Path from {start} to {goal}..");

            IGraph[] originGraph = new IGraph[clusters.Length];
            IGraph[] destinationGraph = new IGraph[clusters.Length];

            int depth = -1;

            for (int level = 0; level < clusters.Length; level++)
            {
                originGraph[level] = Graph(start, level);
                destinationGraph[level] = Graph(goal, level);
                if(originGraph == null || destinationGraph == null)
                {
                    log.Warn("No Graphs Found?");
                    return null;
                }

                if (originGraph[level] == destinationGraph[level]) depth = level;
            }

            if (depth != -1) { log.Info($"Common parent @ level {depth}"); }
            else {  depth = clusters.Length; log.Info($"No common parent found. {depth}"); }

            Stack<INavNode>[] abstractPath = new Stack<INavNode>[depth];
            NavPath finalPath = new NavPath();

            while (depth > 0)
            {
                var origin = originGraph[depth-1];
                var destination = destinationGraph[depth-1];

                PathNode startNode = new PathNode(start, origin.GraphEdgeNodes);
                finalPath.Completed += startNode.Clear;


                // High-level abstract path
                if (depth == abstractPath.Length)
                {
                    PathNode endNode = new PathNode(goal, destination.GraphEdgeNodes);
                    finalPath.Completed += endNode.Clear;

                    var heuristic = new AStarHierarchal();
                    abstractPath[depth - 1] = heuristic.Path(startNode, endNode);
                }
                // Low-level abstract path
                else
                {
                    var highLevelPath = abstractPath[depth];
                    INavNode prevNode = startNode;
                    while (highLevelPath.Count > 0)
                    {
                        var nextNode = highLevelPath.Pop();
                        var heuristic = new AStarHierarchal();
                        abstractPath[depth - 1] = heuristic.Path(prevNode, nextNode);
                        prevNode = nextNode;
                    }
                }
                depth--;
            }

            for (int i = abstractPath.Length-1; i >= 0; i--)
            {
                var path = abstractPath[i];
                var prev = start;
                foreach (var cur in path)
                {
                    log.Debug($"Navigating from {prev} to {cur.Coordinates} @ depth {i+1}");
                    prev = cur.Coordinates;
                }
            }
            return null;
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

        public void AddNavNode(INavNode Node, int depth = 0)
        {
            Cluster cluster = Cluster(Node.Coordinates, depth);
            if (cluster != null)
            {
                if (cluster.AddNode(Node))
                {
                    NavNodeAdded(Node);
                    log.Debug($"Node added to cluster @ {cluster.ClusterCoordinates}");
                }
            }
        }

        public void AddNavNodes(IEnumerable<INavNode> Nodes, bool rebuild, int depth = 0)
        {
            Dictionary<int, List<INavNode>> clusters = new Dictionary<int, List<INavNode>>();
            foreach (var node in Nodes)
            {
                Cluster cluster = Cluster(node.Coordinates, depth);
                if(cluster != null)
                {
                    if (clusters.ContainsKey(cluster.GetHashCode())) clusters[cluster.GetHashCode()].Add(node);
                    else clusters[cluster.GetHashCode()] = new List<INavNode>() { node };
                }
            }

            foreach (var kV in clusters)
            {
                var nodes = kV.Value;
                var cluster = Cluster(nodes.First().Coordinates, depth);
                cluster.AddNodes(nodes, rebuild);
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
                cluster.BuildGraphs();
                foreach (var neighbour in ClusterNeighbours(cluster))
                {
                    if (neighbour == null) continue;
                    neighbour.BuildGraphs();
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

        public bool TryGetNode(Coordinates Coordinates, out INavNode Node, int depth = 0)
        {
            Node = NavSys.Node(Coordinates, depth);
            return Node != null;
        }

        public bool TryGetNode<T>(Coordinates Coordinates, out T Node, int depth = 0) where T : INavNode
        {
            Node = instance_Node<T>(Coordinates,depth);
            if (EqualityComparer<T>.Default.Equals(Node, default))
            {
                return false;
            }
            return true;
        }

        public static INavNode Node(Coordinates Coordinates, int depth = 0)
        {
            return instance.instance_Node(Coordinates,depth);
        }

        public INavNode instance_Node (Coordinates Coordinates, int depth = 0)
        {
            Cluster cluster = Cluster(Coordinates, depth);
            if (cluster != null)
            {
                return cluster.Node(Coordinates);
            }
            return null;
        }

        public T instance_Node<T>(Coordinates Coordinates, int depth = 0) where T : INavNode
        {
            Cluster cluster = Cluster(Coordinates, depth);
            if (cluster != null)
            {
                return cluster.Node<T>(Coordinates);
            }
            return default;
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
                foreach (var graph in val.GetGraph)
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
                        var cluster = Cluster(new Coordinates(x, y), depth);
                        if (cluster != null) yield return cluster;
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
            var cluster = ClusterNeighbour(centre, Coordinates.North);
            if (cluster != null) yield return cluster;
            //E
            cluster = ClusterNeighbour(centre, Coordinates.East);
            if (cluster != null) yield return cluster;
            //S
            cluster = ClusterNeighbour(centre, Coordinates.South);
            if (cluster != null) yield return cluster;
            //W
            cluster = ClusterNeighbour(centre, Coordinates.West);
            if (cluster != null) yield return cluster;
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
        int Depth { get; }

        IEnumerable<Coordinates> Area { get; }
        IEnumerable<IGraph> GetGraph { get; }
        IEnumerable<IGraph> GraphTree { get; }
        IEnumerable<IGraph> GraphChildren { get; }
        IEnumerable<GraphEdgeNode> GraphEdgeNodes { get; }
        IEnumerable<GraphConnection> Connections { get; }

        void PreGraph();
        void BuildGraph();
        void UpdateEdge(IGraph Source, GraphConnection paired);
        void UpdateGraphEdge(IGraph Source, IGraph Cast, GraphConnection match);
        GraphConnection Connection(IGraph cast);
    }

    public class Region : IGraph
    {
        public Coordinates Origin { get; private set; }
        public int Depth { get; private set; }

        public IEnumerable<Coordinates> Area => area;

        private HashSet<Coordinates> area;
        private GraphConnection[] connections;
        private IGraph[] subGraphs;

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

        public void UpdateEdge(IGraph Cast, GraphConnection match)
        {
            if (match == null)
            {
                var connection = Connection(Cast);
                if (connection != null)
                {
                    List<GraphConnection> newConnections = connections.ToList();
                    newConnections.Remove(connection);
                    connections = newConnections.ToArray();
                }
            }
            else if (connections == null)
            {
                connections = new GraphConnection[] { match.Pair(this) };
            }
            else
            {
                List<GraphConnection> newConnections = connections.ToList();
                newConnections.Add(match.Pair(this));
                connections = newConnections.ToArray();
            }
        }

        public void UpdateGraphEdge(IGraph lowLevel, IGraph Cast, GraphConnection subConnection)
        {
            if (subConnection == null)
            {
                var connection = Connection(Cast);
                if (connection != null)
                {
                    List<GraphConnection> newConnections = connections.ToList();
                    newConnections.Remove(connection);
                    connections = newConnections.ToArray();
                }
            }
            else if (connections == null)
            {
                connections = new GraphConnection[] { subConnection.Pair(lowLevel,this,this.Depth) };
            }
            else
            {
                List<GraphConnection> newConnections = connections.ToList();
                newConnections.Add(subConnection.Pair(lowLevel, this, this.Depth));
                connections = newConnections.ToArray();
            }
        }

        public void PreGraph()
        {
            foreach (var connection in Connections)
            {
                connection.PreGraph();
            }
        }

        public void BuildGraph()
        {
            if (connections == null || connections.Length == 0) return;
            var allNodes = GraphEdgeNodes.ToArray();
            foreach (var connection in Connections)
            {
                connection.BuildGraph();
                var sourceData = connection.EdgeData(this).ToArray();
                var castData = connection.EdgeData(connection.Cast).ToArray();

                for (int i = 0; i < sourceData.Length; i++)
                {
                    var sourceNode = connection.Node(sourceData[i]);
                    var castNode = connection.Node(castData[i]);

                    if(sourceNode == null || castNode == null) { NavSys.log.Warn("No nodes found??"); break; }

                    var intraEdges = allNodes.Where(x => x != sourceNode).ToList();
                    intraEdges.Add(castNode);

                    sourceNode.SetNeighbours(intraEdges.ToArray());
                }
            }
        }

        public GraphConnection Connection(IGraph cast)
        {
            if (connections == null) return null;
            foreach (var connection in connections)
            {
                if (connection.Cast == cast) return connection;
            }
            return null;
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

        public IEnumerable<GraphConnection> Connections
        {
            get
            {
                if (connections == null) yield break;
                foreach (var connection in connections)
                {
                    yield return connection;
                }
            }
        }

        public IEnumerable<GraphEdgeNode> GraphEdgeNodes
        {
            get
            {
                if (connections == null) yield break;
                foreach (var connection in connections)
                {
                    foreach (var node in connection.EdgeNodes(this))
                    {
                        yield return node;
                    }
                }
            }
        }

        public override int GetHashCode()
        {
            return Origin.GetHashCode() ^ Depth * 331;
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

        public void BuildRegions()
        {
            RegionBuilder builder = new RegionBuilder();
            builder.Start = Origin;
            builder.Bounds = (Minimum, Maximum);
            builder.Previous = GetGraph.ToArray();
            builder.Build(this);

            SetGraphs(builder.Regions);

            // Remove existing edges
            foreach (var invalid in builder.Previous)
            {
                foreach (var connection in invalid.Connections)
                {
                    var exit = connection.Cast;
                    exit.UpdateEdge(invalid, null);
                }
            }

            // Add the new ones
            foreach (var graph in GetGraph)
            {
                foreach (var connection in graph.Connections)
                {
                    var exit = connection.Cast;
                    exit.UpdateEdge(graph, connection);
                }
            }
        }

        public void PreGraph()
        {
            foreach (var graph in GetGraph)
            {
                graph.PreGraph();
            }
        }

        public void BuildGraphs()
        {
            foreach (var graph in GetGraph)
            {
                graph.BuildGraph();
            }
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

        public T Node<T>(Coordinates Coordinates) where T : INavNode
        {
            T ret = default;
            foreach (var node in AllNodes)
            {
                if (node.Coordinates == Coordinates && node is T match) ret = match;
            }
            return ret;
        }

        public bool AddNode(INavNode node)
        {
            Nodes.Add(node);
            if ((node.Passability & PassabilityFlags.Impassable) != 0) RebuildGraphs(node.Coordinates);
            return true;
        }

        public void AddNodes(IEnumerable<INavNode> nodes, bool rebuild = false)
        {
            Nodes.AddRange(nodes);
            if(rebuild) RebuildGraphs(nodes.First().Coordinates);
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

        public IEnumerable<IGraph> GetGraph
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
                foreach (var graph in GetGraph)
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
                foreach (var graph in SubGraphs)
                {
                    foreach (var node in graph.GraphEdgeNodes)
                    {
                        yield return node;
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
