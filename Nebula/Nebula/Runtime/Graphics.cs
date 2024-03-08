using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nebula.Runtime;

namespace Nebula.Main
{
    public enum RenderTarget
    {
        ToCamera,
        ToScreen,
        ToDebug
    }

    public class Graphics : IControl
    {
        #region Singleton
        private static readonly NLog.Logger log = NLog.LogManager.GetLogger("GRAPHICS");
        public static Graphics Get
        {
            get
            {
                if (instance == null)
                {
                    instance = new Graphics();
                }

                return instance;
            }
        }
        private static Graphics instance = null;
        #endregion

        public static SpriteBatch SpriteBatch => Get._spriteBatch;
        public static SpriteBatch UIBatch => Get._uiBatch;

        public GraphicsDeviceManager GraphicsDeviceMngr => NebulaRuntime.GraphicsDeviceMgr;
        public GraphicsDevice GraphicsDevice => NebulaRuntime.GraphicsDevice;
        public static int SCREEN_WIDTH = (int)(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width);
        public static int SCREEN_HEIGHT = (int)(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height);
        public static int SCREEN_ASPECT => SCREEN_WIDTH / SCREEN_HEIGHT;
        public static int RENDER_WIDTH => 1366;
        public static int RENDER_HEIGHT => 768;
        public static int RENDER_ASPECT => RENDER_WIDTH / RENDER_HEIGHT;
        public Vector2 ScreenSize => new Vector2(GraphicsDeviceMngr.GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDeviceMngr.GraphicsDevice.PresentationParameters.BackBufferHeight);
        public Vector2 ScreenCentre => new Vector2((int)(ScreenSize.X / 2), (int)(ScreenSize.Y / 2));
        public Vector2 ScreenResolution => ScreenSize / new Vector2(RENDER_WIDTH, RENDER_HEIGHT);

        private SpriteBatch _spriteBatch;
        private SpriteBatch _uiBatch;
        private RenderTarget2D mainRenderTarget;

        //Texture2D ballTexture;

        private Graphics() { }

        public void Create(NebulaRuntime game) { }

        public void Initialise()
        {
            log.Info("> ..");
            //Graphics.IsFullScreen = true;
            GraphicsDeviceMngr.PreferredBackBufferWidth = RENDER_WIDTH;
            GraphicsDeviceMngr.PreferredBackBufferHeight = RENDER_HEIGHT;
            GraphicsDeviceMngr.ApplyChanges();
            mainRenderTarget = new RenderTarget2D(GraphicsDevice, RENDER_WIDTH, RENDER_HEIGHT, false, SurfaceFormat.Color, DepthFormat.None);
            _spriteBatch = new SpriteBatch(NebulaRuntime.GraphicsDevice);
            _uiBatch = new SpriteBatch(NebulaRuntime.GraphicsDevice);
            DrawUtils.Setup(_spriteBatch);
        }

        public void LoadContent() { }
        public void Update(GameTime gameTime) { }
        public void UnloadContent() { }

        public void Draw(GameTime gameTime)
        {
            DrawToScreen();

        }

        public void PreDraw()
        {
            GraphicsDevice.SetRenderTarget(mainRenderTarget);
            GraphicsDevice.Clear(ClearOptions.Target, new Color(200, 200, 200), 1.0f, 0);
            SpriteBatchRenderer.Get.StartDraw();
        }

        public void DrawToDebug()
        {
            Cursor.Get.DrawCursor(_uiBatch);
            Debug.DebugDraw();
        }

        public void DrawToScreen()
        {

        }

        public void PostDraw()
        {
            Debug.Draw();
            SpriteBatchRenderer.Get.FinishDraw();
            _uiBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend);
            DrawToDebug();
            _uiBatch.End();


            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(ClearOptions.Target, new Color(200, 200, 200), 1.0f, 0);

            _spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, null);
            _spriteBatch.Draw(mainRenderTarget, Vector2.Zero, null, Color.White, 0f, new Vector2(0, 0), ScreenResolution, SpriteEffects.None, 1f);
            _spriteBatch.End();
        }
    }

    public class DrawCallGizmo : IGizmo
    {

        public bool Enabled { get; set; }


        public DrawCallGizmo()
        {
            Debug.NewDebugGizmo(this);
        }

        public void Update() { }

        public void Draw()
        {
            var drawCount = Graphics.Get.GraphicsDevice.Metrics.DrawCount;
            var fps = string.Format("Draws: {0:0.##}", drawCount);
            DrawUtils.DrawText(fps, new Vector2(1, 15), Color.Yellow);
        }
        public void UnloadContent()
        {
            return;
        }
    }
}
