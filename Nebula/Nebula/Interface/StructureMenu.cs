using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Main;
using Nebula.Input;
using System;

namespace Nebula.Program.Interface
{
    public class StructureMenu : InterfaceMenu
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        private UIText text;

        public StructureMenu()
        {            
        } 

        public override void Close()
        {
        }

        public override void Init()
        {
            Point ORIGIN = new Point(25, 25);
            int HEIGHT = (int)(Graphics.RENDER_HEIGHT - ORIGIN.Y * 2);
            int WIDTH = 175;
            SetSize(ORIGIN,WIDTH,HEIGHT);
            // Heading
            text = new UIText("Constantina", "Structure Overview", Color.White, new Point(bounds.Center.X, bounds.Top+10));
            // List Menu
            var listMenu = new StructureMenu_List();
            listMenu.SetSize(new Point(bounds.Left + 5, bounds.Top + 30), WIDTH - 10, HEIGHT - 40);

            descendantUIElements = new IUIObject[] { text, listMenu };

            base.Init();
        }

        public override void DrawUI(SpriteBatch Batch)
        {
            Vector2 point0 = new Vector2(origin.X + width, origin.Y + height);
            Vector2 point1 = new Vector2(origin.X + width, origin.Y);
            Graphics.DrawLine(Batch, point0, point1, Color.White);

            base.DrawUI(Batch);    
        }

        public override void Open()
        {

        }
    }
}
