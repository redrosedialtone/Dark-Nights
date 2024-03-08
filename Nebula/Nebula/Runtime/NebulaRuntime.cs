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
using Nebula.Runtime;
using Debug = Nebula.Runtime.Debug;

namespace Nebula.Main
{
    public class NebulaRuntime : IDisposable
    {
        #region Singleton
        private static readonly NLog.Logger log = NLog.LogManager.GetLogger("ENGINE");
        public static NebulaRuntime Get
        {
            get
            {
                if (instance == null)
                {
                    instance = new NebulaRuntime();
                }

                return instance;
            }
        }
        private static NebulaRuntime instance = null;
        #endregion

        public static INebulaGame Game;
        public static string dataPath => Game.DataPath;
        public static ContentManager Content => Game.Content;
        public static GraphicsDeviceManager GraphicsDeviceMgr => Game.GraphicsDeviceMgr;
        public static GraphicsDevice GraphicsDevice => Game.GraphicsDevice;

        private IControl[] Controls;

        private DrawCallGizmo drawCallGizmo;
        private DebugModeGizmo debugGizmo;
        private FramerateGizmo frameGizmo;

        private NebulaRuntime() { }

        public void Initialize(INebulaGame Controller)
        {
            Game = Controller;
            log.Info("Initializing Runtime Controls...");
            if (Game == null)
            {
                log.Error("!Engine not linked to game; Breaking!");
                throw new NullReferenceException();
            }

            Controls = new IControl[7];
            Controls[0] = Graphics.Get;
            Controls[1] = new Interface();
            Controls[2] = Input.Get;
            Controls[3] = Resources.Get;
            Controls[4] = new Cursor();
            Controls[5] = new NebulaCamera();
            Controls[6] = SpriteBatchRenderer.Get;
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

            drawCallGizmo = new DrawCallGizmo();
            drawCallGizmo.Enabled = true;

            debugGizmo = new DebugModeGizmo();
            debugGizmo.Enabled = true;

            frameGizmo = new FramerateGizmo(2);
            frameGizmo.Enabled = true;

            Debug.Initialised();
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
            Time.Update(gameTime);
            Runtime.Debug.Update();
            foreach (var control in Controls)
            {
                control.Update(gameTime);
            }
        }

        public void PreDraw()
        {
            Graphics.Get.PreDraw();
        }

        public void Draw(GameTime gameTime)
        {
            Time.Frame();
            foreach (var control in Controls)
            {
                control.Draw(gameTime);
            }
        }

        public void PostDraw()
        {
            Graphics.Get.PostDraw();
        }

        public void Run()
        {
            throw new NotImplementedException();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
