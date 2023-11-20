using Microsoft.Xna.Framework;
using Nebula.Main;
using System;

namespace Nebula.Input
{
    public interface IPointerEventListener
    {
        public IPointerEventListener Intersect(Point mousePos);
        public IPointerEventListener Parent { get; }
        public IPointerEventListener[] Children { get; }
    }

    public interface IPointerEventBase : IPointerEventListener
    {

    }

    public interface IPointerEnterHandler : IPointerEventBase
    {
        public bool PointerEnter(MouseButtonActionState Data);
    }

    public interface IPointerExitHandler : IPointerEventBase
    {
        public bool PointerExit(MouseButtonActionState Data);
    }

    public interface IPointerDownHandler : IPointerEventBase
    {
        public bool PointerDown(MouseButtonActionState Data);
    }

    public interface IPointerUpHandler : IPointerEventBase
    {
        public bool PointerUp(MouseButtonActionState Data);
    }

    public interface IPointerClickHandler : IPointerEventBase
    {
        public bool PointerClick(MouseButtonActionState Data);
    }


}
