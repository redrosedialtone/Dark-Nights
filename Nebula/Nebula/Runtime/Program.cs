using System;

using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using NLog;
using System.Threading;

namespace Nebula.Main
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.Setup().LoadConfigurationFromFile().GetCurrentClassLogger();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Thread debugThread = new Thread(DebugOutput);
            debugThread.Start();
            Logger.Info("Game Init..");
            using (var game = new Runtime())
            {
                game.Run();
            }
        }

        static void DebugOutput()
        {
            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool AllocConsole();
            AllocConsole();
            Console.Title = "Nebula Debug Output";
            Console.WriteLine("[DEBUG OUTPUT]");
            bool _exec = true;
            do 
            {
                var com = Console.ReadLine();
                if (com == "EXIT")
                {
                    _exec = false;
                }
                DebugCom(com);
            } while (_exec == true);
            Environment.Exit(0);
        }

        enum DebugCommands
        {
            EXIT = 0,
            SETLOG = 1
        }

        static void DebugCom(string com)
        {
            string[] args = com.Split(" ");
            bool success = Enum.TryParse<DebugCommands>(args[0], out DebugCommands command);
            if (!success)
            {
                Console.WriteLine($"Unknown Command::{com}");
                return;
            }
            switch (command)
            {
                case DebugCommands.EXIT:
                    break;
                case DebugCommands.SETLOG:
                    if(args.Length > 1)
                    {
                        Console.WriteLine($"Setting Global Log Level To .. {args[1]}");
                    }
                    else { Console.WriteLine($"FORMAT: SETLOG [LEVEL]"); }
                    
                    break;
                default:
                    break;
            }
        }
    }
}
