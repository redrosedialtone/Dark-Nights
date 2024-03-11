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
        public Vector2 Facing { get;protected set; }
        public float Rotation { get; protected set; }
        public Coordinates Coordinates { get; protected set; }
        public int Clearance { get; set; }

        public Vector2 MovementTarget { get; protected set; }
        public NavPath MovementPath;
        public Vector2? CurrentMovement;
        public Vector2? NextMovement { get 
            {
                if (MovementPath != null && !MovementPath.invalid) return MovementPath.tilePath.Peek();
                return null;
            } }
        public bool IsMoving { get; protected set; }
        public bool MovementCompleted { get; protected set; }

        public float BaseSpeed = 4.75f * Defs.UnitPixelSize;

        public event EventHandler<EntityMovementArgs> OnEntityMovement = delegate { };
        private Action cbOnMovementEnd;

        public void SetPosition(Vector2 Position)
        {
            this.Position = Position;
            this.Coordinates = Position;
            OnEntityMovement(this, new EntityMovementArgs(Position));
        }

        public void SetRotation(float Rotation)
        {
            this.Rotation = Rotation;
        }

        private void SetFacing(Vector2 dir)
        {
            if (dir == Vector2.Zero) return;
            Facing = dir;
            Rotation = MathF.Atan2(dir.X, -dir.Y);
        }

        public void Stop(Action cbStopEvent)
        {
            this.cbOnMovementEnd = cbStopEvent;
            if (MovementPath != null)
            {
                MovementPath.Clear();
                MovementTarget = Position;
            }
            else
            {
                cbStopEvent();
            }

        }

        public void MoveTo(Vector2 Target)
        {
            if (MovementPath != null) MovementPath.Finish();

            MovementPath = NavSys.Path(Position, Target, Clearance);

            if (MovementPath != null && !MovementPath.Completed)
            {
                this.MovementTarget = Target;
                MovementCompleted = false;
                IsMoving = true;
            }
        }

        public void Tick(float delta)
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

                    if (deltaMovement.Length() <= travel) { nextPosition = destination; CurrentMovement = null; }
                    else nextPosition += Vector2.Normalize(deltaMovement) * travel;
                    SetFacing(deltaMovement);
                }
                SetPosition(nextPosition);
                if (Position == MovementTarget) MovementTargetReached();
            }
        }

        private Vector2? NextDestination
        {
            get
            {
                if (CurrentMovement != null) return CurrentMovement;
                if (MovementPath != null)
                {
                    if (MovementPath.Completed)
                    {
                        return MovementTarget;
                    }
                    CurrentMovement = MovementPath.Next(Coordinates);
                    return CurrentMovement;
                }
                return null;
            }
        }

        private void MovementTargetReached()
        {
            IsMoving = false;
            Position = MovementTarget;
            MovementCompleted = true;
            MovementPath?.Finish();
            MovementPath = null;
        }


        private bool Completed =>
            Position == MovementTarget;

    }
}
