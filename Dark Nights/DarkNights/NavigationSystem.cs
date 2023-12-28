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
    public class NavigationSystem : Manager
    {
        #region Static
        private static NavigationSystem instance;
        public static NavigationSystem Get => instance;

        public static readonly NLog.Logger log = NLog.LogManager.GetLogger("NAVIGATION");
        #endregion

        public Action<INavNode> NavNodeAdded = delegate { };
        public Action<INavNode> NavNodeRemoved = delegate { };
        public int ClusterSize => (int)(Defs.ChunkSize);
        public int ClusterStep(int level) => ClusterSize * (int)MathF.Pow(3, level);
        public int ClusterFromStep(int size) => (int)(MathF.Log(size / ClusterSize) / MathF.Log(3));

        public Dictionary<int, Cluster>[] clusters = new Dictionary<int, Cluster>[2];

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
                        Cluster newCluster = new Cluster();
                        newCluster.Origin = new Coordinates(x*step, y*step);
                        newCluster.Size = step;
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

                    List<GraphEdge> edges = new List<GraphEdge>();
                    foreach (var neighbour in ClusterNeighbours(cluster))
                    {
                        if (neighbour == null) continue;
                        Region exit = (Region)neighbour.GetGraphs.First();

                        GraphEdgeBuilder edgeBuilder = new GraphEdgeBuilder(new IGraph[] { region, exit });
                        edgeBuilder.Tiles = ClusterEdge(cluster, neighbour);

                        var built = edgeBuilder.Build();
                        foreach (var newEdge in built)
                        {
                            edges.Add(newEdge);
                        }
                    }
                    region.MatchEdges(edges);
                }
            }
        }


        public static NavPath Path(Vector2 A, Vector2 B)
        {
            instance.instance_PathHierarchal(A, B);
            return instance.instance_Path(A, B);
        }

        private NavPath instance_Path(Vector2 A, Vector2 B)
        {
            Coordinates destination = (Coordinates)B;
            if (TryGetNode(B, out INavNode node))
            {
                if(node.Passability == PassabilityFlags.Impassable)
                {
                    log.Warn($"Impassable Node @ {destination}");
                    return null;
                }
            }

            var heuristic = new AStarTiles(A, B);
            NavPath path = new NavPath();
            path.TilePath = heuristic.Path();

            /*Stack<Coordinates> tilePath = new Stack<Coordinates>();
            foreach (var tile in DrawLine(B,A))
            {
                tilePath.Push(tile);
            }
            path.TilePath = tilePath;*/
            return path;
        }

        private void instance_PathHierarchal(Vector2 A, Vector2 B)
        {
            Coordinates start = A;
            Coordinates goal = B;

            int commonParent = -1;

            for (int level = clusters.Length-1; level >= 0; level--)
            {
                Cluster layerStart = Cluster(start, level);
                Cluster layerGoal = Cluster(goal, level);

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


        private void CalculateRegions(Coordinates Coordinates)
        {
            Cluster cluster = Cluster(Coordinates);

            RegionBuilder builder = new RegionBuilder();
            builder.Start = cluster.Origin;
            builder.Bounds = (cluster.Minimum, cluster.Maximum);
            builder.Previous = cluster.GetGraphs.ToArray();
            builder.Build(cluster);

            cluster.SetGraphs(builder.Regions);

            // Remove existing edges
            foreach (var invalid in builder.Previous)
            {
                foreach (var exit in invalid.Exits)
                {
                    exit.UpdateEdge(invalid, null);
                }
            }

            // Add the new ones
            foreach (var graph in cluster.GetGraphs)
            {
                foreach (var exit in graph.Exits)
                {
                    exit.UpdateEdge(graph, graph.GetEdges(exit));
                }
            }

            Cluster parentCluster = ClusterParent(cluster);

            builder = new RegionBuilder();
            builder.Start = parentCluster.Origin;
            builder.Bounds = (parentCluster.Minimum, parentCluster.Maximum);
            builder.Previous = parentCluster.GetGraphs.ToArray();
            builder.Build(parentCluster);

            parentCluster.SetGraphs(builder.Regions);

            // Remove existing edges
            foreach (var invalid in builder.Previous)
            {
                foreach (var exit in invalid.Exits)
                {
                    exit.UpdateEdge(invalid, null);
                }
            }

            // Add the new ones
            foreach (var graph in parentCluster.GetGraphs)
            {
                foreach (var exit in graph.Exits)
                {
                    exit.UpdateEdge(graph, graph.GetEdges(exit));
                }
            }
        }

        public void AddNavNode(INavNode Node)
        {
            INavNode existing = this.Node(Node.Coordinates, out Chunk chunk);
            if (chunk != null)
            {
                if (existing != null)
                {
                    int curType = (int)existing.Passability;
                    int newType = (int)Node.Passability;

                    if (newType > curType)
                    {
                        chunk.Nodes.Remove(existing);
                        chunk.Nodes.Add(Node);
                        NavNodeRemoved(existing);
                    }
                }
                else
                {
                    chunk.Nodes.Add(Node);
                    NavNodeAdded(Node);
                }
                log.Debug($"Node added to chunk @ {chunk.ChunkCoordinates}");
            }

            if ((Node.Passability & PassabilityFlags.Impassable) == PassabilityFlags.Impassable)
            {
                AddClearanceNodes(Node);
                CalculateRegions(Node.Coordinates);
            }
        }


        private void AddClearanceNodes(INavNode Node)
        {
            foreach (var tile in Node.Coordinates.Adjacent)
            {
                NavNode adj = new NavNode(tile, PassabilityFlags.Pathing);
                AddNavNode(adj);
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
            Node = NavigationSystem.Node(Coordinates);
            return Node != null;
        }

        public static INavNode Node(Coordinates Coordinates)
        {
            Chunk chunk = Chunk.Get(Coordinates);
            if (chunk != null)
            {
                return chunk.Node(Coordinates);
            }
            return null;
        }

        public INavNode Node(Coordinates Coordinates, out Chunk chunk)
        {
            chunk = Chunk.Get(Coordinates);
            if (chunk != null)
            {
                return chunk.Node(Coordinates);
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

        public Cluster ClusterNeighbour(Cluster centre, Coordinates dir)
        {
            Coordinates neighbouringEdge = centre.Origin + new Coordinates(dir.X * centre.Size, dir.Y * centre.Size);
            return Cluster(neighbouringEdge, ClusterFromStep(centre.Size));
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
            int level = child.Level + 1;
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
        int Depth { get; }
        IGraph[] Exits { get; }
        Coordinates Origin { get; }
        GraphEdge[] Edges { get; }

        IEnumerable<Coordinates> Area { get; }
        IEnumerable<IGraph> GetGraph { get; }
        IEnumerable<IGraph> GraphTree { get; }
        IEnumerable<IGraph> GraphChildren { get; }

        void UpdateEdge(IGraph Graph, IEnumerable<GraphEdge> newEdges);
        void MatchEdges(IEnumerable<GraphEdge> newEdges);
        IEnumerable<GraphEdge> GetEdges(IGraph Graph);
    }

    public class Region : IGraph
    {
        public int Depth { get; private set; }
        public GraphEdge[] Edges => GetEdges().ToArray();
        public IGraph[] Exits => exits;
        private IGraph[] exits;
        public Coordinates Origin { get; private set; }
        private Dictionary<IGraph, GraphEdge[]> edges;

        public IEnumerable<Coordinates> Area => area;
        private HashSet<Coordinates> area;
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

        public IEnumerable<GraphEdge> GetEdges()
        {
            foreach (var edgeKV in edges)
            {
                foreach (var edge in edgeKV.Value)
                {
                    yield return edge;
                }
            }
        }

        public IEnumerable<GraphEdge> GetEdges(IGraph Graph)
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

        public void UpdateEdge(IGraph Graph, IEnumerable<GraphEdge> newEdges)
        {
            if(newEdges == null)
            {
                if(edges != null && edges.ContainsKey(Graph))
                {
                    edges.Remove(Graph);
                }
            }
            else if(edges == null || edges.Count == 0)
            {
                edges = new Dictionary<IGraph, GraphEdge[]>
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

        public void MatchEdges(IEnumerable<GraphEdge> newEdges)
        {
            if (edges == null || edges.Count == 0)
            {
                Dictionary<IGraph, List<GraphEdge>> temp = new Dictionary<IGraph, List<GraphEdge>>();
                foreach (var edge in newEdges)
                {
                    bool connected = edge.Source == this || edge.Cast == this;
                    if (!connected) continue;
                    var other = edge.Source == this ? edge.Cast : edge.Source;
                    if (temp.ContainsKey(other)) temp[other].Add(edge);
                    else temp.Add(other, new List<GraphEdge>() { edge });
                }

                edges = new Dictionary<IGraph, GraphEdge[]>();
                foreach (var edge in temp)
                {
                    edges.Add(edge.Key, edge.Value.ToArray());
                }
            }
            else
            {
                Dictionary<IGraph, List<GraphEdge>> temp = new Dictionary<IGraph, List<GraphEdge>>();
                foreach (var edge in newEdges)
                {
                    var other = edge.Source == (IGraph)this ? edge.Cast : edge.Source;
                    if (temp.ContainsKey(other)) temp[other].Add(edge);
                    else temp.Add(other, new List<GraphEdge>() { edge });
                }
                foreach (var edgeKv in edges)
                {
                    if (temp.ContainsKey(edgeKv.Key)) continue;
                    else temp.Add(edgeKv.Key, edgeKv.Value.ToList());
                }

                edges = new Dictionary<IGraph, GraphEdge[]>();
                foreach (var edge in temp)
                {
                    edges.Add(edge.Key, edge.Value.ToArray());
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

    public class Cluster
    {
        //public Cluster Parent;
        public Coordinates Origin { get; set; }
        public int Size;
        public Coordinates ClusterCoordinates
        {
            get
            {
                int chunkX = (int)MathF.Floor((float)Origin.X / Size);
                int chunkY = (int)MathF.Floor((float)Origin.Y / Size);
                return new Coordinates(chunkX, chunkY);
            }
        }

        public int Level => NavigationSystem.Get.ClusterFromStep(this.Size);
        public Coordinates Centre => new Coordinates(Origin.X + Size / 2, Origin.Y + Size / 2);
        public Coordinates Minimum => this.Origin;
        public Coordinates Maximum => new Coordinates(Origin.X + Size, Origin.Y + Size);

        private IGraph[] subGraphs;

        public IEnumerable<IGraph> GetGraphs
        {
            get
            {
                if (subGraphs == null) yield break;
                foreach (var graph in subGraphs)
                {
                    yield return graph;
                }
            }
        }

        public IEnumerable<Cluster> Children
        {
            get
            {
                if (this.Level == 0) yield break;
                foreach (var child in NavigationSystem.Get.ClusterChildren(this, this.Level))
                {
                    if(child != null) yield return child;
                }
            }
        }

        public IEnumerable<IGraph> GraphChildren
        {
            get
            {
                if (subGraphs == null || subGraphs.Length == 0) yield break;

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
                //if (subGraphs == null || subGraphs.Length == 0) yield break;

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


        public void SetGraphs(IGraph[] graphs)
        {
            this.subGraphs = graphs;
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

        public override int GetHashCode()
        {
            return Origin.GetHashCode() ^ Size.GetHashCode();
        }
    }

    public class NavigationGizmo : IGizmo
    {
        public bool Enabled { get; set; }

        public bool DrawNodes { get { return _drawNodes; } set { _drawNodes = value; } }
        private bool _drawNodes = false;
        private float drawNodesRadius = 12.0f;

        public bool DrawPaths { get { return _drawPaths; } set { _drawPaths = value; } }
        private bool _drawPaths = false;

        public bool DrawClusters { get; set; }

        public bool DrawRegions { get; set; }
        public bool DrawRegionBounds { get; set; }
        public bool DrawRegionEdges { get; set; }


        private Color inactivePathColor = new Color(25, 25, 25, 25);
        private Color activePathColor = new Color(40, 40, 40, 40);
        private Color nextPathColor = new Color(90, 90, 90, 90);
        private Color[] clusterLevelColor = new Color[] { new Color(0.1f, 0.1f, 0.5f, 0.2f), new Color(0.1f, 0.5f, 0.1f, 0.2f), new Color(0.5f, 0.1f, 0.1f, 0.2f) };

        public NavigationGizmo()
        {
            Debug.NewWorldGizmo(this);
            drawNodesRadius *= Defs.UnitPixelSize;
        }

        public void Update() { }

        public void Draw()
        {
            if (_drawNodes)
            {
                Vector2 pos = Camera.ScreenToWorld(Cursor.Position);
                foreach (var chunk in WorldSystem.Get.ChunksInRadius(pos, drawNodesRadius))
                {
                    if (chunk == null) continue;
                    foreach (var node in chunk.Nodes)
                    {
                        float length = Vector2.Distance(pos, node.Position);
                        if (length > drawNodesRadius) continue;
                        DrawNode(node, 1.0f - (length / drawNodesRadius));
                    }
                }
            }
            if (_drawPaths)
            {
                var character = PlayerController.Get.PlayerCharacter.Movement;
                if (character.MovementPath != null)
                {
                    foreach (var node in character.MovementPath.TilePath)
                    {
                        DrawUtils.DrawRectangleToWorld(node, Defs.UnitPixelSize, Defs.UnitPixelSize, node==character.nextNode ? nextPathColor : activePathColor);
                    }
                    foreach (var inactiveNode in character.MovementPath.lastPath)
                    {
                        DrawUtils.DrawRectangleToWorld(inactiveNode, Defs.UnitPixelSize, Defs.UnitPixelSize, inactivePathColor);
                    }
                }
            }
            if (DrawClusters)
            {
                for (int level = 0; level < NavigationSystem.Get.clusters.Length; level++)
                {
                    var color = clusterLevelColor[level];
                    foreach (var clusterKv in NavigationSystem.Get.clusters[level])
                    {
                        var cluster = clusterKv.Value;
                        Vector2[] corners = new Vector2[4];

                        corners[0] = new Coordinates(0, 0);
                        corners[1] = new Coordinates(cluster.Size, 0);
                        corners[2] = new Coordinates(cluster.Size, cluster.Size);
                        corners[3] = new Coordinates(0, cluster.Size);

                        int offset = NavigationSystem.Get.clusters.Length - level;

                        corners[0] += new Vector2(5f, 5f) * offset;
                        corners[1] += new Vector2(-5f, 5f) * offset;
                        corners[2] += new Vector2(-5f, -5f) * offset;
                        corners[3] += new Vector2(5f, -5f) * offset;

                        var polygon = new Polygon(corners, cluster.Origin);

                        DrawUtils.DrawPolygonOutlineToWorld(polygon, color, 3.0f);
                    }
                }
            }
            if (DrawRegions)
            {
                Vector2 pos = Camera.ScreenToWorld(Cursor.Position);
                int level = 1;
                if (level >= 1)
                {
                    Cluster parent = NavigationSystem.Get.Cluster(pos, 1);
                    if (parent == null) return;

                    int regionIndex = 0;
                    foreach (var graph in parent.GetGraphs)
                    {
                        Color color = ColorSelector.DebugColor(regionIndex);
                        DrawGraph(graph, new Color(color, 0.1f), regionIndex);
                        regionIndex++;
                    }
                }
                else
                {
                    Cluster cluster = NavigationSystem.Get.Cluster(pos, 0);
                    if (cluster == null) return;
                    int regionIndex = 0;
                    foreach (var region in cluster.GetGraphs)
                    {
                        Color color = ColorSelector.DebugColor(regionIndex);
                        DrawGraph(region, new Color(color, 0.1f), regionIndex);
                        regionIndex++;
                    }
                }


            }
        }

        private void DrawGraph(IGraph graph, Color color, int index)
        {
            foreach (var tile in graph.Area)
            {
                DrawUtils.DrawRectangleToWorld(tile, Defs.UnitPixelSize, Defs.UnitPixelSize, color);
            }
            if (DrawRegionEdges)
            {
                if (graph.Edges == null) return;

                foreach (var edge in graph.Edges)
                {
                    Vector2 A = Coordinates.Centre(edge.InterEdge.From);
                    Vector2 B = Coordinates.Centre(edge.InterEdge.To);
                    DrawUtils.DrawCircleToWorld(A, Defs.UnitPixelSize / 2, color);
                    DrawUtils.DrawCircleToWorld(B, Defs.UnitPixelSize / 2, color);
                    DrawUtils.DrawLineToWorld(A, B, color);
                    DrawUtils.DrawText($"EXIT {index}", B, color, 0.5f);
                }

                //foreach (var edge in graph.Edges)
                //{
                //    if (edge.Edges == null) return;

                //    foreach (var exit in edge.Edges)
                //    {
                //        Vector2 A = Coordinates.Centre(exit.From);
                //        Vector2 B = Coordinates.Centre(exit.To);
                //        DrawUtils.DrawCircleToWorld(A, Defs.UnitPixelSize / 2, color);
                //        DrawUtils.DrawCircleToWorld(B, Defs.UnitPixelSize / 2, color);
                //        DrawUtils.DrawLineToWorld(A, B, color);
                //        DrawUtils.DrawText($"EXIT {index}", B, color, 0.5f);
                //    }



                //}
                //var tile = graph.Area.FirstOrDefault();
                //DrawUtils.DrawText($"R{index}", tile, Color.White, 1.0f);
            }
        }

        private void DrawNode(INavNode node, float gradient)
        {
            Circle circle = new Circle(Coordinates.Centre(node.Coordinates), Defs.UnitPixelSize/4);
            Color col;
            switch (node.Passability)
            {
                case PassabilityFlags.Nil:
                    col = Color.White;
                    break;
                case PassabilityFlags.Impassable:
                    col = new Color(1.0f*gradient, 0.1f, 0.1f, 0.8f*gradient);
                    break;
                case PassabilityFlags.Pathing:
                    col = new Color(1.0f * gradient, 1.0f * gradient, 0.1f, 0.8f*gradient);
                    break;
                case PassabilityFlags.Edge:
                    col = new Color(0.1f, 0.1f, 1.0f*gradient, 0.8f * gradient);
                    break;
                case PassabilityFlags.Destination:
                    col = new Color(0.1f, 1.0f * gradient, 0.1f, 0.8f * gradient);
                    break;
                default:
                    col = Color.White;
                    break;
            }
            DrawUtils.DrawCircleToWorld(circle, col);
        }
    }
}
