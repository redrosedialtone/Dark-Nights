using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nebula.Base;
using Nebula.Program.Interface;
using Nebula.Main;

namespace Nebula.Main
{
    class Interface : IControl, IDrawUIBatch
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();
        public static Interface Access;
        private NebulaRuntime game;

        private List<IUIObject> DrawnUIPerFrame = new List<IUIObject>();

        public void Create(NebulaRuntime game)
        {
            this.game = game;
            Access = this;
        }

        public void DrawUI(SpriteBatch Batch)
        {
            foreach (var ui in DrawnUIPerFrame)
            {
                ui.DrawUI(Batch);
            }
        }

        public void Draw(GameTime Time)
        {

        }

        public void Initialise()
        {
            Graphics.AddUIDraw(this);
            //DesignMenu designMenu = new DesignMenu();
            //designMenu.Init();        
        }

        public void LoadContent()
        {
            
        }

        public void UnloadContent()
        {
            return;
        }

        public void Update(GameTime gameTime)
        {
            return;
        }

        public static void RegisterUIDraw(IUIObject Window)
        {
            Access.DrawnUIPerFrame.Add(Window);
            log.Debug("Adding UI Draw Call.. " + Access.DrawnUIPerFrame.Count);
        }

        public static void DeregisterUIDraw(IUIObject Window)
        {
            Access.DrawnUIPerFrame.Remove(Window);
        }

        public static void RegisterUIEventListener(IUIObject Listener)
        {            
            Input.AddPointerEventListener(Listener);
            log.Info("Registering UI Listener.." + Listener.ToString());
        }
    }
}
