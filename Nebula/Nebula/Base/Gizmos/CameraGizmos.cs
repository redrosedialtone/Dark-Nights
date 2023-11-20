using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Base;
using Nebula.Main;
using System;

namespace Nebula
{
	public class PrintCameraPositionGizmo : IDrawGizmos
	{
        public bool DrawGizmo { get; private set; }
        private SpriteFont Font;


		public PrintCameraPositionGizmo()
		{
            Font = Resources.Load<SpriteFont>("FONT/Constantina");
            Graphics.AddGizmo(this);
        }

        public void DrawGizmos(SpriteBatch Batch)
        {
            if (DrawGizmo)
            {
                Vector2 Position = Camera.Get.Position;
                var fps = string.Format("Camera: {2:0.#%} / ({0:0},{1:0})", Position.X, Position.Y, Camera.Get.Zoom);
                Batch.DrawString(Font, fps, new Vector2(1, 30), Color.Yellow);
            }
        }

        public void SetDrawGizmo(bool drawGizmo)
        {
            DrawGizmo = drawGizmo;
        }
    }

    public class DrawCameraPositionGizmo : IDrawGizmos
    {
        public bool DrawGizmo { get; private set; }
        private Texture2D texture;
        private Vector2 Origin;

        public DrawCameraPositionGizmo()
        {
            Graphics.AddGizmo(this);
            texture = new Texture2D(Graphics.Access.GraphicsDevice, 1, 1);
            texture.SetData(new[] { Color.White });
            Origin = new Vector2(1.0f, 1.0f);
        }

        public void DrawGizmos(SpriteBatch Batch)
        {
            if (DrawGizmo)
            {
                Vector2 Position = Camera.Get.Position;
                Batch.Draw(texture,Camera.WorldToScreen(Position),null,Color.White,0f,Origin,new Vector2(2.0f,2.0f),SpriteEffects.None,0f);
            }
        }

        public void SetDrawGizmo(bool drawGizmo)
        {
            DrawGizmo = drawGizmo;
        }
    }

    public class DrawViewportGizmo : IDrawGizmos
    {
        public bool DrawGizmo { get; private set; }

        public DrawViewportGizmo()
        {
            Graphics.AddGizmo(this);
        }

        public void DrawGizmos(SpriteBatch Batch)
        {
            if (DrawGizmo)
            {
                Rectangle cameraBounds = Camera.Get.Bounds;
                Vector2 TL = new Vector2(cameraBounds.Left + 5, cameraBounds.Top + 5);
                Rectangle bounds = new Rectangle((int)TL.X, (int)TL.Y, cameraBounds.Width - 10, cameraBounds.Height - 10);

                TL = new Vector2(bounds.Left+5, bounds.Top+5);
                Vector2 TR = new Vector2(bounds.Right - 5, bounds.Top + 5);
                Vector2 BR = new Vector2(bounds.Right - 5, bounds.Bottom - 5);
                Vector2 BL = new Vector2(bounds.Left + 5, bounds.Bottom - 5);

                Graphics.DrawLine(Batch, TL, TR, Color.Magenta);
                Graphics.DrawLine(Batch, TR, BR, Color.Magenta);
                Graphics.DrawLine(Batch, BR, BL, Color.Magenta);
                Graphics.DrawLine(Batch, BL, TL, Color.Magenta);
            }
        }

        public void SetDrawGizmo(bool drawGizmo)
        {
            DrawGizmo = drawGizmo;
        }
    }
}
