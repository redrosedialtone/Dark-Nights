using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;

namespace Nebula.Main
{
    public class Resources : IControl
    {

        #region Singleton
        private static readonly NLog.Logger log = NLog.LogManager.GetLogger("RESOURCES");
        public static Resources Get
        {
            get
            {
                if (instance == null)
                {
                    instance = new Resources();
                }

                return instance;
            }
        }
        private static Resources instance = null;
        #endregion

        public ContentManager Content => NebulaRuntime.Content;

        private Resources() { }

        public void Create(NebulaRuntime game)
        {
            log.Info("> ..");
        }

        public void Initialise() { }
        public void LoadContent() { }

        public void UnloadContent() { }
        public void Update(GameTime gameTime) { }

        public void Draw(GameTime gameTime) { }

        public static T Load<T>(string _content)
        {
            return Get.Instance_Load<T>(_content);
        }

        private T Instance_Load<T>(string _content)
        {
            log.Trace($"Attempting to load \"{_content}\"..");
            try
            {
                return Content.Load<T>(_content);
            }
            catch (Exception)
            {
                log.Warn($"Failed to load \"{_content}\"..");
                throw;
            }
        }
    }
}