using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using Nebula.Base;

namespace Nebula.Main
{
    class SmartFramerate : IDrawGizmos
    {
        double currentFrametimes;
        double weight;
        int numerator;
        SpriteFont _spriteFont;

        public double framerate
        {
            get
            {
                return (numerator / currentFrametimes);
            }
        }

        public bool DrawGizmo { get; set; }

        public SmartFramerate(int oldFrameWeight)
        {
            numerator = oldFrameWeight;
            weight = (double)oldFrameWeight / ((double)oldFrameWeight - 1d);
            _spriteFont = Resources.Load<SpriteFont>("FONT/Constantina");
        }

        public void Update(double timeSinceLastFrame)
        {
            currentFrametimes = currentFrametimes / weight;
            currentFrametimes += timeSinceLastFrame;
        }

        public void DrawGizmos(SpriteBatch Batch)
        {
            if (DrawGizmo)
            {
                var deltaTime = Time.deltaTime;
                this.Update(deltaTime);
                var fps = string.Format("FPS: {0:0.##}", this.framerate);
                Batch.DrawString(_spriteFont, fps, new Vector2(1, 1), Color.Yellow);
            }
            
        }

        public void SetDrawGizmo(bool drawGizmo)
        {
            DrawGizmo = drawGizmo;
        }
    }
}