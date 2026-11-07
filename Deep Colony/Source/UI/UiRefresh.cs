using UnityEngine;

namespace DeepColony
{
    /// <summary>
    /// Real-time throttle for hub rows: lists are rebuilt at most twice a second, or
    /// at once when the filter key changes, instead of every frame (AZR-619).
    /// </summary>
    internal struct UiRefresh
    {
        private const float Seconds = 0.5f;
        private float last;
        private int lastKey;
        private bool primed;

        internal bool Due(int key = 0)
        {
            float now = Time.realtimeSinceStartup;
            if (primed && key == lastKey && now - last < Seconds) return false;
            primed = true;
            last = now;
            lastKey = key;
            return true;
        }
    }
}
