using Microsoft.Xna.Framework;
using Nebula;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{
    public interface IItem : IEntity
    {
        public bool IsInInventory { get; }

        void MoveToInventory(EntityInventory Inventory);
        void RemoveFromInventory(EntityInventory Inventory);
    }

    public abstract class ItemBase : EntityBase, IItem
    {
        public bool IsInInventory { get; private set; }

        public void MoveToInventory(EntityInventory Inventory)
        {
            IsInInventory = true;
        }

        public void RemoveFromInventory(EntityInventory Inventory)
        {
            IsInInventory = false;
        }
    }

    public class Hammer : ItemBase
    {
        public Hammer(Coordinates coordinates)
        {
            this.Position = coordinates;
            Name = "Hammer";
            Sprite = new Sprite2D(AssetManager.Get.LoadTexture($"{AssetManager.SpriteRoot}/items"),
                new Rectangle(0, 0, 32, 32));
        }
    }

    public class Axe : ItemBase
    {
        public Axe(Coordinates coordinates)
        {
            this.Position = coordinates;
            Name = "Axe";
            Sprite = new Sprite2D(AssetManager.Get.LoadTexture($"{AssetManager.SpriteRoot}/items"),
                new Rectangle(32, 0, 32, 32));
        }
    }
}
