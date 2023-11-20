using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nebula.Base;

namespace Nebula.Main
{
    public interface ISpriteBatchDraw
    {
        void Draw(SpriteBatch Batch);
    }

    public class Graphics : IControl
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetLogger("GRAPHICS");
        public static Graphics Access;

        public static SpriteBatch SpriteBatch => Access._spriteBatch;

        public GraphicsDeviceManager GraphicsDeviceMngr => Runtime.GraphicsDeviceMgr;
        public GraphicsDevice GraphicsDevice => RUNTIME.GraphicsDevice;
        public static int SCREEN_WIDTH = (int)(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width);
        public static int SCREEN_HEIGHT = (int)(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height);
        public static int SCREEN_ASPECT => SCREEN_WIDTH / SCREEN_HEIGHT;
        public static int RENDER_WIDTH => 1366;
        public static int RENDER_HEIGHT => 768;
        public static int RENDER_ASPECT => RENDER_WIDTH / RENDER_HEIGHT;

        private List<IDrawGizmos> gizmos = new List<IDrawGizmos>();
        private List<IDrawGizmos> worldGizmos = new List<IDrawGizmos>();
        private List<IDrawUIBatch> UIDrawCalls = new List<IDrawUIBatch>();
        private List<ISpriteBatchDraw> spriteBatchCalls = new List<ISpriteBatchDraw>();
        private Stack<SpriteBatchRenderer> textureBuffer = new Stack<SpriteBatchRenderer>();

        private Runtime RUNTIME;
        private SpriteBatch _spriteBatch;
        private RenderTarget2D renderTarget;
        private Texture2D circleTexture;
        private Texture2D filledCircleTexture;
        //Texture2D ballTexture;


        public void Create(Runtime game)
        {
            RUNTIME = game;
            Access = this;
        }

        public void Initialise()
        {
            log.Info("> Nebula Graphics Init.. <");
            //Graphics.IsFullScreen = true;
            GraphicsDeviceMngr.PreferredBackBufferWidth = RENDER_WIDTH;
            GraphicsDeviceMngr.PreferredBackBufferHeight = RENDER_HEIGHT;
            GraphicsDeviceMngr.ApplyChanges();
            renderTarget = new RenderTarget2D(GraphicsDevice, RENDER_WIDTH, RENDER_HEIGHT, false, SurfaceFormat.Color, DepthFormat.None);
        }

        public void LoadContent()
        {
            _spriteBatch = new SpriteBatch(RUNTIME.GraphicsDevice);
            circleTexture = Resources.Load<Texture2D>("Sprites/hollowCircle");
            filledCircleTexture = Resources.Load<Texture2D>("Sprites/filledCircle");
            //ballTexture = RUNTIME.Content.Load<Texture2D>("DesignButtonLogo");
        }

        public void Update(GameTime gameTime)
        {

        }

        public void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(renderTarget);
            GraphicsDevice.Clear(ClearOptions.Target, new Color(23,23,23), 1.0f, 0);

            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
            Cursor.Get.DrawCursor(_spriteBatch);
            DrawGizmos();
            DrawUICalls(_spriteBatch);
            _spriteBatch.End();

