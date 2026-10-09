using UnityEngine;

namespace TerraLevel
{
    /// <summary>
    /// Client-side state of the scroll-adjustable radius.
    /// </summary>
    internal static class RadiusState
    {
        /// <summary>Radius of the vanilla Level Ground tool, read from the mud_road_v2 prefab at registration.</summary>
        public static float BaseRadius = 2f;

        /// <summary>Radius the next placed TerraLevel op will use.</summary>
        public static float Current = 2f;

        /// <summary>Scroll wheel delta captured by the ZInput patch, consumed once per frame by the plugin Update.</summary>
        public static float PendingScroll;

        public static void Init(float baseRadius)
        {
            BaseRadius = baseRadius > 0f ? baseRadius : 2f;
            var start = Cfg.DefaultRadius.Value > 0f ? Cfg.DefaultRadius.Value : BaseRadius;
            Current = Clamp(start);
        }

        /// <summary>Applies accumulated scroll. Returns true when the radius changed.</summary>
        public static bool ApplyPendingScroll()
        {
            if (PendingScroll == 0f)
            {
                return false;
            }

            var direction = PendingScroll > 0f ? 1f : -1f;
            PendingScroll = 0f;

            var step = Mathf.Max(0.05f, Cfg.RadiusStep.Value);
            var next = Clamp(Current + direction * step);
            next = Mathf.Round(next / step) * step;
            next = Clamp(next);

            if (Mathf.Approximately(next, Current))
            {
                return false;
            }

            Current = next;
            return true;
        }

        public static float Clamp(float value)
        {
            var min = Mathf.Max(0.05f, Cfg.MinRadius.Value);
            var max = Mathf.Max(min, Cfg.MaxRadius.Value);
            return Mathf.Clamp(value, min, max);
        }
    }
}
