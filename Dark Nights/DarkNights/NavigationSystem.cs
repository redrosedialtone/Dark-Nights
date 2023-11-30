using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        public Action<INavNode> NavNodeUpdate = delegate { };
        public Dictionary<Coordinates, INavNode> nodes = new Dictionary<Coordinates, INavNode>();

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
        }

        public static Stack<INavNode> Path(Vector2 A, Vector2 B)
        {
            return instance.instance_Path(A, B);
        }

        private Stack<INavNode> instance_Path(Vector2 A, Vector2 B)
        {
            Coordinates destination = (Coordinates)B;
            if (nodes.TryGetValue(destination, out INavNode end))
            {
                if(end.Type == NavNodeType.Impassable)
                {
                    log.Warn($"Impassable Node @ {destination}");
                    return null;
                }
            }
            Stack<INavNode> path = new Stack<INavNode>();
            if(end == null)
            {
                end = new AdjacentNode(B, NavNodeType.Destination);
                NavNodeUpdate(end);
            }
            path.Push(end);
            return path;
        }

        public void AddNavNode(INavNode Node)
        {
            if (nodes.TryGetValue(Node.Position, out INavNode cur))
            {

            }
            else
            {
                nodes.Add(Node.Position, Node);
                NavNodeUpdate(Node);
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
                    if (nodes.TryGetValue((x,y), out INavNode cur))
                    {
                        // Already here, ignore
                    }
                    else
                    {
                        AdjacentNode adj = new AdjacentNode(new Coordinates(x,y), NavNodeType.Pathing);
                        AddNavNode(adj);
                    }
                }
            }
        }
    }

    public enum NavNodeType
    {
        Nil = 0,
        Impassable = 1,
        Pathing = 2,
        Edge = 3,
        Destination = 4
    }

    public interface INavNode
    {
        NavNodeType Type { get; }
        Vector2 Position { get; }
        Coordinates Coordinates => Position;
        float Cost { get; }
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
        private List<Circle> nodePolygons;

        public NavigationGizmo()
        {
            Debug.NewWorldGizmo(this);
        }

        public void Update() { }

        public void Draw()
        {
            if (_drawNodes)
            {
                foreach (var poly in nodePolygons)
                {
                    DrawUtils.DrawCircleToWorld(poly, Color.White);
                }
            }
        }

        private void SetDrawWalls()
        {
            if (_drawNodes)
            {
                nodePolygons = new List<Circle>();
                NavigationSystem.Get.NavNodeUpdate += AddWallOutline;
                foreach (var wall in NavigationSystem.Get.nodes)
                {
                    AddWallOutline(wall.Value);
                }
            }
            else
            {
                nodePolygons = null;
                NavigationSystem.Get.NavNodeUpdate -= AddWallOutline;
            }
        }

        private void AddWallOutline(INavNode node)
        {
            Circle circle = new Circle(Coordinates.Centre(node.Coordinates), Defs.UnitPixelSize/4);
            Color col;
            switch (node.Type)
            {
                case NavNodeType.Nil:
                    col = Color.White;
                    break;
                case NavNodeType.Impassable:
                    col = new Color(225, 25, 25, 100);
                    break;
                case NavNodeType.Pathing:
                    col = new Color(225, 225, 25, 100);
                    break;
                case NavNodeType.Edge:
                    col = new Color(25, 25, 225, 100);
                    break;
                case NavNodeType.Destination:
                    col = new Color(25, 225, 25, 100);
                    break;
                default:
                    col = Color.White;
                    break;
            }
            nodePolygons.Add(circle);
            //wallDrawCall += DrawUtils.DrawCircle(circle, col, drawType: DrawType.World);
        }
    }
}
