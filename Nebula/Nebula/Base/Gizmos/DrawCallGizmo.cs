using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Base;
using System;

namespace Nebula.Main
{
    class DrawCallGizmo : IDrawGizmos
    {
        public bool DrawGizmo { get; private set; }
        private SpriteFont _spriteFont;

        public DrawCallGizmo()
        {
            _spriteFont = Resources.Load<SpriteFont>("FONT/Constantina");
        }

        public void DrawGizmos(SpriteBatch Batch)
        {
            if (DrawGizmo)
            {
                var drawCount = Graphics.Access.GraphicsDevice.Metrics.DrawCount;
                var fps = string.Format("Draws: {0:0.##}", drawCount);
                Batch.DrawString(_spriteFont, fps, new Vector2(1, 15), Color.Yellow);
            }
        }

        public void SetDrawGizmo(bool drawGizmo)
        {
            this.DrawGizmo = drawGizmo;
        }
    }
}