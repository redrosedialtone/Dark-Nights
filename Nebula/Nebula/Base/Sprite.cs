using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Main;
using System;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

namespace Nebula
{
    public class Sprite2D
    {
        public Texture2D Texture { get; private set; }
        public Vector2 Pivot { get; private set; }
        public Rectangle SourceRect { get; private set; }

        public Sprite2D(Texture2D texture, Rectangle sourceRect)
        {
            Texture = texture;
            SourceRect = sourceRect;
            Pivot = Vector2.Zero;
        }

        public Sprite2D(Texture2D texture, Rectangle sourceRect, Vector2 pivot)
        {
            Texture = texture;
            SourceRect = sourceRect;
            Pivot = pivot;
        }

        public Vector2 WidthHeightToVector2()
        {
            Vector2 widthheight = Vector2.Zero;

            widthheight.X = SourceRect.Width;
            widthheight.Y = SourceRect.Height;

            return widthheight;
        }
    }

    public class ExpandableTexture
    {
        public Texture2D Texture { get; protected set; }
        public Vector2 Pivot { get; private set; }
        public Rectangle? SourceRect { get; protected set; }
        public Rectangle[] Regions { get; protected set; }
        public int Slices => 9;

        public int LeftLine { get; private set; }
        public int RightLine { get; private set; }
        public int TopLine { get; private set; }
        public int BottomLine { get; private set; }

        public ExpandableTexture(Texture2D tex, Rectangle? sourceRect, int leftCutoff, int rightCutoff, int topCutoff, int bottomCutoff, Vector2 pivot)
        {
            SetTexture(tex);
            SetSourceRect(sourceRect);
            SetCutoffRegions(leftCutoff, rightCutoff, topCutoff, bottomCutoff);
            Pivot = pivot;
        }

        public ExpandableTexture(Texture2D tex, Rectangle? sourceRect, int leftCutoff, int rightCutoff, int topCutoff, int bottomCutoff)
        {
            SetTexture(tex);
            SetSourceRect(sourceRect);
            SetCutoffRegions(leftCutoff, rightCutoff, topCutoff, bottomCutoff);
            Pivot = Vector2.Zero;
        }

        public void SetTexture(Texture2D texture)
        {
            Texture = texture;
        }

        public void SetSourceRect(Rectangle? sourceRect)
        {
            SourceRect = sourceRect;
        }

        public void SetCutoffRegions(int leftCutoff, int rightCutoff, int topCutoff, int bottomCutoff)
        {
            LeftLine = leftCutoff;
            RightLine = rightCutoff;
            TopLine = topCutoff;
            BottomLine = bottomCutoff;

            Regions = CreateRegions(SourceRect.HasValue ? SourceRect.Value : Texture.Bounds);
        }

        public Rectangle GetRectForIndex(Rectangle rectangle, int index)
        {
            int x = rectangle.X;
            int y = rectangle.Y;
            int width = rectangle.Width;
            int height = rectangle.Height;
            int middleWidth = width - LeftLine - RightLine;
            int middleHeight = height - TopLine - BottomLine;
            int bottomY = y + height - BottomLine;
            int rightX = x + width - RightLine;
            int leftX = x + LeftLine;
            int topY = y + TopLine;

            switch (index)
            {
                //Upper-region
                case 0: return new Rectangle(x, y, LeftLine, TopLine);
                case 1: return new Rectangle(leftX, y, middleWidth, TopLine);
                case 2: return new Rectangle(rightX, y, RightLine, TopLine);

                //Middle-region
                case 3: return new Rectangle(x, topY, LeftLine, middleHeight);
                case 4: return new Rectangle(leftX, topY, middleWidth, middleHeight);
                case 5: return new Rectangle(rightX, topY, RightLine, middleHeight);

                //Lower-region
                case 6: return new Rectangle(x, bottomY, LeftLine, BottomLine);
                case 7: return new Rectangle(leftX, bottomY, middleWidth, BottomLine);
                case 8: return new Rectangle(rightX, bottomY, RightLine, BottomLine);

                default: return rectangle;
            }
        }

        public Rectangle[] CreateRegions(Rectangle rectangle)
        {
            List<Rectangle> regions = new List<Rectangle>();

            for (int i = 0; i < Slices; i++)
            {
                regions.Add(GetRectForIndex(rectangle, i));
            }

            return regions.ToArray();
        }
    }
}

