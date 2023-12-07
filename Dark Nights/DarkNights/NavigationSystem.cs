using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula;
using Nebula.Main;
using Nebula.Runtime;
using Nebula.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
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

        public override void Init()
        {
            log.Info("> ..");
            instance = this;

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

        public bool TryGetNode(Coordinates Coordinates, out INavNode Node)
        {
            Node = NavigationSystem.Node(Coordinates);
            return Node != null;
        }

        public static NavPath Path(Vector2 A, Vector2 B)
        {
            return instance.instance_Path(A, B);
        }

        private NavPath instance_Path(Vector2 A, Vector2 B)
        {
            Coordinates destination = (Coordinates)B;

            if (TryGetNode(B, out INavNode node))
            {
                if(node.Type == NavNodeType.Impassable)
                {
                    log.Warn($"Impassable Node @ {destination}");
                    return null;
                }
            }

            var heuristic = new AStar(A, B);
            NavPath path = new NavPath();
            //path.TilePath = heuristic.Path();

            Stack<Coordinates> tilePath = new Stack<Coordinates>();
            foreach (var tile in DrawLine(B,A))
            {
                tilePath.Push(tile);
            }
            path.TilePath = tilePath;
            return path;
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
            INavNode existing = this.Node(Node.Coordinates, out Chunk chunk);
            if (chunk != null)
            {
                if (existing != null)
                {
                    int curType = (int)existing.Type;
                    int newType = (int)Node.Type;

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
            
            if (Node.Type == NavNodeType.Impassable) UpdateImpassable(Node);
        }

        private void UpdateImpassable(INavNode Node)
        {
            int xMin = Node.Coordinates.X - 1;
            int xMax = Node.Coordinates.X + 1;
            int yMin = Node.Coordinates.Y - 1;
            int yMax = Node.Coordinates.Y + 1;
            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    if (x == Node.Coordinates.X && y == Node.Coordinates.Y) continue;
                    AdjacentNode adj = new AdjacentNode(new Coordinates(x, y), NavNodeType.Pathing);
                    AddNavNode(adj);
                }
            }
        }
    }

    public enum NavNodeType
    {
        Nil = 0,
        Destination = 1,
        Edge = 2,
        Pathing = 3,
        Impassable = 4
    }

    public interface INavNode
    {
        NavNodeType Type { get; }
        Vector2 Position { get; }
        Coordinates Coordinates => Position;
        float Cost { get; }
    }

    public class NavPath
    {
        public Stack<Coordinates> TilePath = new Stack<Coordinates>();
        public Stack<Coordinates> lastPath = new Stack<Coordinates>();
        public int Count => TilePath.Count;

        public Coordinates Next()
        {
            var ret = TilePath.Pop();
            lastPath.Push(ret);
            return ret;
        }
    }

    public class AdjacentNode : INavNode
    {
        public NavNodeType Type { get; set; }
        public Vector2 Position { get; set; }
        public float Cost => 1.0f;

        public AdjacentNode(Vector2 Position, NavNodeType Type)
        {
            this.Position = Position; this.Type = Type;
        }
    }

    public class NavigationGizmo : IGizmo
    {
        public bool Enabled { get; set; }
        public bool DrawNodes { get { return _drawNodes; } set { _drawNodes = value; SetDrawWalls(); } }
        private bool _drawNodes = false;
        private float drawNodesRadius = 12.0f;
        public bool DrawPaths { get { return _drawPaths; } set { _drawPaths = value; SetDrawPaths(); } }
        private bool _drawPaths = false;
        private Dictionary<Coordinates, Circle> nodePolygons;
        private Dictionary<Coordinates, Color> nodeColors;

        private Color inactivePathColor = new Color(25, 25, 25, 25);
        private Color activePathColor = new Color(40, 40, 40, 40);
        private Color nextPathColor = new Color(90, 90, 90, 90);

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
        }

        private void SetDrawWalls()
        {
            //if (_drawNodes)
            //{
            //    nodePolygons = new Dictionary<Coordinates, Circle>();
            //    nodeColors = new Dictionary<Coordinates, Color>();
            //    NavigationSystem.Get.NavNodeAdded += DrawNode;
            //    NavigationSystem.Get.NavNodeRemoved += RemoveWallOutline;
            //    /*foreach (var wall in NavigationSystem.Get.nodes)
            //    {
            //        AddWallOutline(wall.Value);
            //    }*/
            //}
            //else
            //{
            //    nodePolygons = null;
            //    NavigationSystem.Get.NavNodeAdded -= DrawNode;
            //    NavigationSystem.Get.NavNodeRemoved -= RemoveWallOutline;
            //}
        }


        private void SetDrawPaths()
        {

        }

        private void DrawNode(INavNode node, float gradient)
        {
            Circle circle = new Circle(Coordinates.Centre(node.Coordinates), Defs.UnitPixelSize/4);
            Color col;
            switch (node.Type)
            {
                case NavNodeType.Nil:
                    col = Color.White;
                    break;
                case NavNodeType.Impassable:
                    col = new Color(1.0f*gradient, 0.1f, 0.1f, 0.8f*gradient);
                    break;
                case NavNodeType.Pathing:
                    col = new Color(1.0f * gradient, 1.0f * gradient, 0.1f, 0.8f*gradient);
                    break;
                case NavNodeType.Edge:
                    col = new Color(0.1f, 0.1f, 1.0f*gradient, 0.8f * gradient);
                    break;
                case NavNodeType.Destination:
                    col = new Color(0.1f, 1.0f * gradient, 0.1f, 0.8f * gradient);
                    break;
                default:
                    col = Color.White;
                    break;
            }
            DrawUtils.DrawCircleToWorld(circle, col);
            //nodePolygons.Add(node.Coordinates, circle);
            //nodeColors.Add(node.Coordinates, col);
            //wallDrawCall += DrawUtils.DrawCircle(circle, col, drawType: DrawType.World);
        }

        //private void RemoveWallOutline(INavNode node)
        //{
        //    if (nodePolygons.TryGetValue(node.Coordinates, out Circle poly))
        //    {
        //        nodePolygons.Remove(node.Coordinates);
        //        nodeColors.Remove(node.Coordinates);
        //    }
        //}
    }
}
