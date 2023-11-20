using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Nebula.Main
{
    public interface IControl
    {
        void Create(Runtime game);
        void Initialise();
        void LoadContent();
        void UnloadContent();
        void Update(GameTime gameTime);
        void Draw(GameTime gameTime);
    }
}
