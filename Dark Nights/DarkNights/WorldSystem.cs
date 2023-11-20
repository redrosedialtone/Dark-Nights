using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebula;
using Nebula.Systems;

namespace DarkNights
{
    public class WorldSystem : Manager
    {
        #region Static
        private static WorldSystem instance;
        public static WorldSystem Get => instance;

        private static readonly NLog.Logger log = NLog.LogManager.GetLogger("WORLD");

        public List<string> Logs { get; set; } = new List<string>();
        public LoggingLevel LoggingLevel { get => _loggingLevel; set => _loggingLevel = value; }
        private LoggingLevel _loggingLevel = LoggingLevel.Warn;

        #endregion

        public static int CHUNK_SIZE;

        public override void Init()
        {
            log.Info("> ...");
            base.Init();
        }
    }
}
