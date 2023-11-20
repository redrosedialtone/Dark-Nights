using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Main;
using System;
using Nebula.Input;

namespace Nebula.Program.Interface
{
    public class DesignMenu : InterfaceMenu, IPointerEventListener
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public DesignMenu()
        {

        }

        public override void Init()
        {
            Point ORIGIN = new Point(25, 25);
            int HEIGHT = (int)(Graphics.RENDER_HEIGHT - ORIGIN.Y * 2);
            int WIDTH = (int)(Graphics.RENDER_WIDTH - ORIGIN.X * 2);
            SetSize(ORIGIN, WIDTH, HEIGHT);

            StructureMenu structureMenu = new StructureMenu();
            descendantUIElements = new IUIObject[] { structureMenu };

            log.Info("WIDTH IS::"+width);

            base.Init();
        }

        public override void Close()
        {
        }

        public override void DrawUI(SpriteBatch Batch)
        {
            Vector2 point0 = new Vector2(origin.X, origin.Y);
            Vector2 point1 = new Vector2(origin.X, origin.Y + height);
            Vector2 point2 = new Vector2(origin.X + width, origin.Y + height);
            Vector2 point3 = new Vector2(origin.X + width, origin.Y);
            Graphics.DrawLine(Batch, point0, point1, Color.WhiteSmoke);
            Graphics.DrawLine(Batch, point1, point2, Color.WhiteSmoke);
            Graphics.DrawLine(Batch, point2, point3, Color.WhiteSmoke);
            Graphics.DrawLine(Batch, point3, point0, Color.WhiteSmoke);

            base.DrawUI(Batch);
        }

        public override void Open()
        {

        } 
    }
}
