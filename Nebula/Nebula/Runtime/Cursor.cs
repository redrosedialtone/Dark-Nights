using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nebula.Main;
using System;

namespace Nebula
{
    public class Cursor : IControl
    {
        private static Cursor instance;
        public static Cursor Get => instance;

        public static Vector2 Position => instance._mousePosition;
        public MousePositionGizmo mousePosGizmo;
        private Vector2 _mousePosition;
        private Texture2D CursorImage;
        private Vector2 CursorOrigin;
        private bool customCursor = true;


        public void Create(Runtime game)
        {
            instance = this;
        }

        public void Draw(GameTime gameTime)
        {
            
        }

        public void Initialise()
        {
            mousePosGizmo = new MousePositionGizmo();
            mousePosGizmo.SetDrawGizmo(true);
        }

        public void LoadContent()
        {
            CursorImage = Resources.Load<Texture2D>("Sprites/Cursor");
            CursorOrigin = new Vector2(CursorImage.Width / 2.0f, CursorImage.Height / 2.0f);
        }

        public void UnloadContent()
        {
            
        }

        public void Update(GameTime gameTime)
        {

        }

        public void DrawCursor(SpriteBatch batch)
        {
            MouseState state = Mouse.GetState();
            _mousePosition = new Vector2(state.X, state.Y);
            if (customCursor)
            {
                batch.Draw(CursorImage, _mousePosition, null, Color.White, 0.0f, CursorOrigin, 1.0f, SpriteEffects.None, 1.0f);
            }
        }
    }
}
