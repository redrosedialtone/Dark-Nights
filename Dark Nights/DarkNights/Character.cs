using Microsoft.Xna.Framework;
using Nebula;
using Nebula.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{


    public class Character : IEntity
    {
        public string Name { get; private set; }
        public Coordinates Coordinates => Movement.Coordinates;
        public Vector2 Position { get { return Movement.Position; } set { Movement.SetPosition(value); } }
        public float Rotation { get { return Movement.Rotation; } set { Movement.SetRotation(value); } }
        public Sprite2D Sprite { get; private set; }
        public EntityMovement Movement;

        public Character(Coordinates Coordinates)
        {
            Movement = new EntityMovement();
            Movement.SetPosition(Coordinates);
            Movement.Clearance = 2;
            Name = "Jerry";
            Sprite = new Sprite2D(AssetManager.Get.LoadTexture($"{AssetManager.SpriteRoot}/Jerry"),
                new Rectangle(0, 0, 64, 64), new Vector2(32,32));
        }

        public void Tick()
        {
            Movement.Move(Time.DeltaTime);
        }
    }
}
