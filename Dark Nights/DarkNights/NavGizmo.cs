using Nebula.Main;
using Nebula.Runtime;
using Nebula;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace DarkNights
{
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

                var curCluster = NavSys.Get.Cluster(pos, 1);

                foreach (var cluster in NavSys.Get.ClustersInRadius(pos, drawNodesRadius))
                {
                    foreach (var node in cluster.AllNodes)
                    {
                        float length = Vector2.Distance(pos, node.Coordinates);
                        if (length > drawNodesRadius) continue;
                        DrawNode(node, 1.0f - (length / drawNodesRadius));
                        if (node.Neighbours != null)
                        {
                            foreach (var neighbour in node.Neighbours)
                            {
                                float alpha = 1.0f - (length / drawNodesRadius) * 0.5f;
                                var color = Color.White;
                                float thickness = 3.5f;

                                if (curCluster.Contains(neighbour.Coordinates))
                                {
                                    if (cluster.Contains(neighbour.Coordinates)) color = Color.Green;
                                    else color = Color.Red;
                                    if (cluster.Depth != curCluster.Depth) thickness = 2.5f;

                                }
                                else
                                {
                                    if (cluster.Contains(neighbour.Coordinates)) color = Color.Blue;
                                    else color = Color.Red;
                                    if (cluster.Depth != curCluster.Depth) thickness = 2.5f;
                                }
                                DrawUtils.DrawLineToWorld(Coordinates.Centre(node.Coordinates), Coordinates.Centre(neighbour.Coordinates), Color.Multiply(color, alpha), thickness);
                            }
                        }
                    }
                }

                if (NavSys.Get.temporaryNodes != null && NavSys.Get.temporaryNodes.Count > 0)
                {
                    foreach (var node in NavSys.Get.temporaryNodes)
                    {
                        DrawNode(node, 1.0f);
                    }
                }
            }
            if (_drawPaths)
            {
                var character = PlayerController.Get.PlayerCharacter.Movement;
                if (character.MovementPath != null)
                {
                    if (character.MovementPath.tilePath != null)
                    {
                        foreach (var node in character.MovementPath.tilePath)
                        {
                            DrawUtils.DrawRectangleToWorld(node, Defs.UnitPixelSize, Defs.UnitPixelSize, node == character.nextNode ? nextPathColor : activePathColor);
                        }
                    }
                    else
                    {
                        foreach (var node in character.MovementPath.nodePath)
                        {
                            DrawUtils.DrawRectangleToWorld(node.Coordinates, Defs.UnitPixelSize, Defs.UnitPixelSize, node.Coordinates == character.nextNode ? nextPathColor : activePathColor);
                        }
                    }

                    foreach (var inactiveNode in character.MovementPath.lastPath)
                    {
                        DrawUtils.DrawRectangleToWorld(inactiveNode, Defs.UnitPixelSize, Defs.UnitPixelSize, inactivePathColor);
                    }
                }
            }
            if (DrawClusters)
            {
                for (int level = 0; level < NavSys.Get.clusters.Length; level++)
                {
                    var color = clusterLevelColor[level];
                    foreach (var clusterKv in NavSys.Get.clusters[level])
                    {
                        var cluster = clusterKv.Value;
                        Vector2[] corners = new Vector2[4];

                        corners[0] = new Coordinates(0, 0);
                        corners[1] = new Coordinates(cluster.Size, 0);
                        corners[2] = new Coordinates(cluster.Size, cluster.Size);
                        corners[3] = new Coordinates(0, cluster.Size);

                        int offset = NavSys.Get.clusters.Length - level;

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
                    Cluster parent = NavSys.Get.Cluster(pos, level);
                    if (parent == null) return;

                    int regionIndex = 0;
                    foreach (var graph in parent.GetGraph)
                    {
                        Color color = ColorSelector.DebugColor(regionIndex);
                        DrawGraph(graph, new Color(color, 0.1f), regionIndex);
                        regionIndex++;
                    }
                }
                else
                {
                    Cluster cluster = NavSys.Get.Cluster(pos, 0);
                    if (cluster == null) return;
                    int regionIndex = 0;
                    foreach (var region in cluster.GetGraph)
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
                if (graph.Connections == null) return;

                foreach (var connection in graph.Connections)
                {
                    foreach (var edge in connection.AllEdges)
                    {
                        Vector2 A = Coordinates.Centre(edge.Node);
                        //Vector2 B = Coordinates.Centre(connection.Linked.Tile);
                        DrawUtils.DrawCircleToWorld(A, Defs.UnitPixelSize / 2, color);
                        //DrawUtils.DrawCircleToWorld(B, Defs.UnitPixelSize / 2, color);
                        //DrawUtils.DrawLineToWorld(A, B, color);
                        DrawUtils.DrawText($"EXIT {index}", A, color, 0.5f);
                    }

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
            Circle circle = new Circle(Coordinates.Centre(node.Coordinates), Defs.UnitPixelSize / 4);
            Color col;
            switch (node.Passability)
            {
                case PassabilityFlags.Nil:
                    col = Color.White;
                    break;
                case PassabilityFlags.Impassable:
                    col = new Color(1.0f * gradient, 0.1f, 0.1f, 0.8f * gradient);
                    break;
                case PassabilityFlags.Pathing:
                    col = new Color(1.0f * gradient, 1.0f * gradient, 0.1f, 0.8f * gradient);
                    break;
                case PassabilityFlags.Edge:
                    col = new Color(0.1f, 0.1f, 1.0f * gradient, 0.8f * gradient);
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
