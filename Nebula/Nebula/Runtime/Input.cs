using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using Nebula.Input;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace Nebula.Main
{
    public enum InputID
    {
        LeftMouseButton = 0,
        RightMouseButton = 1,
        Left = 2,
        Right = 3,
        Up = 4,
        Down = 5,
        Scroll = 6,
        Shift = 7,
        Lock = 8,
        MiddleMouseButton = 9,
        LeftRotate = 10,
        RightRotate = 11,
    }

    public class MouseButtonActionState : IInputData
    {
        public PointerEventData buttonData;
        public ButtonState buttonState;
        public ButtonState previousState;
        public Point mousePosition;

        public InputID ID { get; set; }
        public bool Active => PressedThisFrame();

        public MouseButtonActionState(InputID ID)
        {
            this.ID = ID;
        }

        public bool PressedThisFrame()
        {
            return buttonState == ButtonState.Pressed && previousState == ButtonState.Released;
        }

        public bool ReleasedThisFrame()
        {
            return buttonState == ButtonState.Released && previousState == ButtonState.Pressed;
        }
    }

    public class PointerEventData
    {
        public IPointerDownHandler pressedEvent;
        public IPointerUpHandler releaseEvent;
        public IPointerClickHandler clickEvent;
        public IPointerEnterHandler enterEvent;
        public IPointerExitHandler exitEvent;

        public Point pressPosition;
        public bool elligibleForClick;
        public int clickCount;
        public float clickTime;
        public float delta;

    }

    public interface IInputData
    {
        public InputID ID { get; }
        public bool Active { get; }
    }

    public struct InputActionState
    {
        public InputID ID;
        public ButtonState State;

        public static bool operator ==(InputActionState a, InputActionState b) =>
            a.State == b.State;

        public static bool operator !=(InputActionState a, InputActionState b) =>
            a.State != b.State;

        public override bool Equals([NotNullWhen(true)] object obj)
        {
            if(obj != null && obj is InputActionState b)
            {
                return b.State == this.State;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return State.GetHashCode();
        }
    }

    public struct InputRangeState
    {
        public InputID ID;
        public float State;

        public static bool operator ==(InputRangeState a, InputRangeState b) =>
            a.State == b.State;

        public static bool operator !=(InputRangeState a, InputRangeState b) =>
            a.State != b.State;

        public override bool Equals([NotNullWhen(true)] object obj)
        {
            if (obj != null && obj is InputRangeState b)
            {
                return b.State == this.State;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return State.GetHashCode();
        }
    }

    public class InputActionData : IInputData
    {
        public InputID ID { get; private set; }
        public InputActionState Current;
        public InputActionState Previous;

        public InputActionData(InputID ID)
        {
            this.ID = ID;
        }

        public bool Active => Pressed();
        public bool PressedThisFrame => Current.State == ButtonState.Pressed && Previous.State == ButtonState.Released;

        public bool Pressed()
        {
            return Current.State == ButtonState.Pressed;
        }

        public bool Released()
        {
            return Current.State == ButtonState.Released;
        }
    }

    public class InputRangeData : IInputData
    {
        public InputID ID { get; private set; }
        public InputRangeState Current;
        public InputRangeState Previous;

        public InputRangeData(InputID ID)
        {
            this.ID = ID;
        }

        public bool Active => true;
    }

    public class Input : IControl
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetLogger("INPUT");
        public static Input Access;

        private List<IPointerEventListener> PointerListeners;

        public MouseState PreviousMousePointerEventData;
        public MouseState MousePointerEventData;

        private MouseButtonActionState leftClickButtonData;
        private MouseButtonActionState rightClickButtonData;
        private MouseButtonActionState middleClickButtonData;

        private KeyboardInputMap kbInput;
        public static DefaultCtxt DefaultCtxt;

        public Dictionary<string, IInputContext> InactiveCtxt = new Dictionary<string, IInputContext>();
        public Dictionary<string, IInputContext> ActiveCtxt = new Dictionary<string, IInputContext>();

        public void Create(NebulaRuntime game)
        {
            Access = this;
            PointerListeners = new List<IPointerEventListener>();
        }

        public void Draw(GameTime gameTime)
        {

        }

        public void Initialise()
        {
            SetupInputs();

            leftClickButtonData = (MouseButtonActionState)Data(InputID.LeftMouseButton);
            leftClickButtonData.buttonData = new PointerEventData();
            rightClickButtonData = (MouseButtonActionState)Data(InputID.RightMouseButton);
            rightClickButtonData.buttonData = new PointerEventData();
            middleClickButtonData = (MouseButtonActionState)Data(InputID.MiddleMouseButton);
            middleClickButtonData.buttonData = new PointerEventData();


            kbInput = new KeyboardInputMap();

            DefaultCtxt = new DefaultCtxt();
            AddContext(DefaultCtxt);
            EnableContext(DefaultCtxt.Name);
        }

        public void LoadContent()
        {

        }

        public void UnloadContent()
        {

        }

        public void AddContext(IInputContext ctxt)
        {
            if (InactiveCtxt.ContainsKey(ctxt.Name)) { log.Debug($"InputCtxt {ctxt.Name} already exists!"); return; }
            InactiveCtxt.Add(ctxt.Name, ctxt);
        }

        public void EnableContext(string name)
        {
            if (InactiveCtxt.TryGetValue(name, out IInputContext ctxt))
            {
                ActiveCtxt.Add(name, ctxt);
                InactiveCtxt.Remove(name);
                log.Trace($"InputCtxt {name} enabled.");
            }
            else log.Debug($"InputCtxt {name} not inactive!");
        }

        public void DisableContext(string name)
        {
            if (ActiveCtxt.TryGetValue(name, out IInputContext ctxt))
            {
                InactiveCtxt.Add(name, ctxt);
                ActiveCtxt.Remove(name);
                log.Trace($"InputCtxt {name} disabled.");
            }
            else log.Debug($"InputCtxt {name} not active!");
        }

        public void Update(GameTime gameTime)
        {
            ProcessMouseData();

            foreach (var input in kbInput.MapActions())
            {
                var data = Data(input.ID);
                if(data is InputActionData actionData)
                {
                    actionData.Previous = actionData.Current;
                    actionData.Current = input;

                    if (actionData.Current != actionData.Previous)
                    {
                        log.Debug($"{input.ID.ToString()}::{actionData.Current.State.ToString()}");
                    }
                } 
            }
            foreach (var range in kbInput.MapRanges())
            {
                var data = Data(range.ID);
                if (data is InputRangeData rangeData)
                {
                    rangeData.Previous = rangeData.Current;
                    rangeData.Current = range;

                    if (rangeData.Current != rangeData.Previous)
                    {
                        log.Debug($"{range.ID.ToString()}::{rangeData.Current.State.ToString()}");
                    }
                }
            }

            if (ActiveCtxt.Count > 0)
            {
                foreach (var ctxt in ActiveCtxt)
                {
                    ctxt.Value.ProcessActions(gameTime);
                }
            }
        }

        // Mouse Actions
        #region Mouse
        public static void AddPointerEventListener(IPointerEventListener Listener)
        {
            Access.PointerListeners.Add(Listener);
            log.Debug("Adding Listener.. " + Access.PointerListeners.Count);
        }

        private void ProcessMouseData()
        {
            PreviousMousePointerEventData = MousePointerEventData;
            MousePointerEventData = Mouse.GetState();

            leftClickButtonData.previousState = leftClickButtonData.buttonState;
            leftClickButtonData.buttonState = MousePointerEventData.LeftButton;
            leftClickButtonData.mousePosition = MousePointerEventData.Position;

            rightClickButtonData.previousState = rightClickButtonData.buttonState;
            rightClickButtonData.buttonState = MousePointerEventData.RightButton;
            rightClickButtonData.mousePosition = MousePointerEventData.Position;

            middleClickButtonData.previousState = middleClickButtonData.buttonState;
            middleClickButtonData.buttonState = MousePointerEventData.MiddleButton;
            middleClickButtonData.mousePosition = MousePointerEventData.Position;

            List<IPointerEventListener> listenersIntersectingCursor = new List<IPointerEventListener>();
            foreach (var listener in PointerListeners)
            {
                var eventListener = listener.Intersect(MousePointerEventData.Position);
                if (eventListener != null)
                {
                    listenersIntersectingCursor.Add(eventListener);
                }
            }

            IPointerEventListener[] Events = listenersIntersectingCursor.ToArray();
            ProcessMouseButton(leftClickButtonData, Events);
            ProcessMouseButton(rightClickButtonData, Events);
            ProcessMouseButton(middleClickButtonData, Events);

            ProcessMouseOver(leftClickButtonData, Events);
        }

        private void ProcessMouseButton(MouseButtonActionState Data, IPointerEventListener[] Events)
        {
            PointerEventData pointerData = Data.buttonData;
            bool Pressed = Data.PressedThisFrame();
            bool Released = Data.ReleasedThisFrame();
            if (!Pressed && !Released)
            {
                return;
            }
            if (Pressed)
            {
                pointerData.elligibleForClick = true;
                pointerData.delta = 0;
                pointerData.pressPosition = Data.mousePosition;

                IPointerDownHandler pointerDownExecuted = ExecuteEvents.ExecuteHierarchy<IPointerDownHandler>(Events, Data, ExecuteEvents.pointerDown);
                IPointerClickHandler clickEvent = ExecuteEvents.GetEventListener<IPointerClickHandler>(Events, Data, ExecuteEvents.pointerClick);


                float dT = Time.DeltaTime;
                if (clickEvent != null && clickEvent == pointerData.clickEvent)
                {
                    float timeSinceLastClick = dT - pointerData.clickTime;
                    if (timeSinceLastClick < 0.3F)
                    {
                        pointerData.clickCount++;
                    }
                    else
                    {
                        pointerData.clickCount = 1;
                    }
                    pointerData.clickTime = dT;
                }
                else
                {
                    pointerData.clickCount = 0;
                }

                pointerData.pressedEvent = pointerDownExecuted;
                pointerData.clickEvent = clickEvent;

            }
            if (Released)
            {
                IPointerUpHandler pointerUpExecuted = ExecuteEvents.ExecuteHierarchy<IPointerUpHandler>(Events, Data, ExecuteEvents.pointerUp);
                IPointerClickHandler clickEvent = ExecuteEvents.GetEventListener<IPointerClickHandler>(Events, Data, ExecuteEvents.pointerClick);

                if (pointerData.elligibleForClick && clickEvent == pointerData.clickEvent)
                {
                    ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(Events, Data, ExecuteEvents.pointerClick);
                }
                else
                {
                    pointerData.clickEvent = null;
                }

                pointerData.elligibleForClick = false;
                pointerData.pressedEvent = null;
                pointerData.releaseEvent = pointerUpExecuted;
            }
            log.Debug($"{Data.ID.ToString()}::{Data.buttonState.ToString()}");
        }

        private void ProcessMouseOver(MouseButtonActionState Data, IPointerEventListener[] Events)
        {
            PointerEventData pointerData = Data.buttonData;
            IPointerEnterHandler enterEvent = ExecuteEvents.GetEventListener<IPointerEnterHandler>(Events, Data, ExecuteEvents.EventHandle.PointerEnter);
            IPointerExitHandler exitEvent = ExecuteEvents.GetEventListener<IPointerExitHandler>(Events, Data, ExecuteEvents.EventHandle.PointerExit);

            if (pointerData.enterEvent != enterEvent)
            {
                if (pointerData.exitEvent != null)
                {
                    ExecuteEvents.ExecuteEvent(pointerData.exitEvent, Data, ExecuteEvents.EventHandle.PointerExit);
                }
                ExecuteEvents.ExecuteHierarchy<IPointerExitHandler>(Events, Data, ExecuteEvents.EventHandle.PointerExit);
                if (enterEvent != null)
                {
                    ExecuteEvents.ExecuteHierarchy<IPointerEnterHandler>(Events, Data, ExecuteEvents.EventHandle.PointerEnter);
                }
            }

            pointerData.enterEvent = enterEvent;
            pointerData.exitEvent = exitEvent;
        }
        #endregion

        //Keyboard Actions

        private Dictionary<InputID, IInputData> Inputs = new Dictionary<InputID, IInputData>();

        private void SetupInputs()
        {
            Inputs.Add(InputID.Up, new InputActionData(InputID.Up));
            Inputs.Add(InputID.Left, new InputActionData(InputID.Left));
            Inputs.Add(InputID.Right, new InputActionData(InputID.Right));
            Inputs.Add(InputID.Down, new InputActionData(InputID.Down));
            Inputs.Add(InputID.Scroll, new InputRangeData(InputID.Scroll));
            Inputs.Add(InputID.Shift, new InputActionData(InputID.Shift));
            Inputs.Add(InputID.Lock, new InputActionData(InputID.Lock));
            Inputs.Add(InputID.LeftMouseButton, new MouseButtonActionState(InputID.LeftMouseButton));
            Inputs.Add(InputID.RightMouseButton, new MouseButtonActionState(InputID.RightMouseButton));
            Inputs.Add(InputID.MiddleMouseButton, new MouseButtonActionState(InputID.MiddleMouseButton));
            Inputs.Add(InputID.LeftRotate, new InputActionData(InputID.LeftRotate));
            Inputs.Add(InputID.RightRotate, new InputActionData(InputID.RightRotate));
        }

        public static bool Active(InputID ID) => Access.Instance_Active(ID);

        public bool Instance_Active(InputID ID)
        {
            var data = Instance_Data(ID);
            if (data != null)
            {
                return data.Active;
            }
            return false;
        }

        public static IInputData Data(InputID ID) => Access.Instance_Data(ID);

        public IInputData Instance_Data(InputID ID)
        {
            if (Inputs.TryGetValue(ID, out IInputData data))
            {
                return data;
            }
            return null;
        }
    }
}
