using System;
using System.Collections;
using System.Collections.Generic;
using NLog;
using Nebula;
using Nebula.Main;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Nebula
{
    public interface INebulaGame
    {
        string DataPath { get; }
        ContentManager Content { get; }
        GraphicsDeviceManager GraphicsDeviceMgr { get; }
        GraphicsDevice GraphicsDevice { get; }

    }
}