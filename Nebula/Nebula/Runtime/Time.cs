using Microsoft.Xna.Framework;
using System;

namespace Nebula.Main
{
	public class Time : IControl
	{
        public static Time Access;

        public static float DeltaTime => Access.deltaTime;
        public float deltaTime;

        public void Create(NebulaRuntime game)
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
            deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
    }
}
