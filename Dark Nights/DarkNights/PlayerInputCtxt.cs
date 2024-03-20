using Microsoft.Xna.Framework;
using Nebula.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{
    public class CharacterControlCtxt : InputContext
    {
        public override string Name => "characterControlCtxt";

        public float SENSITIVITY = 500;
        public float SENSITIVITY_ROTATE = 0.02f;

        public Action<Vector2> OnMovement;
        public Action<float> OnRotation;
        public Action<float> OnScroll;
        public Action<bool> OnLock;
        public Action<MouseButtonActionState> OnClick;
        public Action<MouseButtonActionState> OnPress;
        public Action<MouseButtonActionState> OnRelease;
        private bool _movementUpdate = false;
        private bool _mouseDown = false;

        public bool Lock = true;

        public override void ProcessActions(GameTime time)
        {
            bool _active = false;
            bool _oldLock = Lock;
            float movementX = 0.0F;
            float movementY = 0.0F;
            float rotateVal = 0.0F;
            if (Input.Active("InputID.Right"))
            {
                movementX += 1.0f;
                _active = true;
            }

            if (Input.Active("InputID.Left"))
            {
                movementX -= 1.0f;
                _active = true;
            }

            if (Input.Active("InputID.Up"))
            {
                movementY -= 1.0f;
                _active = true;
            }

            if (Input.Active("InputID.Down"))
            {
                movementY += 1.0f;
                _active = true;
            }

            if (Input.Active("InputID.LeftRotate"))
            {
                rotateVal -= 1.0f;
                _active = true;
            }

            if (Input.Active("InputID.RightRotate"))
            {
                rotateVal += 1.0f;
                _active = true;
            }

            var _lockData = Input.Data("InputID.Lock");
            if (_lockData != null && _lockData is InputActionData _data)
            {
                if (_data.PressedThisFrame) Lock = !Lock;
            }

            if (_active) _movementUpdate = true;
            if (_movementUpdate)
            {
                bool _shift = Input.Active("InputID.Shift");
                Vector2 cameraMovement = new Vector2(movementX, movementY);
                if (cameraMovement != Vector2.Zero) cameraMovement.Normalize();
                cameraMovement = cameraMovement * SENSITIVITY * (_shift ? 2.5f : 1);

                OnMovement?.Invoke(cameraMovement);

                if (rotateVal != 0)
                {
                    rotateVal = rotateVal * SENSITIVITY_ROTATE * (_shift ? 2.5f : 1);
                    OnRotation?.Invoke(rotateVal);
                }


            }
            if (!_active) _movementUpdate = false;
            if (_oldLock != Lock)
            {
                OnLock?.Invoke(Lock);
            }

            float scrollY = 0.0f;
            if (Input.Active("InputID.Scroll"))
            {
                InputRangeData data = (InputRangeData)Input.Data("InputID.Scroll");
                scrollY += data.Current.State;
            }

            if (scrollY != 0)
            {
                scrollY = scrollY * Time.DeltaTime * 0.02f;
                OnScroll?.Invoke(scrollY);
            }

            MouseButtonActionState _leftMouseData = Input.Active("InputID.LeftMouseButton") ? (MouseButtonActionState)Input.Data("InputID.LeftMouseButton") : null;
            MouseButtonActionState _rightMouseData = Input.Active("InputID.RightMouseButton") ? (MouseButtonActionState)Input.Data("InputID.RightMouseButton") : null;

            if (_leftMouseData != null)
            {
                if (_leftMouseData.PressedThisFrame())
                {
                    OnPress?.Invoke(_leftMouseData);
                    if(_leftMouseData.availableForClick) OnClick?.Invoke(_leftMouseData);
                }
                else if (_leftMouseData.ReleasedThisFrame()) OnRelease?.Invoke(_leftMouseData);
            }

            if (_rightMouseData != null)
            {
                if (_rightMouseData.PressedThisFrame())
                {
                    OnPress?.Invoke(_rightMouseData);
                    if (_rightMouseData.availableForClick) OnClick?.Invoke(_rightMouseData);
                }
                else if (_rightMouseData.ReleasedThisFrame()) OnRelease?.Invoke(_rightMouseData);
            }
        }
    }

    public interface IPlayerInputCtxt
    {
        public void OnMovementAxis(Vector2 movementAxis);
        public void OnRotate(float rotation);
        public void OnZoom(float zoomDelta);
        public void OnLock(bool locked);
        public void OnClick(MouseButtonActionState Data);
    }
}
