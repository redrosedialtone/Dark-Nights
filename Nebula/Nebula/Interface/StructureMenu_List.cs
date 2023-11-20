using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Main;
using Nebula.Input;
using System;

namespace Nebula.Program.Interface
{
    public class StructureMenu_List : InterfaceMenu
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        private InteractableList text;

        public override void Close()
        {
        }

        public override void Init()
        {
            text = new InteractableList("[Add New]", new Point(bounds.Left, bounds.Top + 10));

            descendantUIElements = new IUIObject[] { text };

            base.Init();
        }

        public override void DrawUI(SpriteBatch Batch)
        {
            base.DrawUI(Batch);
        }

        public override void Open()
        {

        }
    }
}
