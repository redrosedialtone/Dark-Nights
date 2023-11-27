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
using Nebula.Base;

namespace DarkNights
{
    public class ApplicationController : Game, IApplicationController
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

            Nebula = new NebulaRuntime();
        }

        // Start is called before the first frame update
        protected override void Initialize()
        {
            Nebula.Initialize(this);
            instance = this;
            log.Info("> Initialising.. ");
            Systems = new Manager[]
            {
                new WorldSystem(),
                new PlayerController()
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

        public void Create(NebulaRuntime rt)
        {

        }

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
                        sys.Tick(Time.Access);
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
                Nebula.Draw(gameTime);
                base.Draw(gameTime);
            }
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

        public static void AddGizmo(IDrawGizmos Gizmo)
        {
            Graphics.AddGizmo(Gizmo);
        }
    }
}
