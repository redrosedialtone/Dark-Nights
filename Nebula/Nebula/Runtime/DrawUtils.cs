using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Nebula.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static Nebula.Runtime.DrawUtils;

namespace Nebula.Runtime
{
    public static class DrawUtils
    {
        private static SpriteBatch spriteBatch => Graphics.SpriteBatch;
        private static SpriteBatch uiBatch => Graphics.UIBatch;

        private static SpriteFont defaultFont;
        private static Texture2D lineTex;
        private static Texture2D circleTexture;
        private static Texture2D filledCircleTexture;


        public static void Setup(SpriteBatch batch)
        {
            Texture2D _lineTexture = new Texture2D(batch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _lineTexture.SetData(new[] { Color.White });
            lineTex = _lineTexture;
            circleTexture = Resources.Load<Texture2D>("Sprites/hollowCircle");
            filledCircleTexture = Resources.Load<Texture2D>("Sprites/filledCircle");
            defaultFont = Resources.Load<SpriteFont>("FONT/Constantina");


        }

        public static void DrawCircleToWorld(Circle circle, Color color, float thickness = 1f, float layerDepth = 0)
        {
            DrawCircleToWorld(circle.Centre, circle.Radius, color, thickness, layerDepth);
        }

        public static void DrawCircleToWorld(Vector2 Centre, float Radius, Color color, float thickness = 1f, float layerDepth = 0)
        {
            float _scaleVal = Radius / filledCircleTexture.Width;
            Vector2 scale = new Vector2(_scaleVal, _scaleVal);
            Vector2 pos = new Vector2(Centre.X - _scaleVal * filledCircleTexture.Width / 2, Centre.Y - _scaleVal * filledCircleTexture.Width / 2);
            spriteBatch.Draw(filledCircleTexture, pos, null, color, 0, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
        }

        public static void DrawCircleOutlineToWorld(Circle circle, Color color, float thickness = 1f, float layerDepth = 0)
        {
            float _scaleVal = circle.Radius / circleTexture.Width;
            Vector2 scale = new Vector2(_scaleVal, _scaleVal);
            Vector2 pos = new Vector2(circle.Centre.X - _scaleVal * filledCircleTexture.Width / 2, circle.Centre.Y - _scaleVal * filledCircleTexture.Width / 2);
            spriteBatch.Draw(circleTexture, pos, null, color, 0, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
        }

        public static void DrawRectangleToWorld(Vector2 Position, int width, int height, Color color, float thickness = 1f, float layerDepth = 0)
        {
            spriteBatch.Draw(lineTex, Position, null, color, 0f, Vector2.Zero, new Vector2(width,height), SpriteEffects.None, layerDepth);
        }

        public static void DrawPolygonOutlineToWorld(Polygon polygon, Color color, float thickness = 1f, float layerdepth = 0)
        {
            DrawPolygonOutlineToWorld(polygon.Position, polygon.Points, color, thickness, layerdepth);
        }

        public static void DrawPolygonOutlineToWorld(Vector2 Position, Vector2[] Points, Color color, float thickness = 1f, float layerDepth = 0)
        {
            for (int i = 0; i < Points.Length; i++)
            {
                Vector2 v = Points[i];
                Vector2 vN = Points[(i + 1) % Points.Length];
                var d = Vector2.Distance(v, vN);
                var a = (float)Math.Atan2(vN.Y - v.Y, vN.X - v.X);
                var origin = new Vector2(0f, 0.5f);
                var scale = new Vector2(d, thickness);

                spriteBatch.Draw(lineTex, v + Position, null, color, a, origin, new Vector2(scale.X, scale.Y / Camera.Get.Zoom), SpriteEffects.None, layerDepth);
            }
        }

        public static void DrawPolygonOutlineToWorld(Rectangle rect, Color color, float thickness = 1, float layerDepth = 0)
        {
            spriteBatch.Draw(lineTex, new Vector2(rect.X, rect.Y), null, color, 0f, Vector2.Zero, new Vector2(rect.Width, thickness / Camera.Get.Zoom), SpriteEffects.None, layerDepth);
            spriteBatch.Draw(lineTex, new Vector2(rect.Right - thickness / Camera.Get.Zoom, rect.Y), null, color, 0f, Vector2.Zero, new Vector2(thickness / Camera.Get.Zoom, rect.Height), SpriteEffects.None, layerDepth);
            spriteBatch.Draw(lineTex, new Vector2(rect.X, rect.Y), null, color, 0f, Vector2.Zero, new Vector2(thickness / Camera.Get.Zoom, rect.Height), SpriteEffects.None, layerDepth);
            spriteBatch.Draw(lineTex, new Vector2(rect.X, rect.Bottom - thickness / Camera.Get.Zoom), null, color, 0f, Vector2.Zero, new Vector2(rect.Width, thickness / Camera.Get.Zoom), SpriteEffects.None, layerDepth);
        }

        public static void DrawPolygonOutlineToScreen(Polygon polygon, Color color, float thickness = 1f, float layerdepth = 0)
        {
            DrawPolygonOutlineToScreen(polygon.Position, polygon.Points, color, thickness, layerdepth);
        }

        public static void DrawPolygonOutlineToScreen(Vector2 Position, Vector2[] Points, Color color, float thickness = 1f, float layerDepth = 0)
        {
            for (int i = 0; i < Points.Length; i++)
            {
                Vector2 v = Points[i];
                Vector2 vN = Points[(i + 1) % Points.Length];
                var d = Vector2.Distance(v, vN);
                var a = (float)Math.Atan2(vN.Y - v.Y, vN.X - v.X);
                var origin = new Vector2(0f, 0.5f);
                var scale = new Vector2(d, thickness);

                uiBatch.Draw(lineTex, v + Position, null, color, a, origin, new Vector2(scale.X, scale.Y), SpriteEffects.None, layerDepth);
            }
        }

        public static void DrawLineToWorld(Line line, Color color, float thickness = 1f, float layerDepth = 0)
        {
            DrawLineToWorld(line.From, line.To, color, thickness, layerDepth);
        }

        public static void DrawLineToWorld(Vector2 A, Vector2 B, Color color, float thickness = 1f, float layerDepth = 0)
        {
            var d = Vector2.Distance(A,B);
            var a = (float)Math.Atan2(B.Y - A.Y, B.X - A.X);
            var origin = new Vector2(0f, 0.5f);
            var scale = new Vector2(d, thickness);
            spriteBatch.Draw(lineTex, A, null, color, a, origin, new Vector2(scale.X, scale.Y / Camera.Get.Zoom), SpriteEffects.None, layerDepth);
        }

        public static void DrawText(SpriteFont spriteFont, string text, Vector2 position, Color color, float layer)
        {
            DrawText(uiBatch, spriteFont, text, position, color, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 1.0f);
        }

        public static void DrawText(string text, Vector2 position, Color color, float scale = 1f)
        {
            DrawText(uiBatch, defaultFont, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 1.0f);
        }

        private static void DrawText(SpriteBatch batch, SpriteFont spriteFont, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layer)
        {
            batch.DrawString(spriteFont, text, position, color, rotation, origin, scale, effects, layer);
        }
        public static Texture2D CreateSquareTexture(GraphicsDevice device, Color[] colours)
        {
            //initialize a texture
            Texture2D texture = new Texture2D(device, colours.Length, colours.Length);
            //set the color
            texture.SetData(colours);
            return texture;
        }
    }
}
