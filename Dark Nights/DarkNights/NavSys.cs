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
using System.Drawing;
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

        //public int ClusterStep(int level) => ClusterSize * (int)MathF.Pow(3, level);
        //public int ClusterFromStep(int size) => (int)(MathF.Log(size / ClusterSize) / MathF.Log(3));

        public Dictionary<int, Cluster> ClusterGraph;

        public List<PathNode> temporaryNodes = new List<PathNode>();
        private LinkedList<INavNode> dirtyNodes = new LinkedList<INavNode>();
        private HashSet<Cluster> dirtyClusters = new HashSet<Cluster>();
        private int m_hierarchyLevels = 1;
        private bool m_dirtyNodes = false;
        private bool m_dirtyClusters = false;
        private int[] clusterSteps;

        public override void Init()
        {
            log.Info("> ..");
            instance = this;

            clusterSteps = new int[m_hierarchyLevels];
            for (int i = 0; i < m_hierarchyLevels; i++)
            {
                clusterSteps[i] = ClusterSize * (int)MathF.Pow(3, i);
            }
            ClusterGraph = new Dictionary<int, Cluster>();
            GenerateClusters(m_hierarchyLevels);

            ApplicationController.Get.Initiate(this);
        }



        public override void OnInitialized()
        {
            base.OnInitialized();

            NavigationGizmo navGizmo = new NavigationGizmo();
            navGizmo.Enabled = true;
            navGizmo.DrawNodes = true;
            navGizmo.DrawPaths = true;
            navGizmo.DrawClusters = true;
            //navGizmo.DrawClusterEdges = true;
            navGizmo.DrawClearance = true;
        }

        private void GenerateClusters(int hierarchyLevels)
        {
            Coordinates minimum = WorldSystem.Get.World.MinimumTile;
            Coordinates maximum = WorldSystem.Get.World.MaximumTile;

            int count = 0;
            // Generate Clusters
            for (int level = 0; level < hierarchyLevels; level++)
            {
                int step = clusterSteps[level];

                (int X, int Y) min = ((int)MathF.Floor((float)minimum.X / step), (int)MathF.Floor((float)minimum.Y / step));
                (int X, int Y) max = ((int)MathF.Ceiling((float)maximum.X / step), (int)MathF.Ceiling((float)maximum.Y / step));

                for (int x = min.X; x < max.X; x++)
                {
                    for (int y = min.Y; y < max.Y; y++)
                    {
                        Cluster newCluster = new Cluster(new Coordinates(x * step, y * step), level, step);
                        ClusterGraph.Add(newCluster.GetHashCode(), newCluster);
                        count++;
                    }
                }
            }

            log.Info($"Generated Cluster Map of Size {count} & Depth {hierarchyLevels}");

            // Generate Graphs
            foreach (var clusterKV in ClusterGraph)
            {
                clusterKV.Value.BuildSourceGraph();
            }

            foreach (var clusterKV in ClusterGraph)
            {
                clusterKV.Value.BuildCastGraph();
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (m_dirtyClusters)
            {
                foreach (var cluster in dirtyClusters)
                {
                    cluster.RebuildGraph();
                }
                dirtyClusters.Clear();
                m_dirtyClusters = false;
            }
            if (m_dirtyNodes)
            {
                LinkedList<Cluster> dirty = new LinkedList<Cluster>();
                foreach (var node in dirtyNodes)
                {
                    Cluster cluster = GetCluster(node.Coordinates);
                    if (cluster != null)
                    {
                        cluster.AddNode(node, false);
                        if ((node.Passability & PassabilityFlags.Impassable) != 0)
                        {
                            dirtyClusters.Add(cluster);
                            m_dirtyClusters = true;
                        }
                    }
                }
                dirtyNodes.Clear();
                m_dirtyNodes = false;
            }
        }


        public static NavPath Path(Coordinates start, Coordinates goal, int size = 1)
        {
            return instance.instance_Path(start, goal, size);
        }

        private NavPath instance_Path(Coordinates start, Coordinates goal, int size = 1)
        {
            Cluster origin = GetCluster(start);
            Cluster target = GetCluster(goal);

            if (origin == null || target == null)
            {
                log.Warn($"No Cluster Exists @ {goal}");
                return null;
            }

            int clearance = Clearance(goal);
            if (clearance < size)
            {
                log.Warn($"Impassable Node @ {goal}");
                return null;
            }

            PathNode startNode = new PathNode(start,size);
            PathNode endNode = new PathNode(goal,size);

            AStar astar = new AStar(size, (origin.Minimum, origin.Maximum));
            int connected = 0;

            for (int gIndx = 0; gIndx < origin.InterEdgeNodes.Length; gIndx++)
            {
                var node = origin.InterEdgeNodes[gIndx][0];
                if (astar.Path(startNode.Coordinates, node.Coordinates).Length != 0)
                {
                    startNode.ConnectToGraph(origin.InterEdgeNodes[gIndx]);
                    connected++;
                    break;
                }
            }

            astar.SetBounds((target.Minimum, target.Maximum));

            for (int gIndx = 0; gIndx < target.InterEdgeNodes.Length; gIndx++)
            {
                var node = target.InterEdgeNodes[gIndx][0];
                if (astar.Path(endNode.Coordinates, node.Coordinates).Length != 0)
                {
                    endNode.ConnectToGraph(target.InterEdgeNodes[gIndx]);
                    connected++;
                    break;
                }
            }

            if (connected < 2)
            {
                log.Warn("Could not connect path nodes to graph!!");
                startNode.Clear();
                endNode.Clear();
                return null;
            }

            NavPath entityPath;
            if (origin == target &&
                endNode.Neighbours.FirstOrDefault() == startNode.Neighbours.FirstOrDefault())
            {
                entityPath = CreateTilePath(start, goal, size);
            }
            else
            {
                entityPath = CreateAbstractPath(startNode,endNode,size);
            }
            if (entityPath == null || entityPath.invalid)
            {
                log.Warn("Could not generate path!!");
                startNode.Clear();
                endNode.Clear();
                return null;
            }
            entityPath.OnCompleted += startNode.Clear;
            entityPath.OnCompleted += endNode.Clear;

            return entityPath;
        }

        public NavPath CreateTilePath(Coordinates Start, Coordinates Goal, int size)
        {
            int clearance = Clearance(Goal);
            if(clearance < size)
            {
                log.Warn($"Impassable Node @ {Goal}");
                return null;
            }
            var heuristic = new AStar(size);
            var path = heuristic.Path(Start, Goal);

            return new NavPath(size, new Stack<Vector2>(path));
        }

        public NavPath CreateTilePath(Coordinates Start, Coordinates Goal, int size, Cluster cluster)
        {
            int clearance = Clearance(Goal);
            if (clearance < size)
            {
                log.Warn($"Impassable Node @ {Goal}");
                return null;
            }
            var heuristic = new AStar(size, (cluster.Minimum, cluster.Maximum));
            var path = heuristic.Path(Start, Goal);

            return new NavPath(size, new Stack<Vector2>(path));
        }

        private NavPath CreateAbstractPath(INavNode start, INavNode goal, int size = 1)
        {
            AStarHierarchal astar = new AStarHierarchal(size);
            return new NavPath(size, new Stack<INavNode>(astar.Path(start, goal)));
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
            dirtyNodes.AddFirst(Node);
            m_dirtyNodes = true;
        }

        public void AddNavNodes(IEnumerable<INavNode> Nodes, bool rebuild, int depth = 0)
        {
            foreach (var node in Nodes)
            {
                dirtyNodes.AddFirst(node);
            }
            m_dirtyNodes = true;
        }

        public void RemoveNode(INavNode Node)
        {
            Cluster cluster = GetCluster(Node.Coordinates);
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

        public PassabilityFlags Passability(Coordinates Coordinates)
        {
            if (TryGetNode(Coordinates, out INavNode Node))
            {
                return Node.Passability;
            }
            return PassabilityFlags.Nil;
        }

        public bool Passability(Coordinates Coordinates, PassabilityFlags flags)
        {
            if (TryGetNode(Coordinates, out INavNode Node))
            {
                if ((Node.Passability & flags) != 0) return true;
                return false;
            }
            return false;
        }

        public int Clearance(Coordinates Coordinates)
        {
            var cluster = GetCluster(Coordinates);
            (int x, int y) local = cluster.LocalTile(Coordinates);
            return cluster.Clearance[local.x][local.y];
        }

        public int Traversability(Coordinates Coordinates)
        {
            var cluster = GetCluster(Coordinates);
            (int x, int y) local = cluster.LocalTile(Coordinates);
            return cluster.Traversability[local.x][local.y];
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
            Cluster cluster = GetCluster(Coordinates, depth);
            if (cluster != null)
            {
                return cluster.Node(Coordinates);
            }
            return null;
        }

        public T instance_Node<T>(Coordinates Coordinates, int depth = 0) where T : INavNode
        {
            Cluster cluster = GetCluster(Coordinates, depth);
            if (cluster != null)
            {
                return cluster.Node<T>(Coordinates);
            }
            return default;
        }

        public INavNode instance_Node(Coordinates Coordinates, out Cluster cluster)
        {
            cluster = GetCluster(Coordinates, 0);
            if (cluster != null)
            {
                return cluster.Node(Coordinates);
            }
            return null;
        }

        public void RebuildGraphTree(Coordinates coordinates)
        {
            List<Cluster> rebuilt = new List<Cluster>();
            for (int depth = 0; depth < m_hierarchyLevels; depth++)
            {

                Cluster cluster = GetCluster(coordinates, depth);
                cluster.BuildSourceGraph();
                foreach (var neighbour in ClusterNeighbours(cluster))
                {
                    if (neighbour == null) continue;
                    neighbour.BuildSourceGraph();
                }
                rebuilt.Add(cluster);
            }

            foreach (var cluster in rebuilt)
            {
                cluster.BuildCastGraph();
                foreach (var neighbour in ClusterNeighbours(cluster))
                {
                    if (neighbour == null) continue;
                    neighbour.BuildCastGraph();
                }
            }
        }

        public Cluster GetCluster(Coordinates Coordinates, int depth = 0)
        {
            if (depth >= m_hierarchyLevels) return null;
            int step = clusterSteps[depth];
            int X = (int)MathF.Floor((float)Coordinates.X / step);
            int Y = (int)MathF.Floor((float)Coordinates.Y / step);
            Coordinates origin = new Coordinates(X*step, Y*step);
            int hashCode = Cluster.PredictHashCode(origin, depth);
            if(ClusterGraph.TryGetValue(hashCode, out Cluster val))
            {
                return val;
            }
            return null;
        }

        public IEnumerable<Cluster> ClusterTree(Coordinates Coordinates)
        {
            for (int depth = 0; depth < m_hierarchyLevels; depth++)
            {
                yield return GetCluster(Coordinates, depth);
            }
        }

        public IEnumerable<Cluster> ClustersInRadius(Vector2 position, float tiles, int depth = 0)
        {
            //tiles *= Defs.UnitPixelSize;
            Vector2 min = new Vector2(position.X - tiles, position.Y - tiles);
            Vector2 max = new Vector2(position.X + tiles, position.Y + tiles);
            Coordinates tileMin = new Coordinates(min);
            Coordinates tileMax = new Coordinates(max);

            Cluster clusterMin = GetCluster(tileMin, depth);
            Cluster clusterMax = GetCluster(tileMax, depth);

            if (clusterMin == null || clusterMax == null) yield break;

            for (int x = tileMin.X; x <= tileMax.X; x+=clusterMin.Size)
            {
                for (int y = tileMin.Y; y <= tileMax.Y; y+=clusterMin.Size)
                {
                    yield return GetCluster(new Coordinates(x, y), depth);
                }
            }
        }

        public Cluster ClusterNeighbour(Cluster centre, Coordinates dir)
        {
            Coordinates neighbouringEdge = centre.Origin + new Coordinates(dir.X * centre.Size, dir.Y * centre.Size);
            return GetCluster(neighbouringEdge, centre.Depth);
        }

        public IEnumerable<Cluster> ClusterNeighbours(Cluster centre)
        {
            //N
            var cluster = ClusterNeighbour(centre, Coordinates.North);
            yield return cluster;
            //E
            cluster = ClusterNeighbour(centre, Coordinates.East);
            yield return cluster;
            //S
            cluster = ClusterNeighbour(centre, Coordinates.South);
            yield return cluster;
            //W
            cluster = ClusterNeighbour(centre, Coordinates.West);
            yield return cluster;
        }


        public IEnumerable<Cluster> ClusterChildren(Cluster parent, int depth)
        {
            depth--;
            if (depth < 0) yield break;
            int step = clusterSteps[depth];
            (Coordinates min, Coordinates max) bounds = (parent.Minimum, parent.Maximum);
            for (int x = bounds.min.X; x < bounds.max.X; x+=step)
            {
                for (int y = bounds.min.Y; y < bounds.max.Y; y+=step)
                {
                    yield return GetCluster(new Coordinates(x,y), depth);
                }
            }
        }

        public Cluster ClusterParent(Cluster child)
        {
            int level = child.Depth + 1;
            if (level > m_hierarchyLevels) return null;
            return GetCluster(child.Origin, level);
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

    /// <summary>
    /// Clusters are responsible for updating & storing pathfinding data.
    /// </summary>
    public class Cluster
    {
        //public Cluster Parent;
        public Coordinates Origin { get; private set; }
        public int Depth { get; private set; }
        public int Size { get; private set; }

        public int[][] Clearance { get; private set; }
        public int[][] Traversability { get; private set; }
        public InterEdge[][] InterEdges = new InterEdge[4][];
        public AbstractGraphNode[][] InterEdgeNodes = new AbstractGraphNode[4][];
        public List<INavNode> Nodes { get; private set; } = new List<INavNode>();

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

        public void RebuildGraph()
        {
            NavSys.Get.RebuildGraphTree(Origin);
        }

        // Build our side of the graph
        public void BuildSourceGraph()
        {
            // Generate clearances
            BuildClearanceData();
            BuildTraversabilityData();

            // Generate Inter-edges
            int i = 0;
            foreach (var neighbour in NavSys.Get.ClusterNeighbours(this))
            {
                GraphHelper helper = new GraphHelper(this, neighbour);
                InterEdges[i++] = helper.Entrances();
            }

            // Generate Inter-edge nodes.
            AbstractGraphNode[][] newNodes = new AbstractGraphNode[4][];
            for (int bIndx = 0; bIndx < InterEdges.Length; bIndx++)
            {
                var border = InterEdges[bIndx];
                if (border != null && border.Length > 0)
                {
                    newNodes[bIndx] = new AbstractGraphNode[border.Length];
                    for (int eIndx = 0; eIndx < border.Length; eIndx++)
                    {
                        var interEdge = border[eIndx];
                        AbstractGraphNode n1 = Node<AbstractGraphNode>(interEdge.Transition.t);

                        if (n1 == null)
                        {
                            (int x, int y) local = LocalTile(interEdge.Transition.t);
                            int clearance = Clearance[local.x][local.y];
                            newNodes[bIndx][eIndx] = new AbstractGraphNode(interEdge.Transition.t, this.Depth, clearance);
                        }
                        else newNodes[bIndx][eIndx] = n1;
                    }
                }
            }

            InterEdgeNodes = newNodes;
        }

        // Build Connections
        public void BuildCastGraph()
        {
            for (int bIndx = 0; bIndx < InterEdges.Length; bIndx++)
            {
                var border = InterEdges[bIndx];
                if (border != null && border.Length > 0)
                {
                    for (int eIndx = 0; eIndx < border.Length; eIndx++)
                    {
                        var interEdge = border[eIndx];
                        AbstractGraphNode n1 = InterEdgeNodes[bIndx][eIndx];

                        if(NavSys.Get.TryGetNode<AbstractGraphNode>(interEdge.Transition.sym, out AbstractGraphNode n2, this.Depth))
                        {
                            n1.interEdge = n2;
                            n2.interEdge = n1;
                        }                       
                    }
                }
            }

            var openSet = GraphNodes.ToHashSet();
            AStar path = new AStar(1, (this.Minimum, this.Maximum));

            List<AbstractGraphNode[]> interEdges = new List<AbstractGraphNode[]>();

            while (openSet.Count > 0)
            {
                AbstractGraphNode next = openSet.First();
                path.NodeSearch(next, openSet, out HashSet<AbstractGraphNode> closedSet);
                if (closedSet.Count > 0)
                {
                    interEdges.Add(closedSet.ToArray());
                    openSet.ExceptWith(closedSet);
                }
            }

            int g = 0;
            InterEdgeNodes = new AbstractGraphNode[interEdges.Count][];
            foreach (var group in interEdges)
            {
                InterEdgeNodes[g] = group;
                for (int i = 0; i < group.Length; i++)
                {
                    InterEdgeNodes[g][i] = group[i];
                    var cur = group[i];
                    cur.IntraEdges(group);
                }
                g++;
            }
        }


        public void BuildClearanceData()
        {
            int maxClearance = 4;
            Clearance = new int[Size][];
            for (int i = 0; i < Clearance.Length; i++)
            {
                Clearance[i] = new int[Size];
            }
            foreach (var tile in Tiles())
            {
                int x = tile.X - Minimum.X;
                int y = tile.Y - Minimum.Y;
                int clearance = 1;
                if (NavSys.Get.Passability(tile, PassabilityFlags.Impassable))
                {
                    clearance = 0;
                }
                else
                {
                    while (clearance < maxClearance)
                    {
                        bool blocked = false;
                        foreach (var neighbour in ExpandTileBR(tile, clearance))
                        {
                            if (NavSys.Get.Passability(neighbour, PassabilityFlags.Impassable))
                            {
                                blocked = true;
                                break;
                            }
                        }
                        if (blocked) break;
                        clearance++;
                    }
                }

                Clearance[x][y] = clearance;
            }
        }

        private void BuildTraversabilityData()
        {
            int maxClearance = 4;
            Traversability = new int[Size][];
            for (int i = 0; i < Clearance.Length; i++)
            {
                Traversability[i] = Enumerable.Repeat(maxClearance,Size).ToArray();
            }
            foreach (var obstacle in Nodes)
            {
                Coordinates tile = obstacle.Coordinates;
                (int x, int y) local = LocalTile(tile);
                Traversability[local.x][local.y] = 0;
                int clearance = 1;
                // Not an obstacle
                if ((obstacle.Passability & PassabilityFlags.Impassable) == 0) continue;
                while (clearance < maxClearance)
                {
                    foreach (var neighbour in ExpandTile(tile, clearance))
                    {
                        if (neighbour.X < Minimum.X || neighbour.Y < Minimum.Y ||
                            neighbour.X >= Maximum.X || neighbour.Y >= Maximum.Y) continue;
                        (int x, int y) localN = LocalTile(neighbour);
                        if (Traversability[localN.x][localN.y] > clearance)
                        {
                            Traversability[localN.x][localN.y] = clearance;
                        }
                    }
                    clearance++;
                }
            }
        }

        private IEnumerable<Coordinates> ExpandTileBR(Coordinates tile, int expansions)
        {
            int mX = tile.X + expansions;
            int mY = tile.Y + expansions;

            for (int x = tile.X; x <= mX; x++)
            {
                for (int y = tile.Y; y <= mY; y++)
                {
                    if(x == mX || y == mY)
                    {
                        yield return new Coordinates(x, y);
                    }
                }
            }
        }

        private IEnumerable<Coordinates> ExpandTile(Coordinates tile, int expansions)
        {
            (int min, int max) mX = (tile.X - expansions, tile.X + expansions);
            (int min, int max) mY = (tile.Y - expansions, tile.Y + expansions);

            for (int x = mX.min; x <= mX.max; x++)
            {
                for (int y = mY.min; y <= mY.max; y++)
                {
                    if (x == tile.X && y == tile.Y) continue;
                    yield return new Coordinates(x, y);
                }
            }
        }

        public bool AddNode(INavNode node, bool rebuild = true)
        {
            Nodes.Add(node);
            if (rebuild && (node.Passability & PassabilityFlags.Impassable) != 0) RebuildGraph();
            return true;
        }

        public void AddNodes(IEnumerable<INavNode> nodes, bool rebuild = false)
        {
            Nodes.AddRange(nodes);
            if(rebuild) RebuildGraph();
        }

        public bool RemoveNode(INavNode node)
        {
            bool removed = Nodes.Remove(node);
            if (removed)
            {
                if ((node.Passability & PassabilityFlags.Impassable) != 0) RebuildGraph();
            }
            return removed;
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

        public (int,int) LocalTile(Coordinates tile)
        {
            int x = tile.X - this.Minimum.X;
            int y = tile.Y - this.Minimum.Y;
            return (x, y);
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

        public IEnumerable<AbstractGraphNode> GraphNodes
        {
            get
            {
                foreach (var border in InterEdgeNodes)
                {
                    if (border != null && border.Length > 0)
                    {
                        foreach (var node in border)
                        {
                            yield return node;
                        }
                    }
                }
            }
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
                foreach (var border in InterEdgeNodes)
                {
                    if(border != null && border.Length > 0)
                    {
                        foreach (var node in border)
                        {
                            yield return node;
                        }
                    }
                }
            }
        }

        public override int GetHashCode()
        {
            return Origin.GetHashCode() ^ Depth;
        }

        public static int PredictHashCode(Coordinates origin, int Depth)
        {
            return origin.GetHashCode() ^ Depth;
        }
    }
}
