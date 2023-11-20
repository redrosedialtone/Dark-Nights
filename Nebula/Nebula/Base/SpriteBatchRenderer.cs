using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Main;
using System;

namespace Nebula
{
	public class SpriteBatchRenderer : ISpriteBatchDraw
	{
		public Transform Transform => _transform;
		public TextureData TextureData { get; private set; }
		public Color Color { get; private set; }
		public Vector2 TextureScale { get; private set; }
		public Vector2 Size => new Vector2(TextureData.Texture.Width * TextureScale.X, TextureData.Texture.Height * TextureScale.Y);
		public bool Drawing { get; set; }
        public SpriteEffects Effects { get; private set; }
		public float LayerDepth { get; set; }
		public float RotationalOffset { get; set; }

        private Transform _transform;

		public SpriteBatchRenderer(Transform transform)
		{
			Graphics.AddBatchDraw(this);
			_transform = transform;
			TextureScale = Vector2.One;
			Color = Color.White;
			Drawing = true;
			Effects = SpriteEffects.None;
		}

		public void SetTransform(Transform transform)
		{
			this._transform = transform;
		}

        public void Draw(SpriteBatch Batch)
        {
			if (Drawing)
			{
				if (Transform == null) return;
				Batch.Draw(TextureData.Texture, Transform.Position, null, Color, Transform.Rotation - RotationalOffset, TextureData.Origin, TextureScale,Effects, LayerDepth + TextureData.LayerDepth);
            }			
        }

		public void SetTextureData(TextureData Data)
		{
			this.TextureData = Data;
		}

		public void SetColor(Color Color)
		{
			this.Color = Color;
		}

		public void SetScale(Vector2 Scale)
		{
			this.TextureScale = Scale;
		}

		public void FlipHorziontal()
		{
			Effects |= SpriteEffects.FlipHorizontally;
		}

		public bool Flipped => (Effects & SpriteEffects.FlipHorizontally) != 0;
    }
}
