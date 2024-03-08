using Microsoft.Xna.Framework.Graphics;
using Nebula.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{
    public class EntityController : Manager
    {
        #region Static
        private static EntityController instance;
        public static EntityController Get => instance;

        private static readonly NLog.Logger log = NLog.LogManager.GetLogger("ENTITY");
        #endregion

        public override void Init()
        {
            log.Info("> ..");
            instance = this;
            ApplicationController.Get.Initiate(this);
        }

        public void PlaceEntity(IEntity Entity)
        {
            Coordinates Coordinates = Entity.Coordinates;
            Chunk.Get(Coordinates).AddTerrain(Entity);
        }

        public void PlaceEntities(IEntity[] Entities)
        {
            foreach (var entity in Entities)
            {
                if (entity == null) continue;
                Coordinates Coordinates = entity.Coordinates;
                Chunk.Get(Coordinates).AddTerrain(entity);
            }

        }

    }
}
