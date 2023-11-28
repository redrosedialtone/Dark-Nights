using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using Nebula.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Runtime
{
    public static class DrawUtils
    {

        public delegate void DrawUtil(SpriteBatch batch);
        public delegate Vector2[] TrackedUtil();
        public enum DrawType
        {
            Gizmo = 0,
            World = 1
        }

        private static DrawUtil gizmoDrawCall;
        private static DrawUtil worldDrawCall;
        private static Texture2D lineTex;
        private static Texture2D circleTexture;
        private static Texture2D filledCircleTexture;

        public static void Setup(SpriteBatch batch)
        {
            Texture2D _texture = new Texture2D(batch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _texture.SetData(new[] { Color.White });
            lineTex = _texture;
            circleTexture = Resources.Load<Texture2D>("Sprites/hollowCircle");
            filledCircleTexture = Resources.Load<Texture2D>("Sprites/filledCircle");
        }

        public static void DrawGizmoBuffer(SpriteBatch spriteBatch)
        {
            gizmoDrawCall?.Invoke(spriteBatch);
        }

        public static void DrawWorldBuffer(SpriteBatch spriteBatch)
        {
            worldDrawCall?.Invoke(spriteBatch);
        }

        /// <summary>
        /// Draw a static polygon gizmo.
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="color"></param>
        /// <param name="thickness"></param>
        /// <param name="layerDepth"></param>
        /// <returns></returns>
        public static DrawUtil DrawPolygonOutline(Polygon polygon, Color color, float thickness = 1f, float layerDepth = 0, DrawType drawType = DrawType.Gizmo)
        {
            DrawUtil ret = null;
            for (int i = 0; i < polygon.Points.Length; i++)
            {
                Vector2 v = polygon.Points[i];
                Vector2 vN = polygon.Points[(i + 1) % polygon.Points.Length];
                var d = Vector2.Distance(v, vN);
                var a = (float)Math.Atan2(vN.Y - v.Y, vN.X - v.X);
                var origin = new Vector2(0f, 0.5f);
                var scale = new Vector2(d, thickness);

                DrawUtil del;
                if (drawType == DrawType.Gizmo)
                {
                    del = new DrawUtil(delegate (SpriteBatch batch)
                    {
                        batch.Draw(lineTex, v + polygon.Position, null, color, a, origin, scale, SpriteEffects.None, layerDepth);
                    });
                }
                else
                {
                    del = new DrawUtil(delegate (SpriteBatch batch)
                    {
                        batch.Draw(lineTex, v + polygon.Position, null, color, a, origin, new Vector2(scale.X, scale.Y / Camera.Get.Zoom), SpriteEffects.None, layerDepth);
                    });
                }

                ret += del;
            }
            if(drawType== DrawType.Gizmo) gizmoDrawCall += ret;
            else worldDrawCall+= ret;
            return ret;
        }

        public static DrawUtil DrawCircleOutline(Circle circle, Color color, float thickness = 1f, float layerDepth = 0, DrawType drawType = DrawType.Gizmo)
        {
            DrawUtil ret = null;
            float _scaleVal = circle.Radius / 2.0f;
            Vector2 scale = new Vector2(_scaleVal, _scaleVal);
            ret = new DrawUtil(delegate (SpriteBatch batch)
            {
                batch.Draw(circleTexture, circle.Centre, null, color, 0, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
            });
            if (drawType == DrawType.Gizmo) gizmoDrawCall += ret;
            else worldDrawCall += ret;
            return ret;
        }

        public static DrawUtil DrawCircle(Circle circle, Color color, float thickness = 1f, float layerDepth = 0, DrawType drawType = DrawType.Gizmo)
        {
            DrawUtil ret = null;
            float _scaleVal = circle.Radius / 2.0f;
            Vector2 scale = new Vector2(_scaleVal, _scaleVal);
            ret = new DrawUtil(delegate (SpriteBatch batch)
            {
                batch.Draw(filledCircleTexture, circle.Centre, null, color, 0, Vector2.Zero, scale, SpriteEffects.None, layerDepth);
            });
            if (drawType == DrawType.Gizmo) gizmoDrawCall += ret;
            else worldDrawCall += ret;
            return ret;
        }

        public static DrawUtil DrawLine(Line line, Color color, float thickness = 1f, float layerDepth = 0, DrawType drawType = DrawType.Gizmo)
        {
            DrawUtil ret = null;

            if (drawType == DrawType.Gizmo)
            {
                ret = new DrawUtil(delegate (SpriteBatch batch)
                {
                    var d = Vector2.Distance(line.From, line.To);
                    var a = (float)Math.Atan2(line.To.Y - line.From.Y, line.To.X - line.From.X);
                    var origin = new Vector2(0f, 0.5f);
                    var scale = new Vector2(d, thickness);
                    batch.Draw(lineTex, line.From, null, color, a, origin, scale, SpriteEffects.None, layerDepth);
                });
            }
            else
            {
                ret = new DrawUtil(delegate (SpriteBatch batch)
                {
                    var d = Vector2.Distance(line.From, line.To);
                    var a = (float)Math.Atan2(line.To.Y - line.From.Y, line.To.X - line.From.X);
                    var origin = new Vector2(0f, 0.5f);
                    var scale = new Vector2(d, thickness);
                    batch.Draw(lineTex, line.From, null, color, a, origin, new Vector2(scale.X, scale.Y / Camera.Get.Zoom), SpriteEffects.None, layerDepth);
                });
            }
            if (drawType == DrawType.Gizmo) gizmoDrawCall += ret;
            else worldDrawCall += ret;
            return ret;
        }

        public static void Draw(Polygon polygon, Color color, float thickness = 1f, float layerDepth = 0)
        {

        }


        public static void RemoveUtil(DrawUtil cb)
        {
            gizmoDrawCall -= cb;
        }

        public static void DrawCircle()
        {

        }
    }
}
