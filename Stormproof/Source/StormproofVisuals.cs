using System;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Stormproof
{
    // Defender fields, spire arcs, capacitor glow and weather overlays (AZR-502/499).
    // Everything here is drawn per frame for the map on screen and never ticks.
    // Vanilla shaders only, so the same code runs on 1.6 and 1.7.
    [StaticConstructorOnStartup]
    public static class StormproofVisuals
    {
        private const string TexRoot = "Stormproof/Effects/";
        private const int AlphaSteps = 20;
        private const float ArcSeconds = 0.6f;
        private const float FlashSeconds = 1.5f;

        public static readonly Color FieldShield = new Color(1f, 0.55f, 0.25f);
        public static readonly Color FieldBarrier = new Color(0.45f, 1f, 0.55f);
        public static readonly Color FieldClimate = new Color(0.55f, 0.8f, 1f);
        public static readonly Color FieldSky = new Color(1f, 0.92f, 0.55f);
        public static readonly Color FieldDrought = new Color(0.4f, 0.7f, 1f);
        public static readonly Color FieldDampener = new Color(0.65f, 0.55f, 1f);
        private static readonly Color ArcColor = new Color(0.75f, 0.9f, 1f);
        private static readonly Color CapacitorColor = new Color(0.45f, 0.8f, 1f);

        public static readonly Color RingOff = new Color(0.75f, 0.35f, 0.3f);
        public static readonly Color RingIdle = new Color(0.85f, 0.85f, 0.85f);
        public static readonly Color RingProtecting = new Color(0.35f, 0.85f, 1f);

        private static readonly Texture2D FieldTex = Load("Field");
        private static readonly Texture2D GlowTex = Load("Glow");
        private static readonly Texture2D AuroraTex = Load("Aurora", repeat: true);
        private static readonly Texture2D HazeTex = Load("Haze", repeat: true);
        private static readonly Texture2D FrostTex = Load("Frost");

        // Our own material instances, so tinting and panning never touch vanilla's.
        private static readonly Material AuroraMat = Own(AuroraTex, ShaderDatabase.MoteGlow);
        private static readonly Material HeatHazeMat = Own(HazeTex, ShaderDatabase.Transparent);
        private static readonly Material FogMat = Own(HazeTex, ShaderDatabase.Transparent);
        private static readonly Material FrostMat = Own(FrostTex, ShaderDatabase.Transparent);

        private static readonly Dictionary<(Texture2D, Shader, Color, int), Material> faded =
            new Dictionary<(Texture2D, Shader, Color, int), Material>();

        public static bool Enabled => StormproofMod.Settings.visualEffects;

        private static bool Reduced => StormproofMod.Settings.reducedEffects;

        private static float Intensity => Reduced ? 0.45f : 1f;

        // Slow breathing, 0.75 to 1. Flat when reduced, for photosensitivity.
        private static float Pulse(float seed)
        {
            return Reduced ? 0.85f : 0.875f + 0.125f * Mathf.Sin(Time.realtimeSinceStartup * 2.2f + seed);
        }

        // Fields, glows and arcs belong to the map on screen, in map view.
        public static bool ShouldDraw(Map map) =>
            Enabled && map != null && map == Find.CurrentMap && !WorldRendererUtility.WorldSelected;

        // ---- Buildings ----

        public static void DrawMap(Map map, MapComponent_Stormproof comp)
        {
            CellRect view = Find.CameraDriver.CurrentViewRect.ExpandedBy(4);
            DrawFields(StormproofRegistry.Shields.On(map), view, FieldShield, c => c.Protecting);
            DrawFields(StormproofRegistry.AtmosphericBarriers.On(map), view, FieldBarrier, c => c.Protecting);
            DrawFields(StormproofRegistry.ClimateStabilizers.On(map), view, FieldClimate, c => c.Protecting);
            DrawFields(StormproofRegistry.SkyRestorers.On(map), view, FieldSky, c => c.Protecting);
            DrawFields(StormproofRegistry.DroughtCondensers.On(map), view, FieldDrought, c => c.Protecting);
            DrawDampeners(map, view);
            DrawCapacitors(StormproofRegistry.Capacitors.On(map), view);
            DrawArcs(comp.Arcs);
        }

        private static void DrawFields<T>(List<T> comps, CellRect view, Color color, Func<T, bool> protecting)
            where T : ThingComp
        {
            for (int i = 0; i < comps.Count; i++)
            {
                T comp = comps[i];
                if (!view.Contains(comp.parent.Position) || !protecting(comp))
                {
                    continue;
                }
                float size = Math.Max(comp.parent.def.size.x, comp.parent.def.size.z) + 1.6f;
                DrawQuad(FieldTex, comp.parent.DrawPos, size, color,
                    0.55f * Intensity * Pulse(comp.parent.thingIDNumber));
            }
        }

        // The dampener shows its real cover while an ion storm leans on it, and
        // flashes it for a moment after it stops an EMP.
        private static void DrawDampeners(Map map, CellRect view)
        {
            List<CompEmpDampener> dampeners = StormproofRegistry.Dampeners.On(map);
            bool storm = StormproofCompatApi.IonStormActive(map);
            for (int i = 0; i < dampeners.Count; i++)
            {
                CompEmpDampener d = dampeners[i];
                if (!d.Active)
                {
                    continue;
                }
                float flash = Reduced ? 0f : 1f - (Time.realtimeSinceStartup - d.LastBlockRealtime) / FlashSeconds;
                if (!storm && flash <= 0f)
                {
                    continue;
                }
                float alpha = Math.Max(storm ? 0.16f * Pulse(d.parent.thingIDNumber) : 0f, 0.35f * flash) * Intensity;
                // The texture rim sits at 90% of its half-width.
                DrawQuad(FieldTex, d.parent.DrawPos, d.Props.protectRadius * 2f / 0.9f, FieldDampener, alpha);
            }
        }

        private static void DrawCapacitors(List<CompStormCapacitor> capacitors, CellRect view)
        {
            for (int i = 0; i < capacitors.Count; i++)
            {
                CompStormCapacitor c = capacitors[i];
                float charge = c.Props.storedEnergyMax > 0f ? c.StoredEnergy / c.Props.storedEnergyMax : 0f;
                if (charge <= 0.02f || !view.Contains(c.parent.Position))
                {
                    continue;
                }
                float size = Math.Max(c.parent.def.size.x, c.parent.def.size.z) * 1.4f;
                DrawQuad(GlowTex, c.parent.DrawPos, size, CapacitorColor,
                    0.65f * charge * Intensity * Pulse(c.parent.thingIDNumber));
            }
        }

        // ---- Arcs ----

        public struct Arc
        {
            public Vector3 From;
            public Vector3 To;
            public int Seed;
            public float Start;
        }

        public static void AddArc(Map map, Vector3 from, Vector3 to)
        {
            if (!Enabled || map == null)
            {
                return;
            }
            MapComponent_Stormproof comp = MapComponent_Stormproof.For(map);
            if (comp == null)
            {
                return;
            }
            comp.Arcs.Add(new Arc
            {
                From = from,
                To = to,
                Seed = Rand.Int,
                Start = Time.realtimeSinceStartup,
            });
        }

        private static void DrawArcs(List<Arc> arcs)
        {
            float now = Time.realtimeSinceStartup;
            for (int i = arcs.Count - 1; i >= 0; i--)
            {
                float age = (now - arcs[i].Start) / ArcSeconds;
                if (age >= 1f || age < 0f)
                {
                    arcs.RemoveAt(i);
                    continue;
                }
                // Reduced: a steady line that fades, with no flicker.
                float flicker = Reduced ? 1f : (((int)(now * 30f) + arcs[i].Seed) % 3 == 0 ? 0.6f : 1f);
                DrawJagged(arcs[i], (1f - age) * flicker * Intensity);
            }
        }

        private static void DrawJagged(Arc arc, float alpha)
        {
            Material mat = Faded(BaseContent.WhiteTex, ShaderDatabase.MoteGlow, ArcColor, alpha);
            if (mat == null)
            {
                return;
            }
            float y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Vector3 a = arc.From.WithY(y);
            Vector3 b = arc.To.WithY(y);
            Vector3 dir = b - a;
            float length = dir.magnitude;
            if (length < 0.5f)
            {
                return;
            }
            Vector3 side = new Vector3(-dir.z, 0f, dir.x) / length;
            int segments = Mathf.Clamp((int)(length / 1.5f), 3, 24);
            // Same seed each frame, so the bolt holds its shape while it fades.
            var rand = new System.Random(arc.Seed);
            Vector3 prev = a;
            for (int s = 1; s <= segments; s++)
            {
                float t = (float)s / segments;
                Vector3 next = s == segments
                    ? b
                    : a + dir * t + side * ((float)rand.NextDouble() - 0.5f) * Math.Min(2.2f, length * 0.25f);
                GenDraw.DrawLineBetween(prev, next, mat, 0.22f);
                prev = next;
            }
        }

        // ---- Weather ----

        public static void DrawIonAurora(Map map, float lerp, float burst)
        {
            if (AuroraMat == null || !ShouldDrawWeather(map) || lerp <= 0f)
            {
                return;
            }
            float flash = Reduced ? 0f : burst;
            Color c = new Color(0.55f, 1f, 0.75f, (0.3f + 0.35f * flash) * lerp * Intensity);
            Pan(AuroraMat, 0.25f, new Vector2(0.006f, 0.0015f));
            AuroraMat.color = c;
            SkyOverlay.DrawWorldOverlay(map, AuroraMat);
        }

        public static void DrawHeatHaze(Map map, float lerp, bool countered)
        {
            if (HeatHazeMat == null || !ShouldDrawWeather(map) || lerp <= 0f)
            {
                return;
            }
            float strength = countered ? 0.3f : 1f;
            Pan(HeatHazeMat, 0.4f, new Vector2(0.002f, 0.004f));
            HeatHazeMat.color = new Color(1f, 0.62f, 0.3f, 0.16f * strength * lerp * Intensity);
            SkyOverlay.DrawWorldOverlay(map, HeatHazeMat);
        }

        public static void DrawFrost(Map map, float lerp, bool countered)
        {
            if (FrostMat == null || !ShouldDrawWeather(map) || lerp <= 0f)
            {
                return;
            }
            float strength = countered ? 0.3f : 1f;
            FrostMat.color = new Color(0.85f, 0.93f, 1f, 0.55f * strength * lerp * Intensity);
            SkyOverlay.DrawScreenOverlay(FrostMat);
        }

        // Green while it poisons; a calmer blue-green while a barrier holds it off.
        public static void DrawToxicFog(Map map, float lerp, bool countered)
        {
            if (FogMat == null || !ShouldDrawWeather(map) || lerp <= 0f)
            {
                return;
            }
            Color tint = countered ? new Color(0.45f, 0.85f, 0.8f, 0.12f) : new Color(0.55f, 0.85f, 0.2f, 0.28f);
            Pan(FogMat, 0.3f, new Vector2(0.003f, -0.0012f));
            FogMat.color = new Color(tint.r, tint.g, tint.b, tint.a * lerp * Intensity);
            SkyOverlay.DrawWorldOverlay(map, FogMat);
        }

        private static bool ShouldDrawWeather(Map map) => ShouldDraw(map) && !map.IsPocketMap;

        // ---- Helpers ----

        private static void Pan(Material mat, float scale, Vector2 speed)
        {
            mat.mainTextureScale = new Vector2(scale, scale);
            if (!Find.TickManager.Paused)
            {
                mat.mainTextureOffset += speed * Time.deltaTime * Find.TickManager.TickRateMultiplier;
            }
        }

        private static void DrawQuad(Texture2D tex, Vector3 center, float size, Color color, float alpha)
        {
            Material mat = Faded(tex, ShaderDatabase.MoteGlow, color, alpha);
            if (mat == null)
            {
                return;
            }
            Vector3 pos = center.WithY(AltitudeLayer.MoteOverhead.AltitudeFor());
            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(size, 1f, size));
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }

        // One material per (texture, colour, alpha step), made once and reused.
        private static Material Faded(Texture2D tex, Shader shader, Color color, float alpha)
        {
            if (tex == null)
            {
                return null;
            }
            int step = Mathf.RoundToInt(Mathf.Clamp01(alpha) * AlphaSteps);
            if (step <= 0)
            {
                return null;
            }
            var key = (tex, shader, color, step);
            if (!faded.TryGetValue(key, out Material mat))
            {
                mat = MaterialPool.MatFrom(tex, shader, new Color(color.r, color.g, color.b, (float)step / AlphaSteps));
                faded[key] = mat;
            }
            return mat;
        }

        private static Texture2D Load(string name, bool repeat = false)
        {
            Texture2D tex = ContentFinder<Texture2D>.Get(TexRoot + name, reportFailure: false);
            if (tex == null)
            {
                Log.WarningOnce("[Stormproof] Missing effect texture " + TexRoot + name + "; that effect is skipped.",
                    name.GetHashCode());
                return null;
            }
            if (repeat)
            {
                tex.wrapMode = TextureWrapMode.Repeat;
            }
            return tex;
        }

        private static Material Own(Texture2D tex, Shader shader)
        {
            if (tex == null)
            {
                return null;
            }
            return new Material(MaterialPool.MatFrom(tex, shader, Color.white));
        }
    }
}
