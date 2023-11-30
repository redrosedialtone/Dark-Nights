using System;
using System.Collections.Generic;
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
    class Interface : IControl
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public void Create(NebulaRuntime game)
        {
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
    }
}
