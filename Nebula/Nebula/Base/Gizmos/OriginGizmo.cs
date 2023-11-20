using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Base;
using Nebula.Main;
using System;

namespace Nebula
{
    public class OriginGizmo : IDrawGizmos
    {
        public bool DrawGizmo { get; private set; }
        private Texture2D Img;
        private Vector2 Origin;


        public OriginGizmo()
        {
            Img = Resources.Load<Texture2D>("Sprites/Cross");
            Origin = new Vector2(Img.Width / 2.0f, Img.Height / 2.0f);
            Graphics.AddGizmo(this);
        }

        public void DrawGizmos(SpriteBatch batch)
        {
            if (DrawGizmo)
            {
                Vector2 Position = Camera.WorldToScreen(Vector2.Zero);
                batch.Draw(Img, Position, null, Color.White, 0.0f, Origin, 1.0f, SpriteEffects.None, 1.0f);
            }
        }

        public void SetDrawGizmo(bool drawGizmo)
        {
            DrawGizmo = drawGizmo;
        }
    }
}
