using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Base;
using Nebula.Main;

namespace Nebula.Program.Interface
{
    public abstract class GLWindow : UIElement, IDrawGizmos, IDrawUIBatch
    {

        protected bool draw = false;
        protected int width => (int)Size.X;
        protected int height => (int)Size.Y;

        public bool DrawGizmo { get; private set; } = true;

        public GLWindow()
        {

        }

        public override void Init()
        {
            Graphics.AddGizmo(this);
            base.Init();
        }

        public abstract void Open();
        public abstract void Close();
        public virtual void Exit()
        {
            
        }

        public void DrawGizmos(SpriteBatch Batch)
        {
            Vector2 point0 = new Vector2(origin.X, origin.Y);
            Vector2 point1 = new Vector2(origin.X, origin.Y + height);
            Vector2 point2 = new Vector2(origin.X + width, origin.Y + height);
            Vector2 point3 = new Vector2(origin.X + width, origin.Y);
            Graphics.DrawLine(Batch, point0, point1, Color.Green);
            Graphics.DrawLine(Batch, point1, point2, Color.Green);
            Graphics.DrawLine(Batch, point2, point3, Color.Green);
            Graphics.DrawLine(Batch, point3, point0, Color.Green);
            //Batch.Draw(SimpleTexture, new Rectangle((int)origin.X, (int)origin.Y, width, height), Color.Green);
        }

        public void SetDrawGizmo(bool drawGizmo)
        {
            DrawGizmo = drawGizmo;
        }

        protected virtual bool Within(Point point) =>
             bounds.Contains(point);
        protected virtual bool Outside(Point point) =>
             !(bounds.Contains(point));
        public virtual Vector2 WindowBounds(Vector2 point)
        {
            int _x = (int)Math.Min(Math.Max(point.X, origin.X), origin.X + width);
            int _y = (int)Math.Min(Math.Max(point.Y, origin.Y), origin.Y + height);
            return new Vector2(_x, _y);
        }
        protected virtual bool CanDrawTo(Point point)
        {
            return Within(point);
        }
    }
}
