using Microsoft.Xna.Framework;
using System;

namespace Nebula.Main
{
	public class Time : IControl
	{
        public static Time Access;

        public static float  deltaTime => Access._deltaTime;
        private float _deltaTime;

        public void Create(Runtime game)
        {
            Access = this;
        }

        public void Draw(GameTime gameTime)
        {
            
        }

        public void Initialise()
        {
            
        }

        public void LoadContent()
        {
            
        }

        public void UnloadContent()
        {
            
        }

        public void Update(GameTime gameTime)
        {
            _deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
    }
}
