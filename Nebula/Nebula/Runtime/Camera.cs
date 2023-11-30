using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nebula.Systems;
using NLog.Fluent;

namespace Nebula.Main
{
    public abstract class Camera : IControl
    {
        #region Static
        private static Camera instance;
        public static Camera Get => instance;

        protected static readonly NLog.Logger log = NLog.LogManager.GetLogger("CAMERA");
        #endregion

        public Vector2 Position { get { return _position; } set { _position = value; RecalculateView(); } }
        public Vector2 Origin { get { return _origin; } set { _origin = value; RecalculateView(); } }
        public float Zoom { get { return _zoom; } set { _zoom = value; RecalculateView(); } }
        public float Rotation { get { return _rotation; } set { _rotation = value; RecalculateView(); } }
        public Vector2 ViewportCenter => new Vector2(ViewportWidth * 0.5f, ViewportHeight * 0.5f);
        public Point Viewport { get; private set; }
        public int ViewportWidth => Viewport.X;
        public int ViewportHeight => Viewport.Y;

        private Vector2 _position;
        private Vector2 _origin;
        private float _zoom;
        private float _rotation;

        private Vector3 cameraTranslationVector = Vector3.Zero;
        private Vector3 cameraScaleVector = Vector3.Zero;
        private Vector3 viewportResolutionVector = Vector3.Zero;
        private Matrix viewTransform = Matrix.Identity;
        private Matrix viewTranslation = Matrix.Identity;
        private Matrix viewOrigin = Matrix.Identity;
        private Matrix viewRotation = Matrix.Identity;
        private Matrix viewScale = Matrix.Identity;

        private Rectangle _viewBounds;

        private bool _isViewTransformDirty = true;
        private bool _isViewBoundsDirty = true;

        public Matrix ViewTransformationMatrix
        {
            get
            {
                if (_isViewTransformDirty)
                {
                    cameraTranslationVector = new Vector3(-Position.X, -Position.Y, 0.0f);
                    Matrix.CreateTranslation(ref cameraTranslationVector, out viewTranslation);
                    Matrix.CreateRotationZ(Rotation, out viewRotation);

                    cameraScaleVector = new Vector3(Zoom, Zoom, 1.0F);
                    Matrix.CreateScale(ref cameraScaleVector, out viewScale);

                    viewportResolutionVector = new Vector3(Origin.X, Origin.Y, 0);
                    Matrix.CreateTranslation(ref viewportResolutionVector, out viewOrigin);

                    viewTransform = viewTranslation *
                        viewRotation *
                        viewScale *
                        viewOrigin;
                }
                return viewTransform;
            }
        }

        public Vector3 CameraScaleVector
        {
            get
            {
                cameraScaleVector = new Vector3(Zoom, Zoom, 1.0F);
                return cameraScaleVector;
            }
        }

        public Rectangle Bounds
        {
            get
            {
                if (_isViewBoundsDirty)
                {
                    Vector2 Origin = Vector2.Zero;
                    _viewBounds = new Rectangle((int)Origin.X, (int)Origin.Y, ViewportWidth, ViewportHeight);
                }
                return _viewBounds;
            }
        }

        private void RecalculateView()
        {
            _isViewBoundsDirty = true;
            _isViewTransformDirty = true;
        }

        public void Create(NebulaRuntime game)
        {
            instance = this;
        }

        public virtual void Update(GameTime gameTime) { }

        public virtual void Initialise()
        {
            log.Info("> ..");
            Zoom = 1.0f;
            Rotation = 0.0f;
            Viewport = new Point(Graphics.RENDER_WIDTH, Graphics.RENDER_HEIGHT);
            Origin = new Vector2(ViewportWidth / 2.0f, ViewportHeight / 2.0f);
            Position = Vector2.Zero;
        }

        public Rectangle ViewportWorldBoundry()
        {
            Vector2 viewPortCorner = ScreenToWorld(new Vector2(0, 0));
            Vector2 viewPortBottomCorner =
               ScreenToWorld(new Vector2(ViewportWidth, ViewportHeight));

            return new Rectangle((int)viewPortCorner.X,
               (int)viewPortCorner.Y,
               (int)(viewPortBottomCorner.X - viewPortCorner.X),
               (int)(viewPortBottomCorner.Y - viewPortCorner.Y));
        }

