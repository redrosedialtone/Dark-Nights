using Microsoft.Xna.Framework;
using Nebula.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{


    public class Character
    {
        public Coordinates Coordinates => Movement.Coordinates;
        public Vector2 Position => Movement.Position;
        public EntityMovement Movement;

        public Character(Coordinates Coordinates)
        {
            Movement = new EntityMovement();
            Movement.SetPosition(Coordinates);
        }

        public void Tick()
        {
            Movement.Move(Time.DeltaTime);
        }
    }
}
