using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula;
using Nebula.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights.Interface
{
    public interface IMenu
    {
        bool IsOpen { get; }
        bool AllowInteraction { get; set; }

        bool OpenMenu();
        bool CloseMenu();
        void ExitMenu();

        void MenuFocused();
        void MenuDefocused();
        bool AllowMenuFocusChange();

        void Tick();
        void Draw();
    }

    public abstract class MenuBase : IMenu
    {
        public event Action OnMenuOpen;
        public event Action OnMenuClose;
        public bool AllowInteraction { get; set; }
        public bool IsOpen { get; private set; }

        protected bool MenuFocus = false;
        protected bool allowInteraction = true;

        public virtual void Init()
        {
            
        }

        public virtual bool OpenMenu()
        {
            IsOpen = true;
            OnMenuOpen?.Invoke();
            return true;
        }
        public virtual bool CloseMenu()
        {
            IsOpen = false;
            OnMenuClose?.Invoke();
            return true;
        }
        public virtual void ExitMenu()
        {

        }

        public virtual void ClearMenu()
        {

        }

        public virtual void MenuFocused()
        {
            MenuFocus = true;
        }
        public virtual void MenuDefocused()
        {
            MenuFocus = false;
        }
        public virtual bool AllowMenuFocusChange()
        {
            return true;
        }
        public virtual void Tick() { }

        public abstract void  Draw();
    }


}
