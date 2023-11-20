using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Main;
using System;

namespace Nebula
{
	public class Mesh
	{
		public VertexBuffer Vertices { get; private set; }
		public IndexBuffer Indices { get; private set; }
        public Color[] Colors { get; private set; }
		
		public void SetVertices(Vector3[] _vertices)
		{
			int vCount = _vertices.Length;
            if (Colors != null && Colors.Length > 0)
            {
                VertexPositionColor[] vertices = new VertexPositionColor[vCount];
                for (int i = 0; i < vCount; i++)
                {
                    vertices[i] = new VertexPositionColor(_vertices[i], Colors[i]);
                }
                Vertices = new VertexBuffer(Graphics.Access.GraphicsDevice, typeof(VertexPositionColor), vCount, BufferUsage.WriteOnly);
                Vertices.SetData<VertexPositionColor>(vertices);
            }
            else
            {
                VertexPosition[] vertices = new VertexPosition[vCount];
                for (int i = 0; i < vCount; i++)
                {
                    vertices[i] = new VertexPosition(_vertices[i]);
                }
                Vertices = new VertexBuffer(Graphics.Access.GraphicsDevice, typeof(VertexPosition), vCount, BufferUsage.WriteOnly);
                Vertices.SetData<VertexPosition>(vertices);
            }
            
        }

		public void SetTriangles(int[] _indices)
		{
            int iCount = _indices.Length;
            short[] indices = new short[iCount];
            for (int i = 0; i < iCount; i++)
            {
                indices[i] = (short)_indices[i];
            }
            Indices = new IndexBuffer(Graphics.Access.GraphicsDevice, typeof(short), indices.Length, BufferUsage.WriteOnly);
            Indices.SetData(indices);
        }

        public void SetColors(Color[] _colors)
        {
            Colors = _colors;
        }
	}
}
