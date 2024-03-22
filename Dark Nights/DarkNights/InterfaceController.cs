using DarkNights.Interface;
using Nebula.Systems;
using Nebula.Main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nebula;
using Microsoft.Xna.Framework;
using Nebula.Input;

namespace DarkNights
{
    public enum InterfaceMenus
    {
        None = 0,
        InventoryMenu = 1
    }

    public class InterfaceController : Manager
    {
        #region Static
        private static InterfaceController instance;
        public static InterfaceController Get => instance;

        public static readonly NLog.Logger log = NLog.LogManager.GetLogger("INTERFACE");
        #endregion

        public List<IMenu> MenuFocusHierarchy = new List<IMenu>();

        public InventoryMenu InventoryMenu { get; private set; }

        private IMenu curFocusedMenu;


        public override void Init()
        {
            log.Info("> ..");
            instance = this;
            ApplicationController.Get.Initiate(this);

            InventoryMenu = new InventoryMenu();
        }

        public override void OnInitialized()
        {
            base.OnInitialized();
        }

        public override void Tick()
        {
            InventoryMenu.Tick();
        }

        public override void Draw()
        {
            InventoryMenu.Draw();
        }

        public void Exit()
        {
            if (curFocusedMenu != null)
            {
                ExitMenu(curFocusedMenu);
            }
        }

        public void CloseMenu(IMenu Menu)
        {
            log.Info("Closing Menu..." + Menu.GetType());
            if (Menu.IsOpen)
            {
                if (Menu.CloseMenu())
                {
                    OnMenuClosed(Menu);
                }
            }
        }

        public void OpenMenu(IMenu Menu)
        {
            log.Info("Opening Menu..." + Menu.GetType());
            if (!Menu.IsOpen)
            {
                if (Menu.OpenMenu())
                {
                    FocusMenu(Menu);
                }
            }
        }

        public void FocusMenu(IMenu Menu)
        {
            MenuFocusHierarchy.Remove(Menu);
            MenuFocusHierarchy.Insert(0, Menu);
            UpdateInteractionStack();
        }

        public void DefocusMenu(IMenu Menu)
        {
            MenuFocusHierarchy.Remove(Menu);
            MenuFocusHierarchy.Add(Menu);
            UpdateInteractionStack();
        }

        private void OnMenuClosed(IMenu Menu)
        {
            MenuFocusHierarchy.Remove(Menu);
            UpdateInteractionStack();
        }

        public void ExitMenu(IMenu Menu)
        {
            Menu.ExitMenu();
            UpdateInteractionStack();
        }

        private void UpdateInteractionStack()
        {
            bool allowInteraction = true;
            if (MenuFocusHierarchy.Count > 0)
            {
                if (curFocusedMenu != MenuFocusHierarchy[0])
                {
                    log.Info("MenuFocus changed from {0} to {1}", curFocusedMenu, MenuFocusHierarchy[0]);
                    if (curFocusedMenu != null)
                    {
                        curFocusedMenu.MenuDefocused();
                    }
                    curFocusedMenu = MenuFocusHierarchy[0];
                    curFocusedMenu.MenuFocused();
                }
                foreach (var menu in MenuFocusHierarchy)
                {
                    menu.AllowInteraction = allowInteraction;
                    if (allowInteraction)
                    {
                        allowInteraction = menu.AllowMenuFocusChange();
                    }
                }
            }
            else
            {
                curFocusedMenu = null;
            }
        }

        public void OpenMenu(InterfaceMenus MenuType)
        {
            OpenMenu(MenuByType(MenuType));
        }

        public void CloseMenu(InterfaceMenus MenuType)
        {
            CloseMenu(MenuByType(MenuType));
        }

        public IMenu MenuByType(InterfaceMenus MenuType)
        {
            switch (MenuType)
            {
                case InterfaceMenus.None:
                    return null;
                case InterfaceMenus.InventoryMenu:
                    return InventoryMenu;
                default:
                    return null;
            }
        }
    }

    public class InterfaceButton : IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerDragHandler
    {
        public Rectangle Bounds { get; private set; }
        public IPointerEventListener Parent { get; private set; }
        public IPointerEventListener[] Children { get; private set; }

        public event Action<InterfaceButton, MouseButtonActionState> OnClicked;
        public event Action<InterfaceButton, MouseButtonActionState> OnDoubleClicked;
        public event Action<InterfaceButton, MouseButtonActionState> OnFocus;
        public event Action<InterfaceButton, MouseButtonActionState> OnUnfocus;
        public event Action<InterfaceButton, MouseButtonActionState> OnBeginDrag;
        public event Action<InterfaceButton, MouseButtonActionState> OnEndDrag;

        protected Color UIPressed { get; }
        protected Color UISelected { get; }
        protected Color UIInactive { get; }
        protected Color UIHighlight { get; }
        protected Color UIDisabled { get; }

        protected virtual Color CurrentState { get; } = new Color(255, 255, 255, 0);
        protected bool IsFocused = false;
        protected bool interactable = true;
        protected bool checkState = false;

        private Sprite2D highlight;
        private const float fadeTime = 0.25f;
        private float currentTime;
        private float setTime;
        private Color currentColour;
        private Color setColour;
        private bool forceTransitionCompletion = false;
        private bool colorTransitionRunning = false;
        private bool active;
        private bool dragging;

        public bool Active
        {
            get => active; set
            {
                if (active != value)
                {
                    SetActive(value);
                }
            }
        }

        public bool Interactable
        {
            get => interactable; set
            {
                if (interactable != value)
                {
                    if (!value)
                    {
                        DisableInteraction();
                    }
                    else
                    {
                        EnableInteraction();
                    }
                }
                interactable = value;
            }
        }

