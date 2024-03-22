using Microsoft.Xna.Framework;
using Nebula.Main;
using Nebula.Systems;
using Nebula;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using System.Threading;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

namespace DarkNights
{
    public class ApplicationController : Game, INebulaGame
    {
        #region Static
        private static ApplicationController instance;
        public static ApplicationController Get => instance;

        public static NebulaRuntime Nebula;

        private static readonly NLog.Logger log = NLog.LogManager.GetLogger("APPLICATION");
        #endregion

        public static GraphicsDeviceManager DeviceMgr => instance.GraphicsDeviceMgr;
        public static ContentManager GameContent => Get.Content;
        public static string Path => instance.DataPath;
        public GraphicsDeviceManager GraphicsDeviceMgr { get; private set; }

        public string DataPath { get; set; }

        private Manager[] Systems;

        private Manager[] InitializedSystems;
        private int InitializedCount;
        private bool Initialised;

        public ApplicationController()
        {
            log.Info("> ..");

            GraphicsDeviceMgr = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;

            Nebula = NebulaRuntime.Get;
        }

        // Start is called before the first frame update
        protected override void Initialize()
        {
            Nebula.Initialize(this);
            instance = this;
            log.Info("> Initialising.. ");
            Systems = new Manager[]
            {
                new AssetManager(),
                new InterfaceController(),
                new EntityController(),
                new WorldSystem(),
                new PlayerController(),
                new NavSys(),
                new TaskSystem(),
            };
            if (Systems != null)
            {
                InitializedSystems = new Manager[Systems.Length];
                foreach (var sys in Systems)
                {
                    log.Trace("[System Init..]");
                    sys.Init();
                }
            }

            base.Initialize();
        }

        public void Initiate(Manager sys)
        {
            InitializedSystems[InitializedCount] = sys;
            InitializedCount++;
            if (InitializedCount >= InitializedSystems.Length)
            {
                log.Trace("[System Initialized..]");
                FinishInit();
            }
        }

        public void FinishInit()
        {
            Initialised = true;
            foreach (var sys in InitializedSystems)
            {
                sys.OnInitialized();
            }
            log.Info("> ..Initialised!");
        }

        public void Create(NebulaRuntime rt) { }

        protected override void LoadContent()
        {
            Nebula.LoadContent();
        }

        // Update is called once per frame
        protected override void Update(GameTime gameTime)
        {
            if (IsActive)
            {
                Nebula.Update(gameTime);
                if (Initialised)
                {
                    foreach (var sys in Systems)
                    {
                        sys.Update();
                    }
                    if (Time.TickEnabled)
                    {
                        foreach (var sys in Systems)
                        {
                            sys.Tick();
                        }
                    }         
                }
            }


            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                log.Info("Goodbye Cruel World!");
                ExitApplication();
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            if (IsActive)
            {
                PreDraw();

                MainDraw(gameTime);

                PostDraw();
            }
        }

        private void PreDraw()
        {
            Nebula.PreDraw();
        }

        private void MainDraw(GameTime time)
        {
            Nebula.Draw(time);
            if (Initialised)
            {
                foreach (var sys in Systems)
                {
                    sys.Draw();
                }
            }
            base.Draw(time);
        }

        private void PostDraw()
        {
            Nebula.PostDraw();
        }

        protected override void UnloadContent()
        {
            Nebula.UnloadContent();
            Content.Unload();
        }

        public void ExitApplication()
        {
            log.Info("> APPLICATION CLOSED ");
            NLog.LogManager.Shutdown();
            Exit();
        }
    }
}
