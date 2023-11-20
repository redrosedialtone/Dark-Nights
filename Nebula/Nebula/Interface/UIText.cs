using Microsoft.Xna.Framework.Graphics;
using System;
using Microsoft.Xna.Framework;

namespace Nebula.Program.Interface
{
    public class UIText : UIElement
    {
        /// <summary>
        /// Current Text
        /// </summary>
        public string Text => text;
        /// <summary>
        /// Color of the text
        /// </summary>
        public Color Color => color;
        /// <summary>
        /// Text's Font
        /// </summary>
        public SpriteFont Font => font;

        private readonly SpriteFont font;
        protected string text;
        protected Color color = Color.White;
        protected Rectangle rect;
        protected TextAlignmentHorizontal HorizontalAlignment = TextAlignmentHorizontal.Center;
        protected TextAlignmentVertical VerticalAlignment = TextAlignmentVertical.Middle;

        private float _textWidth;
        private float _textHeight;
        private Vector2 _textSize => new Vector2(_textWidth, _textHeight);
        private Vector2 drawPoint;

        public UIText(string fontName, string text, Color color, Point origin)
        {
            this.text = text;
            this.color = color;
            this.origin = origin;
            font = Nebula.Main.Runtime.GameContent.Load<SpriteFont>("FONT/"+fontName);
            Recalculate();
        }

        public UIText(string fontName, string text, Color color, Point origin, TextAlignmentHorizontal hAlignment, TextAlignmentVertical vAlignment)
        {
            this.text = text;
            this.color = color;
            this.origin = origin;
            font = Nebula.Main.Runtime.GameContent.Load<SpriteFont>("FONT/" + fontName);
            HorizontalAlignment = hAlignment;
            VerticalAlignment = vAlignment;
            Recalculate();
        }

        public enum TextAlignmentHorizontal
        {
            Left, Center, Right
        }

        public enum TextAlignmentVertical
        {
            Upper, Middle, Lower
        }

        public void SetText(string Text)
        {
            text = Text;
        }

        public void SetColor(Color color)
        {
            this.color = color;
        }

        public void Recalculate()
        {
            Vector2 tSize = font.MeasureString(text);
            _textWidth = tSize.X; _textHeight = tSize.Y;

            int _x = origin.X;
            int _y = origin.Y;

            switch (HorizontalAlignment)
            {
                case TextAlignmentHorizontal.Left:
                    _x = origin.X;
                    break;
                case TextAlignmentHorizontal.Center:
                    _x = (int)(origin.X -_textWidth / 2);
                    break;
                case TextAlignmentHorizontal.Right:
                    _x = (int)(origin.X + _textWidth);
                    break;
                default:
                    break;
            }
            switch (VerticalAlignment)
            {
                case TextAlignmentVertical.Upper:
                    _y = origin.Y;
                    break;
                case TextAlignmentVertical.Middle:
                    _y = origin.Y;
                    break;
                case TextAlignmentVertical.Lower:
                    _y = origin.Y;
                    break;
                default:
                    break;
            }

            drawPoint = new Vector2(_x, _y);
            size = new Vector2(_textWidth, _textHeight);
            SetSize(origin, (int)_textWidth, (int)_textHeight);
        }

        public override void DrawUI(SpriteBatch Batch)
        {
            Batch.DrawString(font, text, drawPoint, color);
            base.DrawUI(Batch);
        }
    }
}
