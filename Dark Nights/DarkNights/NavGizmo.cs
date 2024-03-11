using Nebula.Main;
using Nebula.Runtime;
using Nebula;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System.Reflection;
using System.Xml.Linq;
using System.Reflection.Emit;

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

        public bool DrawClusterEdges { get; set; }
        public bool DrawClusters { get; set; }
        public bool DrawClearance { get; set; }

        private Color inactivePathColor = new Color(25, 25, 25, 25);
        private Color activePathColor = new Color(40, 40, 60, 40);
        private Color nextPathColor = new Color(125, 90, 90, 90);
        private Color[] clusterLevelColor = new Color[] { new Color(0.1f, 0.1f, 0.5f, 0.2f), new Color(0.1f, 0.5f, 0.1f, 0.2f), new Color(0.5f, 0.1f, 0.1f, 0.2f) };

        public NavigationGizmo()
        {
            Debug.NewWorldGizmo(this);
            drawNodesRadius *= Defs.UnitPixelSize;
        }

        public void Update() { }

        public void Draw()
        {
            Vector2 pos = Camera.ScreenToWorld(Cursor.Position);
            var curCluster = NavSys.Get.GetCluster(pos, 0);

            if (_drawNodes)
            {
                foreach (var cluster in NavSys.Get.ClustersInRadius(pos, drawNodesRadius))
                {
                    Color debugColor;
                    for (int gIndx = 0; gIndx < cluster.InterEdgeNodes.Length; gIndx++)
                    {
                        debugColor = ColorSelector.DebugColor(gIndx);
                        var group = cluster.InterEdgeNodes[gIndx];
                        if (group == null) continue;
                        for (int nIndx = 0; nIndx < group.Length; nIndx++)
                        {
                            var node = group[nIndx];
                            float length = Vector2.Distance(pos, node.Coordinates);
                            if (length > drawNodesRadius) continue;
                            DrawNode(node, 1.0f - (length / drawNodesRadius));
                            if (DrawClusterEdges)
                            {
                                if (node.Neighbours != null)
                                {
                                    foreach (var neighbour in node.Neighbours)
                                    {
                                        var drawColor = Color.White;
                                        if (neighbour == null) continue;
                                        float alpha = 1.0f - (length / drawNodesRadius) * 0.5f;
                                        float thickness = 3.5f;

                                        if (curCluster.Contains(neighbour.Coordinates))
                                        {
                                            if (cluster.Contains(neighbour.Coordinates)) { drawColor = debugColor; alpha *= 2.0f; }
                                            else drawColor = Color.Red;
                                            if (cluster.Depth != curCluster.Depth) thickness = 2.5f;

                                        }
                                        else
                                        {
                                            if (cluster.Contains(neighbour.Coordinates)) drawColor = Color.Blue;
                                            else drawColor = Color.Red;
                                            if (cluster.Depth != curCluster.Depth) thickness = 2.5f;
                                        }
                                        DrawUtils.DrawLineToWorld(Coordinates.Centre(node.Coordinates), Coordinates.Centre(neighbour.Coordinates), Color.Multiply(drawColor, alpha), thickness);
                                    }
                                }
                            }

                        }
                    }
                    foreach (var node in cluster.Nodes)
                    {
                        float length = Vector2.Distance(pos, node.Coordinates);
                        if (length > drawNodesRadius) continue;
                        DrawNode(node, 1.0f - (length / drawNodesRadius));
                        if (DrawClusterEdges)
                        {
                            if (node.Neighbours != null)
                            {
                                foreach (var neighbour in node.Neighbours)
                                {
                                    if (neighbour == null) continue;
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
                foreach (var character in PlayerController.Get.Characters)
                {
                    if (character.Movement.MovementPath != null)
                    {
                        var path = character.Movement.MovementPath;
                        if (path.tilePath != null)
                        {
                            var prev = path.tilePath.FirstOrDefault();
                            foreach (var node in path.tilePath)
                            {
                                DrawUtils.DrawLineToWorld(Coordinates.Centre(prev), Coordinates.Centre(node), node == character.Movement.NextMovement ? nextPathColor : activePathColor, 3);
                                DrawUtils.DrawRectangleToWorld(node, Defs.UnitPixelSize, Defs.UnitPixelSize, node == character.Movement.NextMovement ? nextPathColor : activePathColor);
                                prev = node;
                            }
                        }
                        else if (path.abstractPath != null)
                        {
                            var prev = path.abstractPath.FirstOrDefault();
                            foreach (var node in path.abstractPath)
                            {
                                DrawUtils.DrawLineToWorld(Coordinates.Centre(prev.Coordinates), Coordinates.Centre(node.Coordinates), node.Coordinates == character.Movement.NextMovement ? nextPathColor : activePathColor, 3);
                                DrawUtils.DrawRectangleToWorld(node.Coordinates, Defs.UnitPixelSize, Defs.UnitPixelSize, node.Coordinates == character.Movement.NextMovement ? nextPathColor : activePathColor);
                                prev = node;
                            }
                        }
                        if (path.lastPath != null)
                        {
                            var prev = path.lastPath.FirstOrDefault();
                            foreach (var inactiveNode in path.lastPath)
                            {
                                DrawUtils.DrawLineToWorld(Coordinates.Centre(prev), Coordinates.Centre(inactiveNode), inactivePathColor, 3);
                                DrawUtils.DrawRectangleToWorld(inactiveNode, Defs.UnitPixelSize, Defs.UnitPixelSize, inactivePathColor);
                                prev = inactiveNode;
                            }
                        }

                    }
                }              
            }
            if (DrawClusters)
            {
                var color = clusterLevelColor[0];
                foreach (var clusterKv in NavSys.Get.ClusterGraph)
                {
                    var cluster = clusterKv.Value;
                    Vector2[] corners = new Vector2[4];

                    corners[0] = new Coordinates(0, 0);
                    corners[1] = new Coordinates(cluster.Size, 0);
                    corners[2] = new Coordinates(cluster.Size, cluster.Size);
                    corners[3] = new Coordinates(0, cluster.Size);

                    int offset = cluster.Depth;

                    corners[0] += new Vector2(5f, 5f) * offset;
                    corners[1] += new Vector2(-5f, 5f) * offset;
                    corners[2] += new Vector2(-5f, -5f) * offset;
                    corners[3] += new Vector2(5f, -5f) * offset;

                    var polygon = new Polygon(corners, cluster.Origin);

                    DrawUtils.DrawPolygonOutlineToWorld(polygon, color, 3.0f);
                }
            }
            if (DrawClearance && curCluster != null)
            {
                foreach (var tile in curCluster.Tiles())
                {
                    int x = tile.X - curCluster.Minimum.X;
                    int y = tile.Y - curCluster.Minimum.Y;
                    int clearance = curCluster.Clearance[x][y];
                    int traversability = curCluster.Traversability[x][y];

                    DrawUtils.DrawText($"({clearance},{traversability})", tile, Color.Black, 0.5f);
                }
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
