using Microsoft.Xna.Framework.Graphics;
using Nebula;
using Nebula.Main;
using Nebula.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{
    public class AssetManager : Manager
    {
        #region Static
        private static AssetManager instance;
        public static AssetManager Get => instance;

        private static readonly NLog.Logger log = NLog.LogManager.GetLogger("ASSET");
        #endregion

        public const string SpriteRoot = "Sprites";

        private Dictionary<string, Texture2D> textures;

        public override void Init()
        {
            log.Info("> ..");
            instance = this;
            textures = new Dictionary<string, Texture2D>();

            ApplicationController.Get.Initiate(this);
        }

        public Texture2D LoadTexture(string path)
        {
            Texture2D texture;
            if (textures.TryGetValue(path, out texture) == false)
            {
                texture = Resources.Load<Texture2D>(path);
                textures.Add(path, texture);
            }
            return texture;
        }
    }
}
