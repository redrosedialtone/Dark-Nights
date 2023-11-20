using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Nebula.Input;

namespace Nebula.Program.Interface
{
    public abstract class InterfaceMenu : GLWindow
    {
        protected List<InterfaceMenu> SubMenus = new List<InterfaceMenu>();

        public InterfaceMenu()
        {

        }
    }
}
