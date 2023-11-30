using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Main;
using System;

namespace Nebula
{
	public class SpriteBatchRenderer : IControl
	{
        #region Singleton
        private static readonly NLog.Logger log = NLog.LogManager.GetLogger("GRAPHICS");
        public static SpriteBatchRenderer Get
        {
            get
            {
                if (instance == null)
                {
                    instance = new SpriteBatchRenderer();
                }

                return instance;
            }
        }
        private static SpriteBatchRenderer instance = null;
        #endregion

        public SpriteBatch SpriteBatch { get; private set; }

        private SpriteBatchRenderer() { }

        public void Create(NebulaRuntime runtime)
        {
            SpriteBatch = new SpriteBatch(NebulaRuntime.GraphicsDevice);
        }

        public void Initialise()
        {
            throw new NotImplementedException();
        }

        public void LoadContent()
        {
            throw new NotImplementedException();
        }

        public void UnloadContent()
        {
            throw new NotImplementedException();
        }

        public void Update(GameTime gameTime)
        {
            throw new NotImplementedException();
        }

        public void Draw(GameTime gameTime)
        {
            throw new NotImplementedException();
        }
    }
}
