using Microsoft.Xna.Framework;
using Nebula;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{
    public interface IEntity
    {
        string Name { get; }
        Vector2 Position { get; set; }
        Coordinates Coordinates => Position;
        Sprite2D Sprite { get; }
    }

    public abstract class EntityBase : IEntity
    {
        public string Name { get; protected set; }
        public Coordinates Coordinates => Position;
        public Vector2 Position { get; set; }
        public Sprite2D Sprite { get; protected set; }
    }

    public class Tree : EntityBase
    {
        public readonly ImpassableNode Node;

        public Tree(Coordinates coordinates)
        {
            this.Position = coordinates;
            Name = "Tree";
            Sprite = new Sprite2D(AssetManager.Get.LoadTexture($"{AssetManager.SpriteRoot}/shrubsSheet"),
                new Rectangle(0, 0, 32, 32));
            Node = new ImpassableNode(Coordinates);
        }
    }

    public class Shrub1 : EntityBase
    {
        public Shrub1(Coordinates coordinates)
        {
            this.Position = coordinates;
            Name = "Shrub";
            Sprite = new Sprite2D(AssetManager.Get.LoadTexture($"{AssetManager.SpriteRoot}/shrubsSheet"),
                new Rectangle(0, 32, 32, 32));
        }
    }

    public class Shrub2 : EntityBase
    {
        public Shrub2(Coordinates coordinates)
        {
            this.Position = coordinates;
            Name = "Tree";
            Sprite = new Sprite2D(AssetManager.Get.LoadTexture($"{AssetManager.SpriteRoot}/shrubsSheet"),
                new Rectangle(32, 32, 32, 32));
        }
    }

    public class Sapling : EntityBase
    {
        public readonly ImpassableNode Node;
        public Sapling(Coordinates coordinates)
        {
            this.Position = coordinates;
            Name = "Sapling";
            Sprite = new Sprite2D(AssetManager.Get.LoadTexture($"{AssetManager.SpriteRoot}/shrubsSheet"),
                new Rectangle(32, 0, 32, 32));
            Node = new ImpassableNode(Coordinates);
        }
    }
}
