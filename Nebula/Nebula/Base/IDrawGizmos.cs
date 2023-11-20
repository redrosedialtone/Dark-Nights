using Microsoft.Xna.Framework.Graphics;
using System;

namespace Nebula.Base
{
    public interface IDrawGizmos
    {
        bool DrawGizmo { get; }
        void DrawGizmos(SpriteBatch Batch);
        void SetDrawGizmo(bool drawGizmo);
    }
}
