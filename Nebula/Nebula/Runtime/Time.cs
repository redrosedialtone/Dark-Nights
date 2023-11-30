using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nebula.Runtime;
using System;

namespace Nebula.Main
{
	public static class Time
	{
        public static TimeSpan TotalTime = default(TimeSpan);
        public static TimeSpan ElapsedTime = default(TimeSpan);
        public static double ActiveElapsedTime = 0d;
        public static long TotalUpdates { get; private set; } = 0L;
        public static long TotalFrames { get; private set; } = 0L;
        public static bool RunningSlowly { get; private set; } = false;
        public static bool FixedTimeStep = true;
        public static bool VSyncEnabled = true;
        public static float DeltaTime;

        public static bool TickEnabled { get; private set; } = true;

        public static void EnableTick() { TickEnabled = true; }
        public static void DisableTick() { TickEnabled = false; }

        public static void Update(GameTime gameTime)
        {
            TotalTime = gameTime.TotalGameTime;
            ElapsedTime = gameTime.ElapsedGameTime;
            RunningSlowly = gameTime.IsRunningSlowly;

            if (TickEnabled == true)
            {
                ActiveElapsedTime += ElapsedTime.TotalMilliseconds;
            }
            DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            TotalUpdates++;
        }

        public static void Frame() { TotalFrames++; }
    }

    public class FramerateGizmo : IGizmo
    {
        public bool Enabled { get; set; }

        double currentFrametimes;
        double weight;
        int numerator;

        public double framerate
        {
            get
            {
                return (numerator / currentFrametimes);
            }
        }

        public FramerateGizmo(int oldFrameWeight)
        {
            Debug.NewDebugGizmo(this);
            numerator = oldFrameWeight;
            weight = (double)oldFrameWeight / ((double)oldFrameWeight - 1d);
        }

        public void Draw()
        {
            var fps = string.Format("FPS: {0:0.##}", this.framerate);
            DrawUtils.DrawText(fps, new Vector2(1, 1), Color.Yellow);
        }

        public void Update()
        {
            currentFrametimes = currentFrametimes / weight;
            currentFrametimes += Time.DeltaTime;
        }
    }
}
