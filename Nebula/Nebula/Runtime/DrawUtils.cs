using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Runtime
{
    public static class DrawUtils
    {

        public delegate void batchDraw(SpriteBatch batch);
        public delegate Vector2[] trackedPoints();
        private static batchDraw OnDraw;

        private static Texture2D lineTex;

        public static void LineTexture(SpriteBatch batch)
        {
            Texture2D _texture = new Texture2D(batch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _texture.SetData(new[] { Color.White });
            lineTex = _texture;
        }

        public static void DrawBuffer(SpriteBatch spriteBatch)
        {
            OnDraw?.Invoke(spriteBatch);
        }

        public static batchDraw DrawPolygon(Vector2[] points, Color color, float thickness = 1f, float layerDepth = 0)
        {
            batchDraw ret = null;

            for (int i = 0; i < points.Length; i++)
            {
                Vector2 v = points[i];
                Vector2 vN = points[(i + 1) % points.Length];
                var d = Vector2.Distance(v, vN);
                var a = (float)Math.Atan2(vN.Y - v.Y, vN.X - v.X);
                var origin = new Vector2(0f, 0.5f);
                var scale = new Vector2(d, thickness);

                batchDraw del = new batchDraw(delegate (SpriteBatch batch)
                {
                    batch.Draw(lineTex, v, null, color, a, origin, scale, SpriteEffects.None, layerDepth);
                });

                ret += del;
            }

            //OnDraw += ret;

            return ret;
        }

        public static batchDraw DrawTrackedPolygon(trackedPoints cb, Color color, float thickness = 1f, float layerDepth = 0)
        {
            batchDraw del = new batchDraw(delegate (SpriteBatch batch)
            {
                Vector2[] v = cb.Invoke();
                var ret = DrawPolygon(v, color, thickness, layerDepth);
                ret.Invoke(batch);
            });
                
            OnDraw += del;
            return del;
        }

        public static void UnDraw(batchDraw cb)
        {
            OnDraw -= cb;
        }

        public static void DrawCircle()
        {

        }
    }
}
