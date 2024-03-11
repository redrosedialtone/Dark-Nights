using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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

        public static bool DebugPause = false;
        public static int DebugNextFrame = 0;

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

        public static void Initialised()
        {
            DebugInputCtxt ctxt = new DebugInputCtxt();
            ctxt.Setup();
            Main.Input.Get.EnableContext("debugInput");

            DebugInputMap map = new DebugInputMap();
            map.Setup();

            ctxt.ToggleDebug += Toggle;
            ctxt.PauseFrame += PauseFrame;
            ctxt.NextFrame += NextFrame;
        }

        public static void Update()
        {
            if (!Enabled) return;
            if (Gizmos)
            {
                foreach (var gizmo in debugGizmos)
                {
                    if(gizmo.Enabled) gizmo.Update();
                }
            }
            if (DebugPause)
            {
                if (DebugNextFrame > 0)
                {
                    if (!Time.TickEnabled) Time.SetTickEnabled(true);
                    DebugNextFrame--;
                }
                else { if (Time.TickEnabled) Time.SetTickEnabled(false); }
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

        public static void Toggle() { Toggle(!Enabled); }
        public static void Toggle(bool toggle) { Enabled = toggle; }

        public static void PauseFrame()
        {
            DebugPause = !DebugPause;
            Time.SetTickEnabled(!DebugPause);
        }

        public static void NextFrame()
        {
            if(DebugPause) DebugNextFrame++;
        }
    }

    public class DebugInputCtxt : InputContext
    {
        public override string Name => "debugInput";

        public Action ToggleDebug;
        public Action PauseFrame;
        public Action NextFrame;

        public void Setup()
        {
            Main.Input.Get.AddInput("InputID.ToggleDebug", new InputActionData("InputID.ToggleDebug"));
            Main.Input.Get.AddInput("InputID.PauseFrame", new InputActionData("InputID.PauseFrame"));
            Main.Input.Get.AddInput("InputID.NextFrame", new InputActionData("InputID.NextFrame"));
            Main.Input.Get.AddContext(this);
        }

        public override void ProcessActions(GameTime time)
        {
            var toggleDebug = Main.Input.GetData<InputActionData>("InputID.ToggleDebug");
            if (toggleDebug != null && toggleDebug.PressedThisFrame) ToggleDebug?.Invoke();

            var pauseFrame = Main.Input.GetData<InputActionData>("InputID.PauseFrame");
            if (pauseFrame != null && pauseFrame.PressedThisFrame) PauseFrame?.Invoke();

            var nextFrame = Main.Input.GetData<InputActionData>("InputID.NextFrame");
            if (nextFrame != null && nextFrame.PressedThisFrame) NextFrame?.Invoke();
        }
    }

    public class DebugInputMap : IInputMap
    {
        public string Name => "debugInputMap";

        public static Dictionary<string, Keys> ActionMap = new Dictionary<string, Keys>()
        {
            { "InputID.ToggleDebug", Keys.LeftControl },
            { "InputID.PauseFrame", Keys.Space },
            { "InputID.NextFrame", Keys.OemPeriod },
        };

        public void Setup()
        {
            Main.Input.Get.AddInput("InputID.ToggleDebug", new InputActionData("InputID.ToggleDebug"));
            Main.Input.Get.AddInput("InputID.PauseFrame", new InputActionData("InputID.PauseFrame"));
            Main.Input.Get.AddInput("InputID.NextFrame", new InputActionData("InputID.NextFrame"));
            Main.Input.Get.AddInputMap(this);
        }

        public IEnumerable<InputActionState> MapActions()
        {
            var keyboard = Keyboard.GetState();

            foreach (var input in ActionMap)
            {
                Keys button = input.Value;
                string ID = input.Key;

                bool up = keyboard.IsKeyUp(button);
                InputActionState action = new InputActionState();
                action.ID = ID;
                action.State = up ? ButtonState.Released : ButtonState.Pressed;

                yield return action;
            }
        }

        public IEnumerable<InputRangeState> MapRanges()
        {
            yield break;
        }
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
                DrawUtils.DrawTextToScreen("DEBUG MODE",screenPos,Color.Yellow,1.5f);
            }
        }
    }
}
