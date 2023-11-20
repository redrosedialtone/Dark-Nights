using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Nebula
{
    public class Transform
    {
        public Transform Parent;
        public List<Transform> Children;

        public Vector2 Position
        {
            get
            {
                if (Parent != null) { return Parent.Position + Vector2.Transform(_localPosition, Parent.RotationMatrix()); }
                else { return _localPosition; }
            }
            set
            {
                if (Parent != null) { _localPosition = value - Parent.Position; }
                else { _localPosition = value; }
                OnPositionChange();
            }
        }
        public Vector2 LocalPosition
        {
            get { return _localPosition; }
            set { _localPosition = value; OnPositionChange(); }
        }

        public Vector2 RotationVector
        {
            get
            {
                return new Vector2((float)Math.Cos(Rotation), -(float)Math.Sin(Rotation));
            }
        }
        public float Rotation
        {
            get 
            {
                if (Parent != null) { return Parent.Rotation + _localRotation; }
                else { return _localRotation; }
            }
            set
            {
                _localRotation = value;
            }
        }
        public float LocalRotation
        {
            get { return _localRotation; }
            set { _localRotation = value; }
        }

        public Vector2 Scale { get; set; } = Vector2.One;
        public Matrix Matrix { get; set; } = Matrix.Identity;

        public Action onPositionChange;


        private Vector2 _localPosition;
        private float _localRotation;

        public Transform() { }
        public Transform(Transform toCopy)
        {
            this.Position = toCopy.Position;
            this.Rotation = toCopy.Rotation;
            this.Scale = toCopy.Scale;
        }


        public static Transform Compose(Transform a, Transform b)
        {
            Transform result = new Transform();
            Vector2 transformedPosition = a.TransformVector(b.Position);
            result.Position = transformedPosition;
            result.Rotation = a.Rotation + b.Rotation;
            result.Scale = a.Scale * b.Scale;
            return result;
        }

        public void SetParent(Transform Parent)
        {
            if (this == Parent) return;
            this.Parent = Parent;
            _localPosition = this.Position - Parent.Position;
            _localRotation = this.Rotation - Parent.Rotation;
            Parent.SetChild(this);
        }

        public void SetChild(Transform Child)
        {
            if (Children == null) Children = new List<Transform>() { Child };
            else { Children.Add(Child); }
        }

        public static void Lerp(ref Transform key1, ref Transform key2, float amount, ref Transform result)
        {
            result.Position = Vector2.Lerp(key1.Position, key2.Position, amount);
            result.Scale = Vector2.Lerp(key1.Scale, key2.Scale, amount);
            result.Rotation = MathHelper.Lerp(key1.Rotation, key2.Rotation, amount);
        }

        public Vector2 TransformVector(Vector2 point)
        {
            Vector2 result = Vector2.Transform(point, Matrix.CreateRotationZ(Rotation));
            result *= Scale;
            result += Position;
            return result;
        }

        public Vector2 TransformVector(Vector2 point, float offset)
        {
            Vector2 result = Vector2.Transform(point, Matrix.CreateRotationZ(Rotation + offset));
            result *= Scale;
            result += Position;
            return result;
        }

        public float TransformAngle(float angle)
        {
            if (Parent != null) return angle - Parent.Rotation;
            return angle;
        }

        public Rectangle CastRectangle(Rectangle cast)
        {
            return new Rectangle((int)(cast.X + Position.X), (int)(cast.Y + Position.Y), cast.Width, cast.Height);
        }

        public Matrix RotationMatrix()
        {
            return Matrix.CreateRotationZ(Rotation);
        }

        public Matrix LocalRotationMatrix()
        {
            return Matrix.CreateRotationZ(LocalRotation);
        }

        public float ClampRotation(float val)
        {
            float _t = val % MathHelper.TwoPi;
            if (val < 0)
            {
                val = MathHelper.TwoPi - Math.Abs(val);
            }else if(val > MathHelper.TwoPi)
            {
                val = val - MathHelper.TwoPi;
            }
            return val;
        }

        public void OnPositionChange()
        {
            onPositionChange?.Invoke();
            foreach (var child in AllChildren())
            {
                child.OnPositionChange();
            }
        }

        public IEnumerable<Transform> AllChildren()
        {
            if(Children != null && Children.Count > 0)
            {
                foreach (var child in Children)
                {
                    yield return child;
                }
            }
            yield break;
        }
    }
}