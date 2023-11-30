using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Nebula.Main
{


    public interface IInputMap
    {
        IEnumerable<InputActionState> MapActions();
        IEnumerable<InputRangeState> MapRanges();
    }

    public class KeyboardInputMap : IInputMap
    {
        public static Dictionary<InputID, Keys> ActionMap = new Dictionary<InputID, Keys>()
        {
            { InputID.Left, Keys.A },
            { InputID.Right, Keys.D },
            { InputID.Up, Keys.W },
            { InputID.Down, Keys.S },
            { InputID.Shift, Keys.LeftShift },
            { InputID.Lock, Keys.F },
            { InputID.LeftRotate, Keys.Q },
            { InputID.RightRotate, Keys.E },
            { InputID.ToggleDebug, Keys.LeftControl }
        };

        public IEnumerable<InputActionState> MapActions()
        {
            var keyboard = Keyboard.GetState();

            foreach (var input in ActionMap)
            {
                Keys button = input.Value;
                InputID ID = input.Key;

                bool up = keyboard.IsKeyUp(button);
                InputActionState action = new InputActionState();
                action.ID = ID;
                action.State = up ? ButtonState.Released : ButtonState.Pressed;

                yield return action;
            }
        }

        public IEnumerable<InputRangeState> MapRanges()
        {
            var mouse = Input.Get.MousePointerEventData;
            var prevMouse = Input.Get.PreviousMousePointerEventData;

            InputRangeState range = new InputRangeState();
            range.ID = InputID.Scroll;
            range.State = mouse.ScrollWheelValue - prevMouse.ScrollWheelValue;

            yield return range;
        }
    }
}
