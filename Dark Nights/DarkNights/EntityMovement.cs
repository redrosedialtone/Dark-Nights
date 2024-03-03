using Microsoft.Xna.Framework;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
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
        public NavPath MovementPath;
        public Vector2? nextNode;
        public bool IsMoving { get; protected set; }
        public bool MovementCompleted { get; protected set; }

        public float BaseSpeed = 4.75f * Defs.UnitPixelSize;

        public event EventHandler<EntityMovementArgs> OnEntityMovement = delegate { };


        public void SetPosition(Vector2 Position)
        {
            this.Position = Position;
            this.Coordinates = Position;
            OnEntityMovement(this, new EntityMovementArgs(Position));
        }

        public void MoveTo(Vector2 Target)
        {
            if (MovementPath != null) MovementPath.Finish();

            MovementPath = NavSys.Path(Position, Target);

            if (MovementPath != null)
            {
                this.MovementTarget = Target;
                MovementCompleted = false;
                IsMoving = true;
            }
        }

        public void Move(float delta)
        {
            if (IsMoving)
            {
                Vector2 nextPosition = Position;
                if (MovementCompleted) { nextPosition = MovementTarget; }
                else
                {
                    Vector2 destination = NextDestination ?? Position;
                    Vector2 deltaMovement = destination - Position;

                    float travel = BaseSpeed * delta;

                    if (deltaMovement.Length() <= travel) { nextPosition = destination; nextNode = null; }
                    else nextPosition += Vector2.Normalize(deltaMovement) * travel;
                }
                SetPosition(nextPosition);
                if (Coordinates == MovementTarget) MovementTargetReached();
            }
        }

        private Vector2? NextDestination
        {
            get
            {
                if (nextNode != null) return nextNode;
                if (MovementPath != null)
                {
                    if (MovementPath.Completed)
                    {
                        return MovementTarget;
                    }
                    nextNode = MovementPath.Next(Coordinates);
                    return nextNode;
                }
                return null;
            }
        }

        private void MovementTargetReached()
        {
            IsMoving = false;
            Position = MovementTarget;
            MovementCompleted = true;
            MovementPath = null;
        }


        private bool Completed =>
            Position == MovementTarget;

    }
}
