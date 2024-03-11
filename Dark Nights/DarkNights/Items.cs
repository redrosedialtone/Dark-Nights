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

    }

    public abstract class ItemBase : EntityBase, IItem
    {

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
