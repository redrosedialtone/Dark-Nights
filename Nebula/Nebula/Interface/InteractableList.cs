using Microsoft.Xna.Framework;
using Nebula.Input;
using Nebula.Main;
using System;

namespace Nebula.Program.Interface
{
    public class InteractableList : UIElement, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        private Color color = Color.White;
        private string text;
        private UIText _titleUIText;

        public InteractableList(string Title, Point Position)
        {
            text = Title;
            origin = Position;
        }

        public override void Init()
        {           
            _titleUIText = new UIText("Constantina", text, color,origin, UIText.TextAlignmentHorizontal.Left, UIText.TextAlignmentVertical.Middle);
            descendantUIElements = new IUIObject[] { _titleUIText };

            base.Init();

            SetSize(_titleUIText);
        }

        public bool PointerClick(MouseButtonActionState Data)
        {
            log.Info("Clicked!!");
            return true;
        }

        public bool PointerDown(MouseButtonActionState Data)
        {
            log.Info("Down!!");
            return true;
        }

        public bool PointerEnter(MouseButtonActionState Data)
        {
            _titleUIText.SetColor(Color.LightGray);
            return true;
        }

        public bool PointerExit(MouseButtonActionState Data)
        {
            _titleUIText.SetColor(Color.White);
            return true;
        }
    }
}
