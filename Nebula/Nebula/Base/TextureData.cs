using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Nebula.Main;
using Microsoft.Xna.Framework.Graphics;

namespace Nebula
{

    public class TextureData
    {
        public Texture2D Texture { get; private set; }
        public Vector2 Origin { get; private set; }
        public Rectangle RectangleBounds { get; private set; }


        public float LayerDepth { get; private set; }

        public TextureData(Texture2D Texture)
        {
            this.Texture = Texture;
            Origin = new Vector2(0.5f, 0.5f);
            RectangleBounds = new Rectangle(0,0,Texture.Width,Texture.Height);
        }

        public void SetOrigin(Vector2 Origin)
        {
            this.Origin = Origin;
        }
    }

    /*[System.Flags]
    public enum MeshFlags
    {
        Base = 1 << 0, // Vertices & triangles
        UV = 1 << 1,
        Color = 1 << 2,
        ALL = ~(~0 << 3)
    }

    public class MeshData
	{
        public List<Vector3> vertices;

        public List<int> indices;

        public List<Vector2> UVs;

        public List<Color> colors;

        private MeshFlags _flags;

        public Mesh mesh;


        public MeshData(MeshFlags flags = MeshFlags.Base)
        {
            this.vertices = new List<Vector3>();
            this.indices = new List<int>();
            this.colors = new List<Color>();
            this.UVs = new List<Vector2>();
            this._flags = flags;
        }

        // Most of our meshes are planes, so we know a plane
        // is 4 vertices and 6 triangles, most of the time we will
        // know the capacity of our lists.
        public MeshData(int planeCount, MeshFlags flags = MeshFlags.Base)
        {
            this.vertices = new List<Vector3>(planeCount * 4);
            this.indices = new List<int>(planeCount * 6);
            this.colors = new List<Color>(
                (flags & MeshFlags.Color) == MeshFlags.Color ? planeCount * 4 : 0
            );
            this.UVs = new List<Vector2>(
                (flags & MeshFlags.UV) == MeshFlags.UV ? planeCount * 4 : 0
            );
            this._flags = flags;
        }

        public MeshData()
        {
            this.vertices = new List<Vector3>()
            {
                new Vector3(0,1,0),
                new Vector3(1,1,0),
                new Vector3(0,0,0),
                new Vector3(1,0,0)
            };
            this.UVs = new List<Vector2>()
            {
                new Vector2(0,1),
                new Vector2(1,1),
                new Vector2(0,0),
                new Vector2(1,0)
            };

            this.indices = new List<int>()
            {
                0,1,2,
                2,1,3
            };
        }

        /// Add a triangle to our mesh
        public void AddTriangle(int vIndex, int a, int b, int c)
        {
            this.indices.Add(vIndex + a);
            this.indices.Add(vIndex + b);
            this.indices.Add(vIndex + c);
        }

        /// Clear the MeshData
        public void Clear()
        {
            this.vertices.Clear();
            this.indices.Clear();
            this.colors.Clear();
            this.UVs.Clear();
        }

        public void CreateNewMesh()
        {
            this.mesh = new Mesh();
        }

        /// Build our mesh
        public Mesh Build()
        {
            this.CreateNewMesh();
            if (this.vertices.Count > 0 && this.indices.Count > 0)
            {
                if ((this._flags & MeshFlags.Color) == MeshFlags.Color)
                {
                    this.mesh.SetColors(this.colors.ToArray());
                }
                this.mesh.SetVertices(this.vertices.ToArray());
                this.mesh.SetTriangles(this.indices.ToArray());

                if ((this._flags & MeshFlags.UV) == MeshFlags.UV)
                {
                    //this.mesh.SetUVs(0, this.UVs);
                }

                return this.mesh;
            }
            // Output some kind of error here?
            return null;
        }
    }*/
}