        public InterfaceButton(Rectangle bounds)
        {
            this.Bounds = bounds;
            this.highlight = new Sprite2D(AssetManager.Get.LoadTexture($"{AssetManager.SpriteRoot}/interface"),
                new Rectangle(128, 0, 64, 64));
            Input.AddPointerEventListener(this);
        }

        public InterfaceButton(Rectangle bounds, Color pressed, Color selected, Color highlighted, Color inactive, Color disabled)
        {
            this.Bounds = bounds;
            this.highlight = new Sprite2D(AssetManager.Get.LoadTexture($"{AssetManager.SpriteRoot}/interface"),
                new Rectangle(128, 0, 64, 64));


            this.UIPressed = pressed;
            this.UISelected = selected;
            this.UIHighlight = highlighted;
            this.UIInactive = inactive;
            this.UIDisabled = disabled;
        }

        public virtual void DisableInteraction()
        {
            SetColor(UIInactive, 0);
            interactable = false;
        }

        public virtual void EnableInteraction()
        {
            interactable = true;
            UpdateState();
        }

        public virtual void SetActive(bool setTo)
        {
            if (!active && setTo)
            {
                Input.AddPointerEventListener(this);
            }
            else if (active && !setTo)
            {
                Input.RemovePointerEventListener(this);
            }
            this.active = setTo;
            UpdateState();
        }

        protected void SetColor(Color Colour, float time = fadeTime)
        {
            if (colorTransitionRunning == true)
            {
                //If our colour is already being set.
                if (setColour.Equals(Colour))
                {
                    return;
                }
                //If the current transition is un-interruptable
                if (forceTransitionCompletion)
                {
                    return;
                }
            }
            //If we're inactive and can't perform a transition
            if (!Active)
            {
                currentColour = UIInactive;
                return;
            }
            setColour = new Color((byte)(CurrentState.R + Colour.R), (byte)(CurrentState.G + Colour.G), (byte)(CurrentState.B + Colour.B), (byte)(CurrentState.A + Colour.A));

            currentTime = 0;
            setTime = time;
            colorTransitionRunning = true;
        }

        public void Tick()
        {
            if (colorTransitionRunning)
            {
                if (currentTime < setTime)
                {
                    currentTime += Time.DeltaTime;
                    currentColour = Color.Lerp(currentColour, setColour, currentTime / setTime);
                }
                else
                {
                    colorTransitionRunning = false;
                    if (checkState || forceTransitionCompletion)
                    {
                        checkState = false;
                        forceTransitionCompletion = false;
                        UpdateState();
                    }
                }
            }
        }

        public void Draw()
        {
            UserInterface.Get.DrawUI(highlight.Texture, Bounds, highlight.SourceRect, currentColour, 0, Vector2.Zero, false, false, false);
        }

        public virtual void UpdateState(bool forceCheckFocus = false)
        {
            if (!Active)
            {
                currentColour = UIInactive;
                //Debug.Log("Disabled::"+gameObject.name);
                return;
            }
            if (!interactable)
            {
                SetColor(UIDisabled);
                //Debug.Log("Uninteractable::" + gameObject.name);
                return;
            }
            if (!forceCheckFocus && dragging)
            {
                if (IsFocused)
                {
                    //Debug.Log("FocusedAndActive::" + gameObject.name);
                    SetColor(new Color((byte)0, (byte)0, (byte)0, (byte)(UISelected.A + UIHighlight.A)));
                }
                else
                {
                    //Debug.Log("Selected::" + gameObject.name);
                    SetColor(UISelected);
                }
            }
            else
            {
                if (IsFocused)
                {
                    //Debug.Log("Focused::" + gameObject.name);
                    SetColor(UIHighlight, 0.05f);
                }
                else
                {
                    //Debug.Log("Inactive::" + gameObject.name);
                    SetColor(UIInactive);
                }
            }
        }

        public bool PointerEnter(MouseButtonActionState Data)
        {
            checkState = true;
            IsFocused = true;
            UpdateState();
            OnFocus?.Invoke(this, Data);
            return true;
        }

        public bool PointerExit(MouseButtonActionState Data)
        {
            checkState = true;
            IsFocused = false;
            UpdateState();
            OnUnfocus?.Invoke(this, Data);
            return false;
        }

        public bool PointerClick(MouseButtonActionState Data)
        {
            OnClicked?.Invoke(this, Data);
            if (Data.buttonData.clickCount > 1)
            {
                OnDoubleClicked?.Invoke(this, Data);
            }
            return true;
        }

        public bool PointerDown(MouseButtonActionState Data)
        {
            SetColor(UIPressed, 0.05f);
            forceTransitionCompletion = true;
            return true;
        }

        public bool PointerUp(MouseButtonActionState Data)
        {
            UpdateState();
            return true;
        }

        public bool PointerBeginDrag(MouseButtonActionState Data)
        {
            OnBeginDrag?.Invoke(this, Data);
            dragging = true;
            UpdateState();
            return true;
        }

        public bool PointerDrag(MouseButtonActionState Data)
        {
            return true;
        }

        public bool PointerEndDrag(MouseButtonActionState Data)
        {
            OnEndDrag?.Invoke(this, Data);
            dragging = false;
            UpdateState();
            return true;
        }

        public IPointerEventListener Intersect(Point mousePos)
        {
            if (mousePos.X > Bounds.Left && mousePos.Y < Bounds.Bottom &&
                mousePos.X < Bounds.Right && mousePos.Y > Bounds.Top)
            {
                return this;
            }
            return null;
        }
    }
}
