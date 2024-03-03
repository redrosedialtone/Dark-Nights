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

        public Dictionary<int, Cluster>[] clusters = new Dictionary<int, Cluster>[1];

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
            navGizmo.DrawInterEdges = true;
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
                    cluster.BuildSourceGraph();
                }
            }
        }


        public static NavPath Path(Coordinates A, Coordinates B)
        {
            return instance.instance_PathHierarchal(A, B);

            /*var tiles = instance.TilePath(A, B);
            if (tiles == null || tiles.Count == 0) return null;

            NavPath path = new NavPath();
            path.tilePath = tiles;
            return path;*/
        }

        public Stack<Coordinates> TilePath(Coordinates Start, Coordinates Goal)
        {
            if (TryGetNode(Goal, out INavNode node))
            {
                if (node.Passability == PassabilityFlags.Impassable)
                {
                    log.Warn($"Impassable Node @ {Goal}");
                    return null;
                }
            }
            var heuristic = new AStarManhattan();
            var path = heuristic.Path(Start, Goal);

            return new Stack<Coordinates>(path);
        }

        private NavPath instance_PathHierarchal(Vector2 start, Vector2 goal)
        {
            //log.Debug($"Generating Hierchal Path from {start} to {goal}..");

            NavPath finalPath = new NavPath();
            AStarHierarchal astar = new AStarHierarchal();

            int connected = 0;

            Cluster origin = Cluster(start);
            Cluster target = Cluster(goal);
            if (origin == target)
            {
                finalPath.tilePath = TilePath(start, goal);
                return finalPath;
            }
            PathNode startNode = new PathNode(start);
            PathNode endNode = new PathNode(goal);
            finalPath.OnCompleted += startNode.Clear;
            finalPath.OnCompleted += endNode.Clear;

            for (int gIndx = 0; gIndx < origin.InterEdgeNodes.Length; gIndx++)
            {
                var node = origin.InterEdgeNodes[gIndx][0];
                if(astar.NodePath(startNode, node, origin))
                {
                    startNode.ConnectToGraph(origin.InterEdgeNodes[gIndx]);
                    connected++;
                }
            }



            for (int gIndx = 0; gIndx < target.InterEdgeNodes.Length; gIndx++)
            {
                var node = target.InterEdgeNodes[gIndx][0];
                if (astar.NodePath(endNode, node, target))
                {
                    endNode.ConnectToGraph(target.InterEdgeNodes[gIndx]);
                    connected++;
                }
            }

            if(connected != 2)
            {
                log.Warn("Could not connect path nodes to graph!!");
                return null;
            }

            var path = astar.Path(startNode, endNode);

            finalPath.abstractPath = path;

            return finalPath;
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

        public void RebuildGraphTree(Coordinates coordinates)
        {
            List<Cluster> rebuilt = new List<Cluster>();
            for (int depth = 0; depth < clusters.Length; depth++)
            {
                Cluster cluster = Cluster(coordinates, depth);
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

    /// <summary>
    /// Clusters are responsibly for updating & storing pathfinding data.
    /// </summary>
    public class Cluster
    {
        //public Cluster Parent;
        public Coordinates Origin { get; private set; }
        public int Depth { get; private set; }
        public int Size { get; private set; }

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

        public void RebuildGraph(Coordinates Coordinates)
        {
            NavSys.Get.RebuildGraphTree(Coordinates);
        }

        // Build our side of the graph
        public void BuildSourceGraph()
        {
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

                        if (n1 == null) newNodes[bIndx][eIndx] = new AbstractGraphNode(interEdge.Transition.t, this.Depth);
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
            AStarManhattan path = new AStarManhattan();

            List<AbstractGraphNode[]> interEdges = new List<AbstractGraphNode[]>();

            while (openSet.Count > 0)
            {
                AbstractGraphNode next = openSet.First();
                path.NodeSearch(next, openSet, this, out HashSet<AbstractGraphNode> closedSet);
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

        public bool AddNode(INavNode node)
        {
            Nodes.Add(node);
            if ((node.Passability & PassabilityFlags.Impassable) != 0) RebuildGraph(node.Coordinates);
            return true;
        }

        public void AddNodes(IEnumerable<INavNode> nodes, bool rebuild = false)
        {
            Nodes.AddRange(nodes);
        }

        public bool RemoveNode(INavNode node)
        {
            bool removed = Nodes.Remove(node);
            if (removed)
            {
                if ((node.Passability & PassabilityFlags.Impassable) != 0) RebuildGraph(node.Coordinates);
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
            return Origin.GetHashCode() ^ Size.GetHashCode();
        }
    }
}
