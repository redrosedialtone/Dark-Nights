using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Main;
using Nebula.Runtime;
using System;

namespace Nebula
{
    public class Sprite2D
    {
        public Texture2D Texture { get; private set; }
        public Vector2 Pivot { get; private set; }
        public Rectangle SourceRect { get; private set; }

        public Sprite2D(Texture2D texture, Rectangle sourceRect)
        {
            Texture = texture;
            SourceRect = sourceRect;
            Pivot = Vector2.Zero;
        }

        public Sprite2D(Texture2D texture, Rectangle sourceRect, Vector2 pivot)
        {
            Texture = texture;
            SourceRect = sourceRect;
            Pivot = pivot;
        }

        public Vector2 WidthHeightToVector2()
        {
            Vector2 widthheight = Vector2.Zero;

            widthheight.X = SourceRect.Width;
            widthheight.Y = SourceRect.Height;

            return widthheight;
        }
    }

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
