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
        public virtual void Init() { Initialized = true; ApplicationController.Get.Initialized(this); }
        public virtual void OnInitialized() { }
        public virtual void Tick() { }
    }

}
