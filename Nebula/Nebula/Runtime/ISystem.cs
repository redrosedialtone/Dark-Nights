using Microsoft.Xna.Framework;
using Nebula.Main;
using System.Collections;
using System.Collections.Generic;

namespace Nebula.Systems
{
    /// <summary>
    /// main.Thread() Process
    /// </summary>
    public interface IManager
    {
        void Init();
        void OnInitialized();
        void Tick();

        bool Initialized { get; }
    }

    public abstract class Manager : IManager
    {
        public bool Initialized { get; protected set; }
        public abstract void Init();
        public virtual void OnInitialized() { }
        public virtual void Tick() { }
        public virtual void Update() { }
        public virtual void Draw() { }
    }

}
