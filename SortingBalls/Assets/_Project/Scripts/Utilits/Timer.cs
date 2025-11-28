using System;
using LitMotion;

namespace _Project.Scripts.Utils
{
    public static class Timer
    {
        public static MotionHandle After(float seconds, Action onComplete)
        {
            return LMotion.Create(0f, 1f, seconds)
                .WithOnComplete(onComplete)
                .RunWithoutBinding();
        }

        public static MotionHandle Tick(float seconds, Action<float> onTick, Action onComplete = null)
        {
            return LMotion.Create(0f, 1f, seconds)
                .WithOnComplete(onComplete)
                .Bind(progress => onTick?.Invoke(progress));
        }
    }
}
