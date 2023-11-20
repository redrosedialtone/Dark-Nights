using System;
using System.Collections;
using System.Collections.Generic;
using NLog;
using Nebula;
using Nebula.Main;
using Microsoft.Xna.Framework;

namespace Nebula.Systems
{
    /// <summary>
    /// Main Thread
    /// </summary>
    public class ApplicationController : IControl
    {
        #region Static
        private static ApplicationController instance;
        public static ApplicationController Get => instance;

        private static readonly NLog.Logger log = NLog.LogManager.GetLogger("APPLICATION");

        public List<string> Logs { get; set; } = new List<string>();
        public LoggingLevel LoggingLevel { get => _loggingLevel; set => _loggingLevel = value; }
        private LoggingLevel _loggingLevel = LoggingLevel.Warn;

        #endregion

        public static string DataPath;

        private Manager[] Systems;

        private Manager[] InitializedSystems;
        private int InitializedCount;
        private bool Initialised;
        
        // Start is called before the first frame update
        public void Initialise()
        {
            instance = this;
            DataPath = Runtime.dataPath + "/Data/";
            log.Info("> Application Initialising <");
            Systems = new Manager[]
            {
                //new Player(),
                //new PhysicsModule(),
                /*new ResourceManager(),
                new LoadingSystem(),
                new NavigationSystem(),
                new EntitySystem(),
                new WorldSystem(),     
                new CursorSystem(),
                new Camera(),*/

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
        }

        // Update is called once per frame
        public void Update(GameTime gameTime)
        {
            if (Initialised)
            {
                foreach (var sys in Systems)
                {
                    sys.Tick();
                }
            }       
        }

        public void Initialized(Manager sys)
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
            log.Info("> System Initialized! <");
        }

        public void Create(Runtime rt)
        {

        }

        public void LoadContent()
        {
            
        }

        public void UnloadContent()
        {
            
        }

        public void Draw(GameTime gameTime)
        {
            
        }
    }
}