        public static Vector2 WorldToScreen(Vector2 worldPosition)
        {
            return Vector2.Transform(worldPosition, instance.ViewTransformationMatrix);
        }

        public static Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return Vector2.Transform(screenPosition,
                Matrix.Invert(instance.ViewTransformationMatrix));
        }


        public void OnClick(MouseButtonActionState Data) { }

        public void LoadContent()
        {
            
        }

        public void UnloadContent()
        {
            
        }

        public void Draw(GameTime gameTime)
        {
            
        }
    }

    public class NebulaCamera : Camera, IDefaultCtxt
    {
        private bool _runningCameraMovement = false;
        private bool _cameraMovementActuated = false;
        private Vector2 cameraTarget;
        private Vector2 cameraVelocity;
        private bool CameraLock = false;

        public override void Initialise()
        {
            base.Initialise();
            Input.DefaultCtxt.OnMovement += OnMovementAxis;
            Input.DefaultCtxt.OnScroll += OnZoom;
            Input.DefaultCtxt.OnLock += OnLock;

            CameraLock = Input.DefaultCtxt.Lock;
        }

        public override void Update(GameTime time)
        {
            if (_runningCameraMovement)
            {
                ContinueCameraMovement();
            }
        }

        public void OnMovementAxis(Vector2 cameraMovement)
        {
            if (CameraLock) return; ;
            if (cameraMovement != Vector2.Zero) _cameraMovementActuated = true;
            else { _cameraMovementActuated = false; }
            log.Trace($"Camera Movement Input::{cameraMovement}");
            if (!_runningCameraMovement)
            {
                cameraTarget = Position;
                cameraVelocity = Vector2.Zero;
                _runningCameraMovement = true;
                log.Trace($"Starting Camera Movement From {Position} to {cameraTarget + cameraMovement}");
            }
            cameraTarget = Position + cameraMovement;
        }

        public void OnRotate(float rotation)
        {

        }

        private void ContinueCameraMovement()
        {
            Vector2 cameraPos = Position;
            float _vX = cameraVelocity.X;
            float _x = SmoothDamp(cameraPos.X, cameraTarget.X, ref _vX, Time.DeltaTime * 2.0f, Time.DeltaTime);
            float _vY = cameraVelocity.Y;
            float _y = SmoothDamp(cameraPos.Y, cameraTarget.Y, ref _vY, Time.DeltaTime * 2.0f, Time.DeltaTime);
            cameraVelocity = new Vector2(_vX, _vY);

            Vector2 movement = new Vector2(_x, _y);
            log.Trace($"Camera Moving From [{cameraPos}] to [{movement}] [{cameraTarget}]");
            Position = movement;

            if (!_cameraMovementActuated && Position == cameraTarget)
            {
                log.Trace("CameraMovementEnd");
                _runningCameraMovement = false;
            }
        }

        private float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float deltaTime, float maxSpeed = 999)
        {
            smoothTime = Math.Max(0.0001F, smoothTime);
            float omega = 2F / smoothTime;
            float x = omega * deltaTime;
            float exp = 1F / (1F + x + 0.48F * x * x + 0.235F * x * x * x);
            float change = current - target;
            float originalTo = target;
            // Clamp maximum speed
            float maxChange = maxSpeed * smoothTime;
            change = Math.Clamp(change, -maxChange, maxChange);
            target = current - change;
            float temp = (currentVelocity + omega * change) * deltaTime;
            currentVelocity = (currentVelocity - omega * temp) * exp;
            float output = target + (change + temp) * exp;
            // Prevent overshooting
            if (originalTo - current > 0.0F == output > originalTo)
            {
                output = originalTo;
                currentVelocity = (output - originalTo) / deltaTime;
            }
            return output;
        }


        public void AdjustZoom(float amount)
        {
            Zoom += amount;
            if (Zoom < 0.25f)
            {
                Zoom = 0.25f;
            }
            if (Zoom > 5.00f)
            {
                Zoom = 5.00f;
            }
        }

        public void OnZoom(float zoomDelta)
        {
            AdjustZoom(zoomDelta);
        }

        public void OnLock(bool locked)
        {
            log.Debug($"Camera Lock Set To {locked}");
            CameraLock = locked;
        }
    }
}
