using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Base;
using Nebula.Main;
using System;

namespace Nebula
{
	public class MousePositionGizmo : IDrawGizmos
	{
        public bool DrawGizmo { get; private set; }
        private SpriteFont Font;


        public MousePositionGizmo()
        {
            Font = Resources.Load<SpriteFont>("FONT/Constantina");
            Graphics.AddGizmo(this);
        }

        public void DrawGizmos(SpriteBatch Batch)
        {
            if (DrawGizmo)
            {
                Vector2 Position = Cursor.Position;
                Vector2 WorldPos = Camera.ScreenToWorld(Position);
                var fps = string.Format("({0:0},{1:0}) ({2:0},{3:0})", Position.X, Position.Y, WorldPos.X, WorldPos.Y);

                Batch.DrawString(Font, fps, new Vector2(Position.X, Position.Y + 31), Color.White);
            }
        }

        public void SetDrawGizmo(bool drawGizmo)
        {
            DrawGizmo = drawGizmo;
        }
    }
}
