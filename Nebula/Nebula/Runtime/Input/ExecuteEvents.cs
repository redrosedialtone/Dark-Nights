using Nebula.Main;
using System;
using System.Collections.Generic;

namespace Nebula.Input
{
    public class ExecuteEvents
    {
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        public ExecuteEvents()
        {

        }

        public const EventHandle pointerUp = EventHandle.PointerUp;
        public const EventHandle pointerDown = EventHandle.PointerDown;
        public const EventHandle pointerClick = EventHandle.PointerClick;

        public enum EventHandle
        {
            PointerUp,
            PointerDown,
            PointerClick,
            PointerEnter,
            PointerExit
        }

        public static IPointerEventType ExecuteHierarchy<IPointerEventType>(IPointerEventListener[] Listeners, MouseButtonActionState Data, EventHandle Event) where IPointerEventType : IPointerEventBase
        {
            if (Listeners == null || Listeners.Length == 0)
            {
                return default;
            }
            Stack<IPointerEventListener> stack = new Stack<IPointerEventListener>();
            foreach (var listener in Listeners)
            {
                stack.Push(listener);
            }
            IPointerEventListener cur = stack.Pop();
            while (cur != null)
            {
                bool _triggered = false;
                switch (Event)
                {
                    case EventHandle.PointerUp:
                        if (cur is IPointerUpHandler _upHandle) { _triggered = _upHandle.PointerUp(Data); }
                        break;
                    case EventHandle.PointerDown:
                        if (cur is IPointerDownHandler _downHandle) {  _triggered = _downHandle.PointerDown(Data); }
                        break;
                    case EventHandle.PointerClick:
                        if (cur is IPointerClickHandler _clickHandle) { _triggered = _clickHandle.PointerClick(Data); }
                        break;
                    case EventHandle.PointerEnter:
                        if (cur is IPointerEnterHandler _enterHandle) { _triggered = _enterHandle.PointerEnter(Data); }
                        break;
                    case EventHandle.PointerExit:
                        if (cur is IPointerExitHandler _exitHandle){ _triggered = _exitHandle.PointerExit(Data); }
                        break;
                    default:
                        break;
                }
                if (_triggered)
                {
                    return (IPointerEventType)cur;
                }
                if (cur.Children != null && cur.Children.Length > 0)
                {
                    foreach (var child in cur.Children)
                    {
                        var listener = child.Intersect(Data.mousePosition);
                        if (listener != null)
                        {
                            stack.Push(listener);
                        }                       
                    }
                }
                cur = null;
                if (stack.Count > 0)
                {
                    cur = stack.Pop();
                }
            }
            return default;
        }

        public static IPointerEventType GetEventListener<IPointerEventType>(IPointerEventListener[] Listeners, MouseButtonActionState Data, EventHandle Event) where IPointerEventType : IPointerEventBase
        {
            if (Listeners == null || Listeners.Length == 0)
            {
                return default;
            }
            Stack<IPointerEventListener> stack = new Stack<IPointerEventListener>();
            foreach (var listener in Listeners)
            {
                stack.Push(listener);
            }
            IPointerEventListener cur = stack.Pop();
            while (cur != null)
            {
                switch (Event)
                {
                    case EventHandle.PointerUp:
                        if (cur is IPointerUpHandler _upHandle) { return (IPointerEventType)cur; }
                        break;
                    case EventHandle.PointerDown:
                        if (cur is IPointerDownHandler _downHandle) { return (IPointerEventType)cur; }
                        break;
                    case EventHandle.PointerClick:
                        if (cur is IPointerClickHandler _clickHandle) { return (IPointerEventType)cur; }
                        break;
                    case EventHandle.PointerEnter:
                        if (cur is IPointerEnterHandler _enterHandle) { return (IPointerEventType)cur; }
                        break;
                    case EventHandle.PointerExit:
                        if (cur is IPointerExitHandler _exitHandle) { return (IPointerEventType)cur; }
                        break;
                    default:
                        break;
                }
                if (cur.Children != null && cur.Children.Length > 0)
                {
                    foreach (var child in cur.Children)
                    {
                        var listener = child.Intersect(Data.mousePosition);
                        if (listener != null)
                        {
                            stack.Push(listener);
                        }
                    }
                }
                cur = null;
                if (stack.Count > 0)
                {
                    cur = stack.Pop();
                }
            }
            return default;
        }

        public static bool ExecuteEvent(IPointerEventBase Event, MouseButtonActionState Data, EventHandle EventType)
        {
            bool _triggered = false;
            switch (EventType)
            {
                case EventHandle.PointerUp:
                    if (Event is IPointerUpHandler _upHandle) { _triggered = _upHandle.PointerUp(Data); }
                    break;
                case EventHandle.PointerDown:
                    if (Event is IPointerDownHandler _downHandle) { _triggered = _downHandle.PointerDown(Data); }
                    break;
                case EventHandle.PointerClick:
                    if (Event is IPointerClickHandler _clickHandle) { _triggered = _clickHandle.PointerClick(Data); }
                    break;
                case EventHandle.PointerEnter:
                    if (Event is IPointerEnterHandler _enterHandle) { _triggered = _enterHandle.PointerEnter(Data); }
                    break;
                case EventHandle.PointerExit:
                    if (Event is IPointerExitHandler _exitHandle) { _triggered = _exitHandle.PointerExit(Data); }
                    break;
                default:
                    break;
            }
            return _triggered;
        }

    }
}
