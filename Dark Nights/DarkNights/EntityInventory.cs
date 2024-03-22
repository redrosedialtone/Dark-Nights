using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{
    public class EntityInventory
    {
        public IEntity Entity { get; private set; }

        public IItem leftHand;
        public IItem rightHand;
        public IItem backpack;

        public IEnumerable<IItem> Items => m_items;
        private List<IItem> m_items;

        public Action cbOnInventoryChange;

        public EntityInventory(IEntity entity)
        {
            m_items = new List<IItem>();
            Entity = entity;
        }

        public bool AttemptItemPickup(IItem item)
        {
            return EntityController.Get.MoveWorldItemIntoInventory(item, this);
        }

        public bool AttemptDropItem(IItem item, Coordinates tile)
        {
            return EntityController.Get.MoveInventoryItemIntoWorld(item, this, tile);
        }

        public bool CanPickupItem(IItem item)
        {
            if (m_items.Count <= 3) return true;
            return false;
        }

        public bool CanDropItem(IItem item)
        {
            return true;
        }

        public void AddItem(IItem item)
        {
            m_items.Add(item);
            if (leftHand == null) leftHand = item;
            else if (rightHand == null) rightHand = item;
            else backpack = item;
            item.MoveToInventory(this);
            cbOnInventoryChange?.Invoke();
        }

        public void RemoveItem(IItem item)
        {
            if (m_items.Remove(item))
            {
                if (leftHand == item) leftHand = null;
                else if (rightHand == item) rightHand = null;
                else backpack = null;
                item.RemoveFromInventory(this);
                cbOnInventoryChange?.Invoke();
            }
        }

    }
}
