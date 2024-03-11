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

        public void PlaceEntityInWorld(IEntity Entity)
        {
            Coordinates Coordinates = Entity.Coordinates;
            Chunk.Get(Coordinates).AddEntity(Entity);
        }

        public void PlaceEntitiesInWorld(IEntity[] Entities)
        {
            foreach (var entity in Entities)
            {
                if (entity == null) continue;
                Coordinates Coordinates = entity.Coordinates;
                Chunk.Get(Coordinates).AddEntity(entity);
            }
        }

        public void RemoveEntityFromWorld(IEntity Entity)
        {
            Coordinates Coordinates = Entity.Coordinates;
            Chunk.Get(Coordinates).RemoveEntity(Entity);
        }

        public bool GetEntity(Coordinates Coordinates, out IEntity ret)
        {
            ret = null;
            var chunk = Chunk.Get(Coordinates);
            foreach (var entity in chunk.Entities)
            {
                if(entity.Coordinates == Coordinates)
                {
                    ret = entity;
                    return true;
                }
            }
            return false;
        }

        public bool GetEntity<T>(Coordinates Coordinates, out T ret)
        {
            ret = default;
            var chunk = Chunk.Get(Coordinates);
            foreach (var entity in chunk.Entities)
            {
                if (entity.Coordinates == Coordinates)
                {
                    if (entity is T _type)
                    {
                        ret = _type;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool MoveItemIntoInventory(IItem item, EntityInventory Inventory)
        {
            if (Inventory.CanPickupItem(item))
            {
                Inventory.AddItem(item);
                RemoveEntityFromWorld(item);
                return true;
            }
            return false;
        }

    }
}
