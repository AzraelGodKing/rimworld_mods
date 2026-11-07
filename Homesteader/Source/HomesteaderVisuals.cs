#nullable enable
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Homesteader
{
    /// <summary>
    /// Heat haze for the Kats heat dome and moving water in the stone fountain
    /// (AZR-577). Drawn per frame for the map on screen and never ticks or uses
    /// Rand, so Multiplayer is unaffected. Vanilla shaders only, so the same code
    /// runs on 1.6 and 1.7. Both effects turn off with the "Show effects" setting.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class HomesteaderVisuals
    {
        private const string TexRoot = "Homesteader/Effects/";
        private const int AlphaSteps = 20;

        /// <summary>Cooling overlay edge colour: icy blue.</summary>
        public static readonly Color CoolingEdge = new Color(0.55f, 0.85f, 1f);

        private static readonly Color WaterColor = new Color(0.7f, 0.88f, 1f);

        private static readonly Texture2D? HazeTex = Load("Haze", repeat: true);
        private static readonly Texture2D? RippleTex = Load("Ripple");

        // Our own instance, so tinting and panning never touch a shared vanilla material.
        private static readonly Material? HeatHazeMat = HazeTex == null
            ? null
            : new Material(MaterialPool.MatFrom(HazeTex, ShaderDatabase.Transparent, Color.white));

        private static readonly Dictionary<int, Material> rippleMats = new Dictionary<int, Material>();

        // Game-speed clock for the water: stands still while paused, advances once per frame.
        private static float waterClock;
        private static int waterClockFrame = -1;

        internal static bool Enabled => HomesteaderMod.Settings.showEffects;

        /// <summary>Slow warm shimmer over the whole map while the heat dome holds.</summary>
        internal static void DrawHeatHaze(Map map, float lerp)
        {
            if (HeatHazeMat == null || !Enabled || lerp <= 0f || map != Find.CurrentMap)
            {
                return;
            }

            HeatHazeMat.mainTextureScale = new Vector2(0.4f, 0.4f);
            if (!Find.TickManager.Paused)
            {
                HeatHazeMat.mainTextureOffset += new Vector2(0.002f, 0.005f) * Time.deltaTime * Find.TickManager.TickRateMultiplier;
            }

            HeatHazeMat.color = new Color(1f, 0.66f, 0.35f, 0.14f * lerp);
            SkyOverlay.DrawWorldOverlay(map, HeatHazeMat);
        }

        /// <summary>Two rings spreading from the plume, half a cycle apart.</summary>
        internal static void DrawFountainWater(Thing fountain, float size)
        {
            if (RippleTex == null || !Enabled)
            {
                return;
            }

            AdvanceWaterClock();
            Vector3 center = fountain.TrueCenter().WithY(AltitudeLayer.BuildingOnTop.AltitudeFor());
            for (int i = 0; i < 2; i++)
            {
                float phase = Mathf.Repeat(waterClock * 0.45f + i * 0.5f, 1f);
                Material? mat = Faded(1f - phase);
                if (mat == null)
                {
                    continue;
                }

                float scale = size * Mathf.Lerp(0.35f, 1f, phase);
                Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, new Vector3(scale, 1f, scale));
                Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
            }
        }

        private static void AdvanceWaterClock()
        {
            if (waterClockFrame == Time.frameCount)
            {
                return;
            }

            waterClockFrame = Time.frameCount;
            if (!Find.TickManager.Paused)
            {
                waterClock += Time.deltaTime * Find.TickManager.TickRateMultiplier;
            }
        }

        // One material per alpha step, made once and reused.
        private static Material? Faded(float alpha)
        {
            int step = Mathf.RoundToInt(Mathf.Clamp01(alpha) * AlphaSteps);
            if (step <= 0 || RippleTex == null)
            {
                return null;
            }

            if (!rippleMats.TryGetValue(step, out Material? mat) || mat == null)
            {
                Color c = new Color(WaterColor.r, WaterColor.g, WaterColor.b, 0.55f * step / AlphaSteps);
                mat = MaterialPool.MatFrom(RippleTex, ShaderDatabase.MoteGlow, c);
                rippleMats[step] = mat;
            }

            return mat;
        }

        private static Texture2D? Load(string name, bool repeat = false)
        {
            Texture2D? tex = ContentFinder<Texture2D>.Get(TexRoot + name, reportFailure: false);
            if (tex == null)
            {
                Log.Warning("[Homesteader] Effect texture " + TexRoot + name + " is missing; that effect is off.");
                return null;
            }

            if (repeat)
            {
                tex.wrapMode = TextureWrapMode.Repeat;
            }

            return tex;
        }
    }

    /// <summary>Animated water on a fountain while it has water (AZR-577). Reusable by a waterwheel.</summary>
    public class CompProperties_FountainWater : CompProperties
    {
        /// <summary>Widest ring, in cells.</summary>
        public float rippleSize = 1.6f;

        public CompProperties_FountainWater()
        {
            compClass = typeof(CompFountainWater);
        }
    }

    public class CompFountainWater : ThingComp
    {
        private CompRefuelable? fuel;

        private CompProperties_FountainWater Props => (CompProperties_FountainWater)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            fuel = parent.GetComp<CompRefuelable>();
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (fuel == null || fuel.HasFuel)
            {
                HomesteaderVisuals.DrawFountainWater(parent, Props.rippleSize);
            }
        }
    }
}
