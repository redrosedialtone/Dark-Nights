using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public class EntityMovement
    {
        public Vector2 Position { get; protected set; }
        public Coordinates Coordinates { get; protected set; }

        public Vector2 MovementTarget { get; protected set; }
        public bool IsMoving { get; protected set; }
        public bool MovementCompleted { get; protected set; }
        public float DistanceToTarget;

        public float BaseSpeed = 4.75f * Defs.UnitPixelSize;
        public float VelocityMod = 0.72f;
        public float VelocityCap = 1.00f;
        public float Velocity;
        public float MovementSpeed => BaseSpeed * Velocity;

        public event EventHandler<EntityMovementArgs> OnEntityMovement = delegate { };


        public void SetPosition(Vector2 Position)
        {
            this.Position = Position;
            this.Coordinates = Position;
            OnEntityMovement(this, new EntityMovementArgs(Position));
        }

        public void MoveTo(Vector2 Target)
        {
            this.MovementTarget = Target;
            MovementCompleted = false;
            IsMoving = true;

            DistanceToTarget = MathF.Sqrt(
                    MathF.Pow(Position.X - MovementTarget.X, 2) +
                    MathF.Pow(Position.Y - MovementTarget.Y, 2));

        }

        public void Move(float delta)
        {
            if (IsMoving)
            {
                Vector2 nextPosition = Position;
                if (MovementCompleted) { nextPosition = MovementTarget; }
                else
                {
                    Velocity = MathF.Min(Velocity + VelocityMod * delta, VelocityCap);
                    float travel = MovementSpeed * delta;
                    DistanceToTarget -= travel;

                    Vector2 deltaMovement = Vector2.Normalize(MovementTarget - Position);
                    nextPosition += deltaMovement * travel;

                    if (DistanceToTarget <= travel) MovementCompleted = true;
                }
                SetPosition(nextPosition);
                if (Position == MovementTarget) MovementTargetReached();
            }
        }

        private void MovementTargetReached()
        {
            IsMoving = false;
            Position = MovementTarget;
            MovementCompleted = true;
            Velocity = 0;
        }


        private bool Completed =>
            Position == MovementTarget;

    }
}
