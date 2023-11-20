using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;

namespace Nebula.Main
{
    public class Resources : IControl
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetLogger("RESOURCES");
        public static Resources Access;

        public static ContentManager Content => Access._contentManager;
        private readonly ContentManager _contentManager;
        private Runtime RUNTIME;

        public Resources(ContentManager contentManager)
        {
            _contentManager = contentManager;
        }

        public void Create(Runtime game)
        {
            log.Info("[RESOURCES]");
            RUNTIME = game;
            Access = this;
        }

        public void Initialise()
        {
            
        }

        public void LoadContent()
        {
            
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

        public static T Load<T>(string _content)
        {
            return Access.Instance_Load<T>(_content);
        }

        private T Instance_Load<T>(string _content)
        {
            log.Info($"Attempting to load \"{_content}\"..");
            try
            {
                return _contentManager.Load<T>(_content);
            }
            catch (Exception)
            {
                log.Warn($"Failed to load \"{_content}\"..");
                throw;
            }
        }
    }
}