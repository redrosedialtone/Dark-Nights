using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework;

namespace Nebula.Main
{
    public enum LoggingLevel
    {
        NeverShow = 0,
        Exception = 1,
        Error = 2,
        Warning = 3,
        Log = 4,
        AlwaysShow = 5
    }

    public interface ILogCreator
    {
        LoggingLevel LoggingLevel { get; set; }
        List<string> Logs { get; set; }
    }

    public static class LoggerExtension
    {
        public static void Log(this ILogCreator self, string text, LoggingLevel level = LoggingLevel.Log)
        {
            text = text.Insert(0, "(" + level.ToString().ToUpper() + ":");
            if ((int)level <= 2)
            {
                text = text.Insert(0, "!!!\n");
                text = text.Insert(text.Length, "\n!!!");
            }
            //self.Logs.Add(text);

            if ((int)level <= (int)self.LoggingLevel)
            {
                //                Debug.Log(text);
            }
        }
    }
}
