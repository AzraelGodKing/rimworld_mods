#if RIMWORLD_1_7
using UnityEngine;
using Verse;

namespace WaveSurvivalReforged
{
    /// <summary>
    /// RimWorld 1.7 only: a screen-edge glow that builds through the last hours before a wave
    /// and flashes when it arrives, red for normal waves, violet for special and amber for boss
    /// waves. Draw-only (no Rand, nothing saved). The 1.6 build keeps just the alert pulse.
    /// </summary>
    internal static class WaveVignette
    {
        private const string ShaderDefName = "WSR_WaveVignette";
        private const float WarnStrength = 0.32f;
        private const float FlashStrength = 0.75f;
        private const float FlashSeconds = 2.5f;

        private static Material material;
        private static bool failed;
        private static float flashStart = -100f;
        private static WavePool flashPool;

        public static void Notify_WaveArrived(WavePool pool)
        {
            flashStart = Time.realtimeSinceStartup;
            flashPool = pool;
        }

        public static void Draw(WaveSurvivalComponent waves)
        {
            if (!WaveSurvivalMod.Settings.screenEffect || !WaveSurvivalComponent.ModeActive || !waves.Started)
                return;
            Map map = Find.CurrentMap;
            if (map == null || RimWorld.Planet.WorldRendererUtility.WorldSelected)
                return;

            float strength = 0f;
            WavePool pool = waves.NextWavePool;
            float sinceFlash = Time.realtimeSinceStartup - flashStart;
            if (sinceFlash >= 0f && sinceFlash < FlashSeconds)
            {
                strength = FlashStrength * (1f - sinceFlash / FlashSeconds);
                pool = flashPool;
            }
            int ticks = waves.TicksUntilNextWave;
            if (ticks >= 0 && ticks <= Alert_WaveTimer.WarnTicks)
            {
                float ramp = 1f - ticks / (float)Alert_WaveTimer.WarnTicks;
                // A slow breath, about one every three seconds.
                float breath = 0.85f + 0.15f * Mathf.Sin(Time.realtimeSinceStartup * 2.1f);
                strength = Mathf.Max(strength, WarnStrength * ramp * breath);
            }
            if (strength <= 0.005f || !EnsureMaterial())
                return;

            Color c = ColorFor(pool);
            material.color = new Color(c.r, c.g, c.b, strength);
            // A quad over the camera's view, so the glow sits at the screen edges.
            CellRect view = Find.CameraDriver.CurrentViewRect;
            var center = new Vector3((view.minX + view.maxX + 1) * 0.5f,
                AltitudeLayer.MetaOverlays.AltitudeFor() + 0.2f,
                (view.minZ + view.maxZ + 1) * 0.5f);
            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(view.Width, 1f, view.Height));
            Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
        }

        private static Color ColorFor(WavePool pool)
        {
            switch (pool)
            {
                case WavePool.Boss:
                    return new Color(1f, 0.55f, 0.05f);
                case WavePool.Special:
                    return new Color(0.6f, 0.15f, 0.9f);
                default:
                    return new Color(0.85f, 0.05f, 0.05f);
            }
        }

        private static bool EnsureMaterial()
        {
            if (material != null)
                return true;
            if (failed)
                return false;
            Shader shader = DefDatabase<ShaderDef>.GetNamedSilentFail(ShaderDefName)?.Shader;
            if (shader == null)
            {
                failed = true;
                Log.Warning("[WaveSurvivalReforged] 1.7 shader " + ShaderDefName + " did not load; the wave screen effect is off.");
                return false;
            }
            material = new Material(shader);
            return true;
        }
    }
}
#endif
