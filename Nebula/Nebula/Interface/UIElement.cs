using Microsoft.Xna.Framework.Graphics;
using System;
using Microsoft.Xna.Framework;
using Nebula.Input;

namespace Nebula.Program.Interface
{
    public interface IUIObject : IPointerEventListener
    {
        /// <summary>
        /// UI Objects are drawn from the Origin (Top-Left)
        /// </summary>
        public Point Origin { get; }
        /// <summary>
        /// The Bounds of the UI Object 
        /// </summary>
        public Rectangle Bounds { get; }
        /// <summary>
        /// The size of the UI Object (width, height)
        /// </summary>
        public Vector2 Size { get; }
        public Point Anchor { get; }

        public IUIObject[] Descendants { get; }

        public void Init();
        public void SetParent(IUIObject Parent);
        public void DrawUI(SpriteBatch Batch);
    }

    public abstract class UIElement : IUIObject
	{
        public Point Origin => origin;
        public Rectangle Bounds => bounds;
        public Vector2 Size => size;
        public Point Anchor => anchor;

        public IPointerEventListener Parent => parentEvent;
        public IPointerEventListener[] Children => childEvents;
        public IUIObject[] Descendants => descendantUIElements;

        protected IUIObject[] descendantUIElements;
        protected IPointerEventListener parentEvent;
        protected IPointerEventListener[] childEvents;  
        protected Point origin;
        protected Rectangle bounds;
        protected Vector2 size;
        protected Point anchor = Point.Zero;

        public virtual void Init()
        {
            if (parentEvent == null)
            {
                Nebula.Main.Interface.RegisterUIEventListener(this);
                Nebula.Main.Interface.RegisterUIDraw(this);
            }
            
            if (descendantUIElements != null)
            {
                foreach (var child in descendantUIElements)
                {
                    child.SetParent(this);
                    child.Init();
                }
            }
            childEvents = descendantUIElements;
        }

        public virtual void SetSize(Point Origin, int Width, int Height)
        {
            origin = Origin;
            size = new Vector2(Width, Height);
            bounds = new Rectangle(origin, size.ToPoint());
        }

        public virtual void SetSize(UIElement Base)
        {
            size = Base.Size;
            bounds = Base.Bounds;
        }

        public virtual IPointerEventListener Intersect(Point mousePos)
        {
            if (Bounds.Contains(mousePos)) return this;
            return null;
        }

        public void SetParent(IUIObject Parent)
        {
            parentEvent = Parent;
        }

        public virtual void DrawUI(SpriteBatch Batch)
        {
            if (descendantUIElements != null && descendantUIElements.Length > 0)
            {
                foreach (var descendant in descendantUIElements)
                {
                    descendant.DrawUI(Batch);
                }
            }
        }
    }
}
