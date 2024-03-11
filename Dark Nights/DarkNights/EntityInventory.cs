using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{
    public class EntityInventory
    {
        public IEnumerable<IItem> Items => m_items;
        private List<IItem> m_items;

        public EntityInventory()
        {
            m_items = new List<IItem>();
        }

        public bool PickupItem(IItem item)
        {
            return EntityController.Get.MoveItemIntoInventory(item, this);
        }

        public bool CanPickupItem(IItem item)
        {
            return true;
        }

        public void AddItem(IItem item)
        {
            m_items.Add(item);
        }

    }
}
