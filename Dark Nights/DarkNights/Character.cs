using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{
    public class EntityMovementArgs : EventArgs
    {
        public readonly Vector2 newPosition;

        public EntityMovementArgs(Vector2 newPosition)
        {
            this.newPosition = newPosition;
        }
    }

    public class Character
    {
        public Coordinates Coordinates { get; private set; }
        public Vector2 Position { get; private set; }

        public event EventHandler<EntityMovementArgs> OnEntityMovement = delegate { };

        public Character(Coordinates Coordinates)
        {
            this.Coordinates = Coordinates;
            this.Position = Coordinates;
        }

        public void SetPosition(Vector2 Position)
        {
            this.Position = Position;
            this.Coordinates = Position;
            OnEntityMovement(this, new EntityMovementArgs(Position));
        }
    }
}
