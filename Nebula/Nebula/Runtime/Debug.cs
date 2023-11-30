using Microsoft.Xna.Framework;
using Nebula.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nebula.Runtime
{
    public interface IGizmo
    {
        bool Enabled { get; }
        void Update();
        void Draw();
    }

    public static class Debug
    {
        public static readonly NLog.Logger log = NLog.LogManager.GetLogger("DEBUG");

        public static bool Enabled { get; private set; }
        public static bool Gizmos { get; private set; }
        private static List<IGizmo> debugGizmos = new List<IGizmo>();
        private static List<IGizmo> worldGizmos = new List<IGizmo>();

        static Debug()
        {
            #if DEBUG
                 Enabled = true;
                 Gizmos = true;
            #else
                Enabled = false;
                LogsEnabled = true;
            #endif
        }

        public static void Toggle() { Enabled = !Enabled; }
        public static void Toggle(bool toggle) { Enabled = toggle; }

        public static void Update()
        {
            if (Main.Input.OnPress(InputID.ToggleDebug))
            {
                Toggle();
            }
            if (!Enabled) return;
            if (Gizmos)
            {
                foreach (var gizmo in debugGizmos)
                {
                    if(gizmo.Enabled) gizmo.Update();
                }
            }
        }

        public static void Draw()
        {
            if (!Enabled) return;
            if (Gizmos)
            {
                foreach (var gizmo in worldGizmos)
                {
                    if (gizmo.Enabled) gizmo.Draw();
                }
            }
        }

        public static void DebugDraw()
        {
            if (!Enabled) return;
            if (Gizmos)
            {
                foreach (var gizmo in debugGizmos)
                {
                    if(gizmo.Enabled) gizmo.Draw();
                }
            }
        }

        public static void NewDebugGizmo(IGizmo newGizmo) { debugGizmos.Add(newGizmo); }
        public static void NewWorldGizmo(IGizmo newGizmo) { worldGizmos.Add(newGizmo); }
    }

    public class DebugModeGizmo : IGizmo
    {
        public bool Enabled { get; set; }

        public DebugModeGizmo()
        {
            Debug.NewDebugGizmo(this);
        }

        public void Update() { }

        public void Draw()
        {
            if (Debug.Enabled)
            {
                Vector2 screenPos = new Vector2(Graphics.RENDER_WIDTH/2,5);
                DrawUtils.DrawText("DEBUG MODE",screenPos,Color.Yellow,1.5f);
            }
        }
    }
}
