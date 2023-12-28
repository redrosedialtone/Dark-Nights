using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkNights
{
    public static class ColorSelector
    {
        // 14 colours
        private static Color[] DebugColors = new Color[]
        {
            new Color(125,95,106, 255),
            new Color(125,108,90,255),
            new Color(125,125,100, 255),
            new Color(85,125,100, 255),
            new Color(110,95 ,125, 255),
            new Color(115,15, 30, 255),
            new Color(125,70,24, 255),
            new Color(125,125,15, 255),
            new Color(105,125,30, 255),
            new Color(30,90,30, 255),
            new Color(45,120,120, 255),
            new Color(0,65,100, 255),
            new Color(72,15,90, 255),
            new Color(120,25,115, 255),
        };

        public static Color DebugColor(int i)
        {
            return DebugColors[i % DebugColors.Length];
        }
    }
}
