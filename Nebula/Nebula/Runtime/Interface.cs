using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nebula.Main;

namespace Nebula.Main
{
    public class UserInterface : IControl
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetLogger("INTERFACE");
        public static UserInterface Get
        {
            get
            {
                if (instance == null)
                {
                    instance = new UserInterface();
                }

                return instance;
            }
        }
        private static UserInterface instance = null;
        public SpriteBatch Batch => Graphics.UIBatch;

        public void Create(NebulaRuntime game)
        {

        }

        public void StartDraw()
        {
            Batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, null);
        }

        public void EndDraw()
        {
            Batch.End();
        }


        public void Draw(GameTime Time)
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
            return;
        }

        public void Update(GameTime gameTime)
        {
            return;
        }

        public void DrawUI(Texture2D texture, Rectangle destRect, Rectangle? sourceRect, Color color, float rotation, Vector2 origin, bool absOrigin, bool flippedX, bool flippedY)
        {
            SpriteEffects se = SpriteEffects.None;
            if (flippedX == true) se |= SpriteEffects.FlipHorizontally;
            if (flippedY == true) se |= SpriteEffects.FlipVertically;
            Vector2 realOrigin = (sourceRect.HasValue && absOrigin == false) ? UtilityGlobals.GetOrigin(texture, origin.X, origin.Y) : origin;
            Batch.Draw(texture, destRect, sourceRect, color, rotation, realOrigin, se, 0);
        }

        public void DrawUI(Texture2D texture, Vector2 position, Rectangle? sourceRect, Color color, float rotation, Vector2 origin, Vector2 scale, bool absOrigin, bool flippedX, bool flippedY)
        {
            SpriteEffects se = SpriteEffects.None;
            if (flippedX == true) se |= SpriteEffects.FlipHorizontally;
            if (flippedY == true) se |= SpriteEffects.FlipVertically;
            Vector2 realOrigin = (sourceRect.HasValue && absOrigin == false) ? UtilityGlobals.GetOrigin(texture, origin.X, origin.Y) : origin;
            Batch.Draw(texture, position, sourceRect, color, rotation, realOrigin, scale, se, 0);
        }

        public void DrawSlicedSprite(ExpandableTexture texture, Rectangle rect, Color color, bool flippedX = false, bool flippedY = false)
        {
            float rotation = 0f;
            Vector2 absOrigin = Vector2.Zero;
            for (int i = 0; i < texture.Regions.Length; i++)
            {
                Rectangle destRect = texture.GetRectForIndex(rect, i);

                //Sliced textures must use absolute origins since they can have vastly different sizes for each region
                DrawUI(texture.Texture, destRect, texture.Regions[i], color, rotation, absOrigin, true, flippedX, flippedY);
            }
        }

        
    }
}
