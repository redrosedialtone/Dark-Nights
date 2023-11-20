using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Main;
using System;

namespace Nebula
{
    public class Sprite
    {
        public Texture2D SpriteTexture { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Origin { get; set; }

        public Sprite(Texture2D spriteTexture, Vector2 position)
        {
            this.SpriteTexture = spriteTexture;
            this.Position = position;
            Origin = new(SpriteTexture.Width/2,SpriteTexture.Height/2);
        }

        public void Draw()
        {
            Graphics.SpriteBatch.Draw(SpriteTexture, Position, null, Color.White,0f,Origin,1f,SpriteEffects.None,0f);
        }
    }
}

