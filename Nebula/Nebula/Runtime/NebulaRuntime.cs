using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Nebula.Systems;
using System.Xml.Linq;

namespace Nebula.Main
{
    public class NebulaRuntime : IDisposable
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetLogger("ENGINE");


        public static NebulaRuntime Access;
        public static IApplicationController Game;
        public static string dataPath => Game.DataPath;
        public static ContentManager Content => Game.Content;
        public static GraphicsDeviceManager GraphicsDeviceMgr => Game.GraphicsDeviceMgr;
        public static GraphicsDevice GraphicsDevice => Game.GraphicsDevice;

        private IControl[] Controls;

        private SmartFramerate frameCountGizmo;
        private DrawCallGizmo drawCallGizmo;
        private OriginGizmo originGizmo;

        public NebulaRuntime()
        {
            Access = this;
        }

        public void Initialize(IApplicationController Controller)
        {
            Game = Controller;
            log.Info("Initializing Runtime Controls...");
            if (Game == null)
            {
                log.Error("!Engine not linked to game; Breaking!");
                throw new NullReferenceException();
            }

            Controls = new IControl[7];
            Controls[0] = new Graphics();
            Controls[1] = new Interface();
            Controls[2] = new Input();
            Controls[3] = new Time();
            Controls[4] = new Resources(Content);
            Controls[5] = new Cursor();
            Controls[6] = new NebulaCamera();
            Controls[0].Create(this);
            Controls[1].Create(this);
            Controls[2].Create(this);
            Controls[3].Create(this);
            Controls[4].Create(this);
            Controls[5].Create(this);
            Controls[6].Create(this);
            foreach (var control in Controls)
            {
                control.Initialise();
            }
            frameCountGizmo = new SmartFramerate(2);
            Graphics.AddGizmo(frameCountGizmo);
            frameCountGizmo.SetDrawGizmo(true);

            drawCallGizmo = new DrawCallGizmo();
            Graphics.AddGizmo(drawCallGizmo);
            drawCallGizmo.SetDrawGizmo(true);

            originGizmo = new OriginGizmo();
            originGizmo.SetDrawGizmo(false);
        }

        public void LoadContent()
        {
            foreach (var control in Controls)
            {
                control.LoadContent();
            }
        }

        public void UnloadContent()
        {
            foreach (var control in Controls)
            {
                control.UnloadContent();
            }
            
        }

        public void Update(GameTime gameTime)
        {
            foreach (var control in Controls)
            {
                control.Update(gameTime);
            }
        }

        public void Draw(GameTime gameTime)
        {
            foreach (var control in Controls)
            {
                control.Draw(gameTime);
            }
        }
        public void Run()
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        //public void ExitApplication()
        //{
        //    log.Info("> APPLICATION CLOSED <");
        //    NLog.LogManager.Shutdown();
        //    Exit();
        //}
    }
}