            var matrix = Camera.Get.ViewTransformationMatrix;
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, matrix);
            log.Trace($"Drawing {spriteBatchCalls.Count} sprites..");
            DrawSpriteBatch(_spriteBatch);
            DrawWorldGizmos();
            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);

            _spriteBatch.Begin();
            _spriteBatch.Draw(renderTarget, new Rectangle(0,0,RENDER_WIDTH,RENDER_HEIGHT), Color.White);
            _spriteBatch.End();
        }

        public static void AddGizmo(IDrawGizmos Gizmo)
        {
            Access.gizmos.Add(Gizmo);
        }

        public static void AddWorldGizmo(IDrawGizmos Gizmo)
        {
            Access.worldGizmos.Add(Gizmo);
        }

        private void DrawGizmos()
        {
            foreach (var gizmo in gizmos)
            {
                gizmo.DrawGizmos(_spriteBatch);
            }
        }

        private void DrawWorldGizmos()
        {
            foreach (var gizmo in worldGizmos)
            {
                gizmo.DrawGizmos(_spriteBatch);
            }
        }

        public static void AddUIDraw(IDrawUIBatch Batch)
        {
            Access.UIDrawCalls.Add(Batch);
        }

        private void DrawUICalls(SpriteBatch Batch)
        {
            foreach (var batch in UIDrawCalls)
            {
                batch.DrawUI(Batch);
            }
        }

        public static void AddBatchDraw(ISpriteBatchDraw draw)
        {
            Access.spriteBatchCalls.Add(draw);
        }

        private void DrawSpriteBatch(SpriteBatch Batch)
        {
            foreach (var child in spriteBatchCalls)
            {
                child.Draw(Batch);
            }
        }

        public static void DrawLine(SpriteBatch spriteBatch, Vector2 point1, Vector2 point2, Color color, float thickness = 1f, float layerDepth = 0)
        {
            var distance = Vector2.Distance(point1, point2);
            var angle = (float)Math.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            DrawLine(spriteBatch, point1, distance, angle, color, thickness, layerDepth);
        }

        public static void DrawLine(SpriteBatch spriteBatch, Vector2 point, float length, float angle, Color color, float thickness = 1f, float layerDepth = 0)
        {
            Texture2D _texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            _texture.SetData(new[] { Color.White });
            var origin = new Vector2(0f, 0.5f);
            var scale = new Vector2(length, thickness);
            spriteBatch.Draw(_texture, point, null, color, angle, origin, scale, SpriteEffects.None, layerDepth);
        }

        public static void DrawLines(SpriteBatch spriteBatch, Vector2[] points, Color color, float thickness = 1f, float layerDepth = 0)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 vert = points[i];
                Vector2 nextVert = points[(i + 1) % points.Length];
                DrawLine(spriteBatch, vert, nextVert, color, thickness, layerDepth);
            }
        }

        public static void DrawCircle(SpriteBatch spriteBatch, Vector2 centre, Vector2 origin, int radius, Color color, float angle, float layerDepth = 0)
        {
            Texture2D texture = Access.circleTexture;
            float _scaleVal = radius / 32.0f;
            Vector2 scale = new Vector2(_scaleVal, _scaleVal);
            Vector2 scaledOrigin = new Vector2(origin.X + 32.0f, origin.Y + 32.0f);
            spriteBatch.Draw(texture, centre, null, color, angle, scaledOrigin, scale, SpriteEffects.None, layerDepth);
        }

        public static void DrawFilledCircle(SpriteBatch spriteBatch, Vector2 centre, Vector2 origin, int radius, Color color, float angle, float layerDepth = 0)
        {
            Texture2D texture = Access.filledCircleTexture;
            float _scaleVal = radius / 32.0f;
            Vector2 scale = new Vector2(_scaleVal, _scaleVal);
            Vector2 scaledOrigin = new Vector2(origin.X + 32.0f, origin.Y + 32.0f);
            spriteBatch.Draw(texture, centre, null, color, angle, scaledOrigin, scale, SpriteEffects.None, layerDepth);
        }

        public static void DrawRectangle(SpriteBatch spriteBatch, Rectangle Rect, Vector2 origin, Color color, float thickness = 1f, float layerdepth = 0)
        {
            Vector2 A = new Vector2(Rect.Left, Rect.Top);
            Vector2 B = new Vector2(Rect.Right, Rect.Top);
            Vector2 C = new Vector2(Rect.Right, Rect.Bottom);
            Vector2 D = new Vector2(Rect.Left, Rect.Bottom);

            DrawLine(spriteBatch,A,B,color,thickness,layerdepth);
            DrawLine(spriteBatch, B, C, color, thickness, layerdepth);
            DrawLine(spriteBatch, C, D, color, thickness, layerdepth);
            DrawLine(spriteBatch, D, A, color, thickness, layerdepth);
        }

        public void UnloadContent()
        {
            return;
        }
    }
}
