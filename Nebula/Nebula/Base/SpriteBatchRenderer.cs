using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Main;
using Nebula.Runtime;
using System;
using System.Reflection.Emit;

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

        public Effect testEffect;
        public Camera Camera => Camera.Get;
        public SpriteBatch Batch => Graphics.SpriteBatch;

        private SpriteBatchRenderer() { }

        public void Create(NebulaRuntime runtime) { }

        public void Initialise() { }

        public void LoadContent()
        {
            testEffect = Resources.Load<Effect>("Test");
        }

        public void UnloadContent()
        {
            
        }

        public void Update(GameTime gameTime)
        {
            
        }

        public void Draw(GameTime gameTime)
        {
            
        }

        public void StartDraw()
        {
            var matrix = Camera.ViewTransformationMatrix;
            Batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, matrix);
        }

        public void FinishDraw()
        {
            Batch.End();
        }

        public void DrawSprite(Sprite2D sprite, Vector2 position)
        {
            Batch.Draw(sprite.Texture, position + sprite.Pivot, sprite.SourceRect, Color.White, 0, sprite.Pivot, 1.0f, SpriteEffects.None, 0);
        }

        public void DrawSprite(Sprite2D sprite, Vector2 position, float rotation)
        {
            Batch.Draw(sprite.Texture, position + sprite.Pivot, sprite.SourceRect, Color.White, rotation, sprite.Pivot, 1.0f, SpriteEffects.None, 0);
        }
    }
}
